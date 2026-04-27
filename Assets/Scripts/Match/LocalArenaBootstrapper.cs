using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로컬 멀티플레이어 전용 부트스트래퍼.
/// LocalMultiplayerConfig.PlayerCount 만큼 플레이어를 스폰하고
/// 각각에 입력 장치를 할당합니다.
///
/// 입력 배분:
///   P1        → 키보드 + 마우스 (PlayerInput)
///   P2 ~ P10  → 게임패드 0번 ~ 8번 (GamepadInput, gamepadIndex = i-1)
/// </summary>
public class LocalArenaBootstrapper : MonoBehaviour
{
    [Header("로컬 플레이어 프리팹")]
    [SerializeField] private GameObject localPlayerPrefab;

    [Header("스폰 포인트 (없으면 자동 계산)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("스폰 Y")]
    [SerializeField] private float spawnY = 2.5f;

    private readonly List<GameObject> _players = new List<GameObject>();

    void Start()
    {
        if (!LocalMultiplayerConfig.IsLocalMode) return;

        // 씬에 남아 있는 싱글플레이어 Player 제거
        var existing = GameObject.Find("Player");
        if (existing != null) Destroy(existing);

        SpawnPlayers();
        SetupCamera();
        SetupRespawnManager();
        SetupCloneManagerSpawnPoints();
    }

    // ── 스폰 ─────────────────────────────────────────────────────

    private void SpawnPlayers()
    {
        int count = LocalMultiplayerConfig.PlayerCount;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetSpawnPosition(i, count);
            GameObject playerGO = Instantiate(localPlayerPrefab, pos, Quaternion.identity);
            playerGO.name = "Player" + (i + 1);

            var stats = playerGO.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.playerId = i;
                stats.isClone  = false;
            }

            AssignInput(playerGO, i);

            // 색상 적용
            playerGO.GetComponent<PlayerVisuals>()?.ApplyPlayerColor(i);

            _players.Add(playerGO);

            // RespawnManager 에 등록
            RespawnManager.Instance?.RegisterPlayer(playerGO);
        }

        Debug.Log($"[LocalArena] {count}명 스폰 완료");
    }

    private void AssignInput(GameObject playerGO, int playerIndex)
    {
        var pi = playerGO.GetComponent<PlayerInput>();
        var gi = playerGO.GetComponent<GamepadInput>();

        if (playerIndex == 0)
        {
            // P1: 키보드 + 마우스
            if (pi != null) pi.enabled = true;
            if (gi != null) gi.enabled = false;
        }
        else
        {
            // P2+: 게임패드 (인덱스 = playerIndex - 1)
            if (pi != null) pi.enabled = false;
            if (gi != null)
            {
                gi.gamepadIndex = playerIndex - 1;
                gi.enabled      = true;

                int padCount = Gamepad.all.Count;
                if (gi.gamepadIndex >= padCount)
                    Debug.LogWarning($"[LocalArena] P{playerIndex + 1}: 게임패드 {gi.gamepadIndex}번이 연결되어 있지 않습니다. ({padCount}개 감지)");
            }
        }
    }

    // ── 카메라 ────────────────────────────────────────────────────

    private void SetupCamera()
    {
        var cam = ArenaCamera.Instance;
        if (cam == null) return;

        if (_players.Count == 1)
        {
            cam.SetTarget(_players[0].transform);
        }
        else
        {
            var targets = new Transform[_players.Count];
            for (int i = 0; i < _players.Count; i++)
                targets[i] = _players[i].transform;
            cam.SetTargets(targets);
        }
    }

    // ── RespawnManager ───────────────────────────────────────────

    private void SetupRespawnManager()
    {
        var rm = RespawnManager.Instance;
        if (rm == null) return;

        rm.ClearPlayers();
        foreach (var p in _players)
            rm.RegisterPlayer(p);
    }

    // ── CloneManager 스폰 포인트 연결 ────────────────────────────

    private void SetupCloneManagerSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        var cm = CloneManager.Instance;
        if (cm == null) return;

        typeof(CloneManager)
            .GetField("spawnPoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(cm, spawnPoints);
    }

    // ── 스폰 위치 계산 ────────────────────────────────────────────

    private Vector3 GetSpawnPosition(int index, int total)
    {
        // Inspector 에 스폰 포인트가 있으면 우선 사용
        if (spawnPoints != null && index < spawnPoints.Length)
            return new Vector3(spawnPoints[index].position.x, spawnY, spawnPoints[index].position.z);

        // 없으면 원형으로 자동 배치
        float angle  = index * (360f / total) * Mathf.Deg2Rad;
        float radius = 5f;
        return new Vector3(Mathf.Sin(angle) * radius, spawnY, Mathf.Cos(angle) * radius);
    }

    public IReadOnlyList<GameObject> Players => _players;
}
