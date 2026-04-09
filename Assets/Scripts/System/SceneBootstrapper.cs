using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ArenaScene 전용 초기화.
/// NetworkManager(DontDestroyOnLoad)가 MenuScene에서 넘어온 뒤 스폰 포인트를 연결합니다.
/// 서버라면 MatchNetworkManager를 씬에서 찾아 NetworkObject를 스폰합니다.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        // 스폰 포인트를 NeonNetworkManager 에 주입
        if (NeonNetworkManager.Net != null && spawnPoints != null && spawnPoints.Length > 0)
            NeonNetworkManager.Net.SetSpawnPoints(spawnPoints);

        // 서버: MatchNetworkManager 스폰
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var mnm = FindFirstObjectByType<MatchNetworkManager>();
            if (mnm != null && !mnm.IsSpawned)
                mnm.GetComponent<NetworkObject>().Spawn();
        }
    }
}
