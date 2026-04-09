using System.Collections;
using UnityEngine;

/// <summary>
/// SRP: 플레이어 리스폰만 담당합니다.
/// [버그3 픽스] Rigidbody null 체크 추가.
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
        if (entityId != 0) return;
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

        // [버그3 픽스] null 체크
        var rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        playerObject.SetActive(true);
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
