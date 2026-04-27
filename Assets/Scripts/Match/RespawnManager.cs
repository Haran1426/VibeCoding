using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// SRP: 싱글/로컬 멀티플레이어 리스폰만 담당합니다.
///
/// - 싱글:       playerObjects[0] 만 처리
/// - 로컬 멀티:  playerObjects 리스트에서 playerId 로 찾아 처리
/// - 멀티플레이: NetworkManager.IsListening 이 true 이면 아무것도 하지 않습니다.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform[] spawnPoints;

    // 싱글 호환용 (Inspector 직접 연결)
    [SerializeField] private GameObject playerObject;

    // 로컬 멀티: LocalArenaBootstrapper 에서 RegisterPlayer 호출
    private readonly List<GameObject> _localPlayers = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerObject != null)
            _localPlayers.Add(playerObject);
    }

    void OnEnable()  => EventBus.OnEntityDied += OnEntityDied;
    void OnDisable() => EventBus.OnEntityDied -= OnEntityDied;

    /// <summary>LocalArenaBootstrapper 에서 호출해 플레이어를 등록합니다.</summary>
    public void RegisterPlayer(GameObject player)
    {
        if (player != null && !_localPlayers.Contains(player))
            _localPlayers.Add(player);
    }

    public void ClearPlayers() => _localPlayers.Clear();

    private void OnEntityDied(int entityId, Vector3 pos, int hitBy)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;
        if (entityId >= 100) return; // 분신은 CloneManager 가 처리
        if (MatchManager.Instance?.CurrentState != MatchState.Playing) return;

        var target = FindPlayer(entityId);
        if (target == null) return;

        var recorder = target.GetComponent<InputRecorder>();
        if (recorder != null)
        {
            CloneManager.Instance?.SpawnClone(recorder.GetRecording());
            recorder.ClearRecording();
        }

        StartCoroutine(DoRespawn(target));
    }

    private IEnumerator DoRespawn(GameObject playerGO)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (MatchManager.Instance?.CurrentState != MatchState.Playing) yield break;
        if (playerGO == null) yield break;

        playerGO.transform.position = GetSpawnPoint();
        playerGO.GetComponent<PlayerStats>()?.ResetKnockback();
        playerGO.GetComponent<DeathDetector>()?.ResetDead();
        playerGO.GetComponent<InputRecorder>()?.ClearRecording();

        var rb = playerGO.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        playerGO.SetActive(true);
        playerGO.GetComponent<PlayerStats>()?.StartInvincibility(1.5f);

        AudioManager.Instance?.PlayRespawn();
        VFXManager.Instance?.PlayLevelUp(playerGO.transform.position);
    }

    private GameObject FindPlayer(int playerId)
    {
        foreach (var p in _localPlayers)
        {
            if (p == null) continue;
            var stats = p.GetComponent<PlayerStats>();
            if (stats != null && stats.playerId == playerId) return p;
        }
        return null;
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return new Vector3(0f, 2.5f, 0f);
    }
}
