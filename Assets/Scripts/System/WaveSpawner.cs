using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject chaserPrefab;
    public GameObject shooterPrefab;
    public GameObject bigChaserPrefab;

    [Header("Spawn Settings")]
    public float spawnRadius = 25f;
    public float spawnInterval = 1.5f;

    private Transform player;
    private float gameTime = 0f;
    private float nextSpawnTime = 0f;
    private int currentWave = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        gameTime = GameManager.Instance.survivalTime;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            float interval = Mathf.Max(0.3f, spawnInterval - gameTime * 0.01f);
            nextSpawnTime = Time.time + interval;
        }
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        Vector3 spawnPos = GetSpawnPosition();

        // 시간에 따라 적 종류 결정
        GameObject prefab = ChooseEnemyPrefab();
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 시간에 따라 스탯 강화
        float strengthMult = 1f + gameTime * 0.02f;
        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
        EnemyAI ai = enemy.GetComponent<EnemyAI>();

        if (eh != null)
        {
            eh.maxHealth *= strengthMult;
            eh.SetStats(eh.maxHealth, eh.expReward, eh.scoreReward);
        }
        if (ai != null)
            ai.moveSpeed = Mathf.Min(ai.moveSpeed * (1f + gameTime * 0.005f), 12f);
    }

    GameObject ChooseEnemyPrefab()
    {
        float t = gameTime;
        float roll = Random.value;

        if (t < 30f) return chaserPrefab;
        if (t < 60f) return roll < 0.8f ? chaserPrefab : shooterPrefab;
        if (t < 120f) return roll < 0.6f ? chaserPrefab : (roll < 0.85f ? shooterPrefab : bigChaserPrefab);
        // 120초 이후: 다 나옴
        if (roll < 0.5f) return chaserPrefab;
        if (roll < 0.75f) return shooterPrefab;
        return bigChaserPrefab != null ? bigChaserPrefab : chaserPrefab;
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 center = player != null ? player.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float r = spawnRadius + Random.Range(0f, 5f);
        return new Vector3(center.x + Mathf.Cos(angle) * r, 0.5f, center.z + Mathf.Sin(angle) * r);
    }
}
