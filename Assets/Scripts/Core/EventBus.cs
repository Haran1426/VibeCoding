using System;
using System.Collections.Generic;

/// <summary>
/// Neon Rewind Arena 전용 이벤트 채널.
/// 씬 전환 시 Clear() 를 호출해 메모리 누수를 방지합니다.
/// </summary>
public static class EventBus
{
    // ── Match ────────────────────────────────────────────────
    public static event Action<MatchState>          OnMatchStateChanged;
    public static event Action                      OnMatchStarted;
    public static event Action<Dictionary<int,int>> OnMatchEnded;   // playerId → score

    // ── Entity 사망 ──────────────────────────────────────────
    // (entityId, deathPosition, lastHitByPlayerId)
    public static event Action<int, UnityEngine.Vector3, int> OnEntityDied;

    // ── 점수 ─────────────────────────────────────────────────
    public static event Action<int, int> OnScoreChanged;   // (playerId, newScore)

    // ── 넉백 ─────────────────────────────────────────────────
    public static event Action<int, float> OnKnockbackChanged; // (entityId, knockbackPct)

    // ── 분신 ─────────────────────────────────────────────────
    public static event Action<int> OnCloneSpawned;   // cloneCount

    // ── Raise helpers ────────────────────────────────────────
    public static void RaiseMatchStateChanged(MatchState s)           => OnMatchStateChanged?.Invoke(s);
    public static void RaiseMatchStarted()                            => OnMatchStarted?.Invoke();
    public static void RaiseMatchEnded(Dictionary<int,int> scores)    => OnMatchEnded?.Invoke(scores);
    public static void RaiseEntityDied(int id, UnityEngine.Vector3 p, int hitBy) => OnEntityDied?.Invoke(id, p, hitBy);
    public static void RaiseScoreChanged(int id, int score)           => OnScoreChanged?.Invoke(id, score);
    public static void RaiseKnockbackChanged(int id, float pct)       => OnKnockbackChanged?.Invoke(id, pct);
    public static void RaiseCloneSpawned(int count)                   => OnCloneSpawned?.Invoke(count);

    public static void Clear()
    {
        OnMatchStateChanged = null;
        OnMatchStarted      = null;
        OnMatchEnded        = null;
        OnEntityDied        = null;
        OnScoreChanged      = null;
        OnKnockbackChanged  = null;
        OnCloneSpawned      = null;
    }
}
