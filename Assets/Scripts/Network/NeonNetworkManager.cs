using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkManager 확장.
/// 플레이어 프리팹 스폰 + 씬 관리를 담당합니다.
/// </summary>
public class NeonNetworkManager : NetworkManager
{
    public static NeonNetworkManager Net => Singleton as NeonNetworkManager;

    [Header("스폰 포인트")]
    [SerializeField] private Transform[] spawnPoints;

    private int _spawnIndex;

    // ── 스폰 포인트 주입 (SceneBootstrapper 가 ArenaScene 로드 후 호출) ──
    public void SetSpawnPoints(Transform[] points)
    {
        spawnPoints = points;
        _spawnIndex = 0;
    }

    // ── 서버: 플레이어 연결 시 스폰 위치 배정 ─────────────────
    public Vector3 GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return new Vector3(Random.Range(-6f, 6f), 1f, Random.Range(-6f, 6f));
        Vector3 pos = spawnPoints[_spawnIndex % spawnPoints.Length].position;
        _spawnIndex++;
        return pos;
    }
}
