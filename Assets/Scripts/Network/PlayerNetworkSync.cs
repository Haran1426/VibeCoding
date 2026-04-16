using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 네트워크 동기화.
///
/// 권한 모델:
///   - 위치/회전 : NetworkTransform (Owner 권한) — 내 캐릭터는 내가 움직임
///   - 행동(공격/점프/대시) : ServerRpc → 서버가 처리 후 모든 클라이언트에 결과 전달
///   - 분신 데이터 : 사망 시 서버가 InputFrame[] 를 ClientRpc 로 브로드캐스트
///   - 넉백 % : NetworkVariable 로 실시간 동기화
///   - 점수 : NetworkVariable 로 실시간 동기화
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerNetworkSync : NetworkBehaviour
{
    // ── NetworkVariables ────────────────────────────────────
    public NetworkVariable<float> NetKnockback = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> NetScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> NetPlayerId = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── 컴포넌트 ────────────────────────────────────────────
    private PlayerController _controller;
    private PlayerStats      _stats;
    private InputRecorder    _recorder;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _stats      = GetComponent<PlayerStats>();
        _recorder   = GetComponent<InputRecorder>();
    }

    // ════════════════════════════════════════════════════════
    //  NetworkBehaviour 생명주기
    // ════════════════════════════════════════════════════════

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 서버: playerId = OwnerClientId 기반 할당
            NetPlayerId.Value = (int)OwnerClientId;
            _stats.playerId   = (int)OwnerClientId;

            // 스폰 위치 배정
            Vector3 pos = NeonNetworkManager.Net != null
                ? NeonNetworkManager.Net.GetNextSpawnPoint()
                : Vector3.up;
            transform.position = pos;
        }

        if (IsOwner)
        {
            // 내 캐릭터: 키보드 또는 게임패드 활성화 (연결된 것만)
            var pi = GetComponent<PlayerInput>();
            var gi = GetComponent<GamepadInput>();
            if (pi != null) pi.enabled = true;
            if (gi != null) gi.enabled = true;

            _stats.playerId = (int)OwnerClientId;

            // 카메라를 내 캐릭터에 고정
            var cam = FindFirstObjectByType<ArenaCamera>();
            cam?.SetTarget(transform);
        }
        else
        {
            // 다른 플레이어: 로컬 입력 전체 비활성화
            var pi = GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false;

            var gi = GetComponent<GamepadInput>();
            if (gi != null) gi.enabled = false;

            var rec = GetComponent<InputRecorder>();
            if (rec != null) rec.enabled = false;
        }

        // NetworkVariable 변경 구독
        NetKnockback.OnValueChanged += OnKnockbackSync;
        NetScore.OnValueChanged     += OnScoreSync;
    }

    public override void OnNetworkDespawn()
    {
        NetKnockback.OnValueChanged -= OnKnockbackSync;
        NetScore.OnValueChanged     -= OnScoreSync;
    }

    // ── NetworkVariable 콜백 ────────────────────────────────
    private void OnKnockbackSync(float prev, float next)
    {
        _stats.knockbackPercent = next;
        EventBus.RaiseKnockbackChanged(_stats.playerId, next);
    }

    private void OnScoreSync(int prev, int next)
    {
        EventBus.RaiseScoreChanged(_stats.playerId, next);
    }

    // ════════════════════════════════════════════════════════
    //  ServerRpc — 클라이언트 → 서버
    // ════════════════════════════════════════════════════════

    /// <summary>공격 실행 요청</summary>
    [ServerRpc]
    public void AttackServerRpc(Vector3 aimDir)
    {
        // 서버에서 히트 판정
        Vector3 center = transform.position + aimDir.normalized * _stats.attackRange;
        center.y = transform.position.y + 0.5f;

        Collider[] hits = Physics.OverlapSphere(center, _stats.attackRadius);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            var receiver = col.GetComponent<KnockbackReceiver>();
            if (receiver == null) continue;

            Vector3 dir = (col.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = aimDir;

            receiver.ApplyKnockback(dir.normalized, _stats.attackPower, _stats.playerId);

            // 히트 대상이 플레이어라면 서버 점수 추가
            var targetSync = col.GetComponent<PlayerNetworkSync>();
            if (targetSync != null)
                AddScoreServerSide((int)OwnerClientId, 1);
        }

        // 모든 클라이언트에 공격 VFX 동기화
        AttackVFXClientRpc(center);
    }

    /// <summary>점프 실행 요청</summary>
    [ServerRpc]
    public void JumpServerRpc()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(Vector3.up * _stats.jumpForce, ForceMode.Impulse);
    }

    /// <summary>플레이어 사망 알림 (DeathDetector → 서버 → 전체 클라이언트)</summary>
    [ServerRpc]
    public void DiedServerRpc(InputFrame[] frames)
    {
        // 분신 데이터를 모든 클라이언트에 브로드캐스트
        int cloneId = 1000 + (int)OwnerClientId * 100 + Random.Range(0, 99);
        SpawnCloneClientRpc(frames, transform.position, cloneId);

        // 서버에서 리스폰 처리
        StartCoroutine(RespawnAfterDelay(2f));
    }

    // ════════════════════════════════════════════════════════
    //  ClientRpc — 서버 → 모든 클라이언트
    // ════════════════════════════════════════════════════════

    /// <summary>모든 클라이언트에서 분신 로컬 생성</summary>
    [ClientRpc]
    public void SpawnCloneClientRpc(InputFrame[] frames, Vector3 deathPos, int cloneId)
    {
        var list = new List<InputFrame>(frames);
        // PlayCloneSpawn 은 CloneManager.SpawnClone() 내부에서 호출됨 — 중복 방지
        CloneManager.Instance?.SpawnClone(list);
        EventBus.RaiseCloneSpawned(CloneManager.Instance?.ActiveCloneCount ?? 0);
    }

    /// <summary>공격 VFX 모든 클라이언트 재생</summary>
    [ClientRpc]
    public void AttackVFXClientRpc(Vector3 center)
    {
        VFXManager.Instance?.PlayAttack(center);
        AudioManager.Instance?.PlayAttack();
    }

    /// <summary>넉백 % 서버에서 동기화</summary>
    public void ServerUpdateKnockback(float value)
    {
        if (IsServer) NetKnockback.Value = value;
    }

    /// <summary>점수 서버에서 동기화</summary>
    public void AddScoreServerSide(int playerId, int amount)
    {
        if (!IsServer) return;
        NetScore.Value += amount;
    }

    // ── 리스폰 코루틴 (서버 전용) ────────────────────────────
    private System.Collections.IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);

        if (NetworkManager.Singleton.IsServer)
        {
            Vector3 pos = NeonNetworkManager.Net?.GetNextSpawnPoint() ?? Vector3.up;
            RespawnClientRpc(pos);
        }
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 pos)
    {
        transform.position = pos;
        _stats.ResetKnockback();
        GetComponent<DeathDetector>()?.ResetDead();
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(true);
        AudioManager.Instance?.PlayRespawn();
    }
}
