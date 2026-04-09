using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: 점수 집계만 담당합니다.
/// 적중(RegisterHit) + 처치(RegisterKill) 로 점수를 쌓습니다.
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

    /// <summary>적중 시 소점수</summary>
    public void RegisterHit(int attackerId)
    {
        AddScore(attackerId, 1);
    }

    /// <summary>처치 확정 시 큰 점수 (DeathDetector → EventBus → 여기)</summary>
    private void OnEntityDied(int victimId, Vector3 pos, int killerId)
    {
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
}
