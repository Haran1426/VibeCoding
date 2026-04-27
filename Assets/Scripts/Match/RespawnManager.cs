using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// SRP: 싱글플레이어 리스폰만 담당합니다.
///
/// 멀티플레이어에서는 PlayerNetworkSync.DiedServerRpc 가 리스폰을 처리합니다.
/// NetworkManager.IsListening 이 true 이면 이 매니저는 아무것도 하지 않습니다.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] private float       respawnDelay = 2f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject  playerObject;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => EventBus.OnEntityDied += OnEntityDied;
    void OnDisable() => EventBus.OnEntityDied -= OnEntityDied;

    private void OnEntityDied(int entityId, Vector3 pos, int hitBy)
    {
        // 멀티플레이어 중에는 PlayerNetworkSync 가 처리 — 여기서는 건드리지 않음
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        // 싱글: 플레이어(ID 0~99)만 처리. 분신 사망은 CloneManager 가 처리.
        if (entityId >= 100) return;
        if (entityId != (playerObject?.GetComponent<PlayerStats>()?.playerId ?? 0)) return;
        if (MatchManager.Instance?.CurrentState != MatchState.Playing) return;

        var recorder = playerObject?.GetComponent<InputRecorder>();
        if (recorder != null)
        {
            CloneManager.Instance?.SpawnClone(recorder.GetRecording());
            recorder.ClearRecording();
        }

        StartCoroutine(DoRespawn());
    }

    private IEnumerator DoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (MatchManager.Instance?.CurrentState != MatchState.Playing) yield break;
        if (playerObject == null) yield break;

        playerObject.transform.position = GetSpawnPoint();
        playerObject.GetComponent<PlayerStats>()?.ResetKnockback();
        playerObject.GetComponent<DeathDetector>()?.ResetDead();
        playerObject.GetComponent<InputRecorder>()?.ClearRecording();

        var rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        playerObject.SetActive(true);

        // 리스폰 무적 (1.5초)
        playerObject.GetComponent<PlayerStats>()?.StartInvincibility(1.5f);

        AudioManager.Instance?.PlayRespawn();
        VFXManager.Instance?.PlayLevelUp(playerObject.transform.position);
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return new Vector3(0f, 1f, 0f);
    }
}
