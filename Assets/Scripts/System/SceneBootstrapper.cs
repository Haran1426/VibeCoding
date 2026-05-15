using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Arena scene startup glue. Repairs arena basics, wires spawn points, and spawns
/// the server-authoritative match controller for online matches.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        EnsureCoreManagers();
        EnsureRuntimeUIManagers();

        Transform[] runtimeSpawnPoints = ArenaMapRuntimeBuilder.EnsureArena();
        if ((spawnPoints == null || spawnPoints.Length == 0) &&
            runtimeSpawnPoints != null && runtimeSpawnPoints.Length > 0)
            spawnPoints = runtimeSpawnPoints;

        if (NeonNetworkManager.Net != null && spawnPoints != null && spawnPoints.Length > 0)
            NeonNetworkManager.Net.SetSpawnPoints(spawnPoints);

        RespawnManager.Instance?.SetSpawnPoints(spawnPoints);
        CloneManager.Instance?.SetSpawnPoints(spawnPoints);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            EnsureNetworkMatchManager();
        }
    }

    private static void EnsureCoreManagers()
    {
        if (FindFirstObjectByType<AudioManager>() == null)
            new GameObject("AudioManager_Runtime").AddComponent<AudioManager>();

        if (FindFirstObjectByType<VFXManager>() == null)
            new GameObject("VFXManager_Runtime").AddComponent<VFXManager>();

        if (FindFirstObjectByType<HitStopManager>() == null)
            new GameObject("HitStopManager_Runtime").AddComponent<HitStopManager>();

        if (FindFirstObjectByType<GameFeelDirector>() == null)
            new GameObject("GameFeelDirector_Runtime").AddComponent<GameFeelDirector>();
    }

    private static void EnsureRuntimeUIManagers()
    {
        var canvas = GameObject.Find("Arena HUD Canvas")?.GetComponent<Canvas>();
        if (canvas == null)
            canvas = RuntimeUIFactory.EnsureCanvas("Arena Runtime Canvas");

        var root = GameObject.Find("RuntimeUIManagers");
        if (root == null)
        {
            root = new GameObject("RuntimeUIManagers");
            root.transform.SetParent(canvas.transform, false);
        }

        EnsureComponent<HUDManager>(root);
        EnsureComponent<ScoreboardUI>(root);
        EnsureComponent<KillFeedUI>(root);
        EnsureComponent<PauseManager>(root);
        EnsureComponent<ResultsPanel>(root);
    }

    private static void EnsureComponent<T>(GameObject root) where T : Component
    {
        if (FindFirstObjectByType<T>() != null) return;
        root.AddComponent<T>();
    }

    private static void EnsureNetworkMatchManager()
    {
        var mnm = FindFirstObjectByType<MatchNetworkManager>();
        if (mnm == null)
        {
            var go = new GameObject("MatchNetworkManager_Runtime");
            go.AddComponent<NetworkObject>();
            mnm = go.AddComponent<MatchNetworkManager>();
        }

        var netObj = mnm.GetComponent<NetworkObject>();
        if (netObj == null)
            netObj = mnm.gameObject.AddComponent<NetworkObject>();

        if (!mnm.IsSpawned)
            netObj.Spawn();
    }
}
