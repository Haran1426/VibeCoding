using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// SRP: 싱글플레이어 점수 집계만 담당합니다.
///
/// 멀티플레이어에서는 PlayerNetworkSync.NetScore (NetworkVariable) 가 권위 있는 점수이며
/// 이 시스템은 아무것도 처리하지 않습니다.
///
/// 싱글에서만: RegisterHit(+1), OnEntityDied(+5), 최고점수 PlayerPrefs 저장.
/// </summary>
public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance { get; private set; }

    private readonly Dictionary<int, int> _scores = new Dictionary<int, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => EventBus.OnEntityDied += OnEntityDied;
    void OnDisable() => EventBus.OnEntityDied -= OnEntityDied;

    /// <summary>공격 적중 시 소점수 (+1). 싱글 전용.</summary>
    public void RegisterHit(int attackerId)
    {
        if (IsMultiplayer()) return;
        AddScore(attackerId, 1);
    }

    private void OnEntityDied(int victimId, Vector3 pos, int killerId)
    {
        if (IsMultiplayer()) return;
        if (killerId >= 0)
            AddScore(killerId, 5);
    }

    private void AddScore(int playerId, int amount)
    {
        if (!_scores.ContainsKey(playerId)) _scores[playerId] = 0;
        _scores[playerId] += amount;

        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (_scores[playerId] > best)
        {
            PlayerPrefs.SetInt("BestScore", _scores[playerId]);
            PlayerPrefs.Save();
        }

        EventBus.RaiseScoreChanged(playerId, _scores[playerId]);
    }

    public int GetScore(int playerId) =>
        _scores.TryGetValue(playerId, out int s) ? s : 0;

    public Dictionary<int, int> GetAllScores() =>
        new Dictionary<int, int>(_scores);

    private static bool IsMultiplayer() =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
}
