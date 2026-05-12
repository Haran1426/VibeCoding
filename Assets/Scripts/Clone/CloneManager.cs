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

    // [버그6 픽스] 재생 완료된 분신 자동 제거
    void Update()
    {
        for (int i = _activeClones.Count - 1; i >= 0; i--)
        {
            if (_activeClones[i].IsReplayFinished)
            {
                ReturnToPool(_activeClones[i]);
                _activeClones.RemoveAt(i);
                EventBus.RaiseCloneSpawned(_activeClones.Count);
            }
        }
    }

    private void OnEntityDied(int entityId, Vector3 position, int hitBy)
    {
        // 분신 사망 — 풀로 반환
        var dead = _activeClones.Find(c => c.GetComponent<PlayerStats>().playerId == entityId);
        if (dead != null)
        {
            _activeClones.Remove(dead);
            ReturnToPool(dead);
            EventBus.RaiseCloneSpawned(_activeClones.Count);
            return;
        }

        // 실제 플레이어(id 0~99) 사망 → 분신 생성은 RespawnManager 가 전달해줌
    }

    /// <summary>
    /// RespawnManager 가 플레이어 사망 시 기록을 넘겨주면 호출합니다.
    /// </summary>
    public void SpawnClone(List<InputFrame> frames)
    {
        SpawnClone(frames, GetSpawnPoint(), _cloneIdCounter);
    }

    public void SpawnClone(List<InputFrame> frames, Vector3 spawnPos)
    {
        SpawnClone(frames, spawnPos, _cloneIdCounter);
    }

    public void SpawnClone(List<InputFrame> frames, Vector3 spawnPos, int cloneId)
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

        Color   color    = CloneColors[_colorIndex % CloneColors.Length];
        _colorIndex++;

        if (cloneId >= _cloneIdCounter)
            _cloneIdCounter = cloneId + 1;

        clone.Init(frames, spawnPos, cloneId, color);
        _activeClones.Add(clone);

        EventBus.RaiseCloneSpawned(_activeClones.Count);
        VFXManager.Instance?.PlayCloneSpawn(spawnPos);
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
        GameObject go = clonePrefab != null
            ? Instantiate(clonePrefab)
            : CreateRuntimeClone();
        go.SetActive(false);
        return go.GetComponent<CloneController>();
    }

    private static GameObject CreateRuntimeClone()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "RuntimeClone";
        go.SetActive(false);

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.linearDamping = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (go.GetComponent<PlayerStats>() == null) go.AddComponent<PlayerStats>();
        if (go.GetComponent<PlayerController>() == null) go.AddComponent<PlayerController>();
        if (go.GetComponent<DeathDetector>() == null) go.AddComponent<DeathDetector>();
        if (go.GetComponent<KnockbackReceiver>() == null) go.AddComponent<KnockbackReceiver>();
        if (go.GetComponent<CloneController>() == null) go.AddComponent<CloneController>();

        int cloneLayer = LayerMask.NameToLayer("Clone");
        if (cloneLayer >= 0) go.layer = cloneLayer;

        return go;
    }

    private void ReturnToPool(CloneController clone)
    {
        clone.PrepareForPool();
        clone.gameObject.SetActive(false);
        _pool.Enqueue(clone);
    }

    private Vector3 GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        return new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
    }

    public void SetSpawnPoints(Transform[] points)
    {
        if (points == null || points.Length == 0) return;
        spawnPoints = points;
    }

    public int ActiveCloneCount => _activeClones.Count;
}
