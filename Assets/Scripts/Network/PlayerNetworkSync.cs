using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 네트워크 동기화.
///
/// 권한 모델:
///   위치/회전      — NetworkTransform (Owner 권한)
///   공격 판정      — AttackServerRpc → 서버에서 히트 결정 → VFX ClientRpc
///   넉백 %        — NetworkVariable (Server write)
///   점수          — NetworkVariable (Server write)
///   플레이어ID     — NetworkVariable (Server write) → NonOwner playerId 동기화
///   사망/분신      — DiedServerRpc → SpawnCloneClientRpc + NotifyDiedClientRpc
///   분신 처치 점수 — Owner 로컬 감지 → CloneKilledServerRpc
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

    // ── Inspector ───────────────────────────────────────────
    [SerializeField] private LayerMask attackTargetMask;

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
            NetPlayerId.Value = (int)OwnerClientId;
            _stats.playerId   = (int)OwnerClientId;

            Vector3 pos = NeonNetworkManager.Net != null
                ? NeonNetworkManager.Net.GetNextSpawnPoint()
                : Vector3.up;
            transform.position = pos;
        }

        if (IsOwner)
        {
            var pi = GetComponent<PlayerInput>();
            var gi = GetComponent<GamepadInput>();
            if (pi != null) pi.enabled = true;
            if (gi != null) gi.enabled = true;

            _stats.playerId = (int)OwnerClientId;

            var cam = FindFirstObjectByType<ArenaCamera>();
            cam?.SetTarget(transform);
        }
        else
        {
            var pi = GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false;

            var gi = GetComponent<GamepadInput>();
            if (gi != null) gi.enabled = false;

            var rec = GetComponent<InputRecorder>();
            if (rec != null) rec.enabled = false;
        }

        // NetworkVariable 구독
        NetKnockback.OnValueChanged += OnKnockbackSync;
        NetScore.OnValueChanged     += OnScoreSync;
        NetPlayerId.OnValueChanged  += OnPlayerIdSync;

        // 늦게 접속한 클라이언트: 이미 설정된 playerId 반영
        if (NetPlayerId.Value >= 0)
        {
            _stats.playerId = NetPlayerId.Value;
            GetComponent<PlayerVisuals>()?.ApplyPlayerColor(NetPlayerId.Value);
        }

        // Owner: 로컬에서 분신 처치를 감지해 서버에 점수 알림
        if (IsOwner)
            EventBus.OnEntityDied += OnEntityDiedLocal;
    }

    public override void OnNetworkDespawn()
    {
        NetKnockback.OnValueChanged -= OnKnockbackSync;
        NetScore.OnValueChanged     -= OnScoreSync;
        NetPlayerId.OnValueChanged  -= OnPlayerIdSync;

        if (IsOwner)
            EventBus.OnEntityDied -= OnEntityDiedLocal;
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

    private void OnPlayerIdSync(int prev, int next)
    {
        _stats.playerId = next;
        GetComponent<PlayerVisuals>()?.ApplyPlayerColor(next);
    }

    // ── 분신 처치 감지 (Owner 로컬) ──────────────────────────

    private void OnEntityDiedLocal(int victimId, Vector3 pos, int killerId)
    {
        // 내가 처치한 분신(ID >= 100)만 처리. 플레이어 처치는 DiedServerRpc에서 처리.
        if (killerId != _stats.playerId) return;
        if (victimId < 100) return;

        CloneKilledServerRpc();
    }

    // ════════════════════════════════════════════════════════
    //  ServerRpc — 클라이언트 → 서버
    // ════════════════════════════════════════════════════════

    /// <summary>공격 실행. 서버에서 히트 판정 후 VFX 브로드캐스트.</summary>
    [ServerRpc]
    public void AttackServerRpc(Vector3 aimDir)
    {
        Vector3 center = transform.position + aimDir.normalized * _stats.attackRange;
        center.y = transform.position.y + 0.5f;

        // attackTargetMask 가 0(비어있음)이면 플레이어/분신 레이어 전체 허용
        int mask = attackTargetMask == 0 ? ~0 : (int)attackTargetMask;
        Collider[] hits = Physics.OverlapSphere(center, _stats.attackRadius, mask);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;

            var receiver = col.GetComponent<KnockbackReceiver>();
            if (receiver == null) continue;

            Vector3 dir = col.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = aimDir;

            receiver.ApplyKnockback(dir.normalized, _stats.attackPower, _stats.playerId);

            // 플레이어 적중 히트 점수 (+1)
            if (col.GetComponent<PlayerNetworkSync>() != null)
                AddNetScore(_stats.playerId, 1);
        }

        AttackVFXClientRpc(center);
    }

    /// <summary>플레이어 사망 알림. 서버에서 분신 생성 + 점수 + 리스폰 처리.</summary>
    [ServerRpc]
    public void DiedServerRpc(InputFrame[] frames, int killerId)
    {
        // 처치자 점수 (+5). 분신(ID >= 100)에게 죽은 경우 점수 없음.
        if (killerId >= 0 && killerId < 100)
            AddNetScore(killerId, 5);

        // 분신 ID를 서버에서 통일 생성 → 모든 클라이언트에 동일한 ID 부여
        int cloneId = 1000 + (int)OwnerClientId * 100
                           + (CloneManager.Instance?.ActiveCloneCount ?? 0);

        SpawnCloneClientRpc(frames, transform.position, cloneId);

        // 사망 이벤트 브로드캐스트 (KillFeed, ScoreSystem 등 EventBus 구독자 알림)
        NotifyDiedClientRpc((int)OwnerClientId, transform.position, killerId);

        StartCoroutine(RespawnAfterDelay(2f));
    }

    /// <summary>분신 처치 시 Owner → Server 점수 추가. GDD: 분신 처치 = +1.</summary>
    [ServerRpc]
    private void CloneKilledServerRpc()
    {
        AddNetScore(_stats.playerId, 1);
    }

    // ════════════════════════════════════════════════════════
    //  ClientRpc — 서버 → 모든 클라이언트
    // ════════════════════════════════════════════════════════

    /// <summary>모든 클라이언트에서 분신 로컬 생성.</summary>
    [ClientRpc]
    public void SpawnCloneClientRpc(InputFrame[] frames, Vector3 deathPos, int cloneId)
    {
        var list = new List<InputFrame>(frames);
        CloneManager.Instance?.SpawnClone(list);
        // RaiseCloneSpawned 는 CloneManager.SpawnClone 내부에서 발행 — 중복 방지
    }

    /// <summary>사망 이벤트를 모든 클라이언트에 브로드캐스트. KillFeed / ScoreSystem 구동.</summary>
    [ClientRpc]
    private void NotifyDiedClientRpc(int victimId, Vector3 pos, int killerId)
    {
        EventBus.RaiseEntityDied(victimId, pos, killerId);
    }

    /// <summary>공격 VFX + 히트음을 모든 클라이언트에 재생.</summary>
    [ClientRpc]
    public void AttackVFXClientRpc(Vector3 center)
    {
        VFXManager.Instance?.PlayAttack(center);
        AudioManager.Instance?.PlayAttackHit();
    }

    /// <summary>리스폰 처리: 위치/상태 초기화 후 활성화.</summary>
    [ClientRpc]
    private void RespawnClientRpc(Vector3 pos)
    {
        transform.position = pos;
        _stats.ResetKnockback();
        GetComponent<DeathDetector>()?.ResetDead();
        GetComponent<InputRecorder>()?.ClearRecording();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        gameObject.SetActive(true);

        // 리스폰 무적 (1.5초)
        _stats.StartInvincibility(1.5f);

        AudioManager.Instance?.PlayRespawn();
        VFXManager.Instance?.PlayLevelUp(pos);
    }

    // ════════════════════════════════════════════════════════
    //  서버 전용 유틸
    // ════════════════════════════════════════════════════════

    /// <summary>KnockbackReceiver 에서 호출. 서버에서만 NetKnockback 갱신.</summary>
    public void ServerUpdateKnockback(float value)
    {
        if (IsServer) NetKnockback.Value = value;
    }

    /// <summary>특정 playerId(ClientId)의 NetScore 를 amount 만큼 증가.</summary>
    private void AddNetScore(int playerId, int amount)
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if ((int)client.ClientId != playerId) continue;
            var sync = client.PlayerObject?.GetComponent<PlayerNetworkSync>();
            if (sync != null) sync.NetScore.Value += amount;
            return;
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!NetworkManager.Singleton.IsServer) yield break;

        Vector3 pos = NeonNetworkManager.Net?.GetNextSpawnPoint() ?? Vector3.up;
        RespawnClientRpc(pos);
    }
}
