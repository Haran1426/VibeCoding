using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: 분신 생성과 풀링만 담당합니다.
/// 플레이어 사망 시 기록을 받아 분신을 스폰합니다.
/// </summary>
public class CloneManager : MonoBehaviour
{
    public static CloneManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private int        maxClones  = 8;
    [SerializeField] private Transform[] spawnPoints;

    // 분신 색상 순환 (GDD: 네온 계열)
    private static readonly Color[] CloneColors =
    {
        new Color(0.2f, 0.2f, 0.9f, 0.5f),
        new Color(0.9f, 0.1f, 0.5f, 0.5f),
        new Color(0.6f, 0.1f, 0.9f, 0.5f),
        new Color(0.1f, 0.8f, 0.9f, 0.5f),
    };

    private readonly List<CloneController> _activeClones  = new List<CloneController>();
    private readonly Queue<CloneController> _pool          = new Queue<CloneController>();
    private int _colorIndex;
    private int _cloneIdCounter = 100; // 분신 ID는 100부터 (플레이어는 0~99)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => EventBus.OnEntityDied += OnEntityDied;
    void OnDisable() => EventBus.OnEntityDied -= OnEntityDied;

    private void OnEntityDied(int entityId, Vector3 position, int hitBy)
    {
        // 분신 사망 — 풀로 반환
        var dead = _activeClones.Find(c => c.GetComponent<PlayerStats>().playerId == entityId);
        if (dead != null)
        {
            _activeClones.Remove(dead);
            ReturnToPool(dead);
            return;
        }

        // 실제 플레이어(id 0~99) 사망 → 분신 생성은 RespawnManager 가 전달해줌
    }

    /// <summary>
    /// RespawnManager 가 플레이어 사망 시 기록을 넘겨주면 호출합니다.
    /// </summary>
    public void SpawnClone(List<InputFrame> frames)
    {
        if (frames == null || frames.Count == 0) return;

        // 최대 수 초과 시 가장 오래된 분신 제거
        while (_activeClones.Count >= maxClones)
        {
            var oldest = _activeClones[0];
            _activeClones.RemoveAt(0);
            ReturnToPool(oldest);
        }

        CloneController clone = GetFromPool();
        if (clone == null) return;

        Vector3 spawnPos = GetSpawnPoint();
        Color   color    = CloneColors[_colorIndex % CloneColors.Length];
        int     id       = _cloneIdCounter++;
        _colorIndex++;

        clone.Init(frames, spawnPos, id, color);
        _activeClones.Add(clone);

        EventBus.RaiseCloneSpawned(_activeClones.Count);
        AudioManager.Instance?.PlayCloneSpawn();
    }

    // ── 풀 관리 ──────────────────────────────────────────────
    private CloneController GetFromPool()
    {
        if (_pool.Count > 0)
        {
            var c = _pool.Dequeue();
            return c;
        }
        if (clonePrefab == null) return null;
        GameObject go = Instantiate(clonePrefab);
        go.SetActive(false);
        return go.GetComponent<CloneController>();
    }

    private void ReturnToPool(CloneController clone)
    {
        clone.gameObject.SetActive(false);
        _pool.Enqueue(clone);
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
    }

    public int ActiveCloneCount => _activeClones.Count;
}
