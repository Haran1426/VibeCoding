using UnityEngine;

/// <summary>
/// 게임 씬의 진입점. 플레이어, 적 스포너, 매니저들을 씬에 배치합니다.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject waveSpawnerPrefab;
    public GameObject gameManagerPrefab;
    public GameObject abilityManagerPrefab;
    public GameObject hudPrefab;

    void Awake()
    {
        // 필수 매니저 생성 (prefab 없어도 fallback 생성)
        EnsureManager<GameManager>("GameManager", gameManagerPrefab);
        EnsureManager<AbilityManager>("AbilityManager", abilityManagerPrefab);
        EnsureManager<WaveSpawner>("WaveSpawner", waveSpawnerPrefab);
    }

    void Start()
    {
        // 플레이어 스폰
        if (playerPrefab != null && GameObject.FindGameObjectWithTag("Player") == null)
        {
            GameObject player = Instantiate(playerPrefab, Vector3.up * 0.5f, Quaternion.identity);
            player.tag = "Player";
        }

        // 카메라에 CameraFollow 붙이기
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraFollow>() == null)
        {
            cam.gameObject.AddComponent<CameraFollow>();
        }
    }

    void EnsureManager<T>(string objName, GameObject prefab) where T : MonoBehaviour
    {
        if (FindObjectOfType<T>() != null) return;
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            new GameObject(objName).AddComponent<T>();
        }
    }
}
