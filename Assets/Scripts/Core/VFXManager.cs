using UnityEngine;

/// <summary>
/// 씬 전체 파티클 이펙트를 중앙 관리합니다.
/// 프리팹이 없으면 코드로 파티클 시스템을 즉석 생성합니다.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs (선택 - 없으면 자동 생성)")]
    [SerializeField] private GameObject bulletHitPrefab;
    [SerializeField] private GameObject enemyDeathPrefab;
    [SerializeField] private GameObject expOrbAbsorbPrefab;
    [SerializeField] private GameObject levelUpPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────

    // ── Neon Rewind Arena 전용 ──────────────────────────────
    public void PlayAttack(Vector3 pos)
        => SpawnBurst(pos, new Color(1f, 0.9f, 0.1f), 10, 0.1f, 5f, 0.15f);

    // ── 기존 호환 ────────────────────────────────────────────
    public void PlayBulletHit(Vector3 pos, Color color)
    {
        if (bulletHitPrefab != null)
            Spawn(bulletHitPrefab, pos);
        else
            SpawnBurst(pos, color, 8, 0.12f, 4f, 0.25f);
    }

    public void PlayEnemyDeath(Vector3 pos, Color color)
    {
        if (enemyDeathPrefab != null)
            Spawn(enemyDeathPrefab, pos);
        else
            SpawnBurst(pos, color, 20, 0.18f, 6f, 0.5f);
    }

    public void PlayExpOrbAbsorb(Vector3 pos)
    {
        if (expOrbAbsorbPrefab != null)
            Spawn(expOrbAbsorbPrefab, pos);
        else
            SpawnBurst(pos, new Color(0f, 1f, 0.5f), 6, 0.1f, 3f, 0.2f);
    }

    public void PlayLevelUp(Vector3 pos)
    {
        if (levelUpPrefab != null)
            Spawn(levelUpPrefab, pos);
        else
            SpawnRing(pos, new Color(0.2f, 0.9f, 1f), 30, 0.15f, 8f, 0.6f);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────

    private static void Spawn(GameObject prefab, Vector3 pos)
    {
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(go, 3f);
    }

    /// <summary>원형 방향으로 파티클을 버스트합니다.</summary>
    private static void SpawnBurst(Vector3 pos, Color color, int count,
                                   float size, float speed, float lifetime)
    {
        GameObject root = new GameObject("VFX_Burst");
        root.transform.position = pos;
        ParticleSystem ps = root.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime  = lifetime;
        main.startSpeed     = speed;
        main.startSize      = size;
        main.startColor     = color;
        main.gravityModifier = 0f;
        main.loop           = false;
        main.playOnAwake    = false;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        SetNeonRenderer(ps, color);

        var burst = new ParticleSystem.Burst(0f, (short)count);
        emission.enabled = true;
        emission.SetBursts(new[] { burst });
        ps.Play();

        Destroy(root, lifetime + 0.2f);
    }

    /// <summary>링 형태로 파티클을 방출합니다 (레벨업용).</summary>
    private static void SpawnRing(Vector3 pos, Color color, int count,
                                  float size, float speed, float lifetime)
    {
        GameObject root = new GameObject("VFX_Ring");
        root.transform.position = pos;
        ParticleSystem ps = root.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime  = lifetime;
        main.startSpeed     = speed;
        main.startSize      = size;
        main.startColor     = color;
        main.gravityModifier = 0f;
        main.loop           = false;
        main.playOnAwake    = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.5f;

        SetNeonRenderer(ps, color);

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ps.Play();

        Destroy(root, lifetime + 0.2f);
    }

    private static void SetNeonRenderer(ParticleSystem ps, Color color)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        mat.SetColor("_Color", color);
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
