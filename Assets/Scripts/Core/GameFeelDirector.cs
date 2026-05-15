using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Centralizes non-authoritative feedback beats: kill emphasis, match-end slow motion,
/// camera shake, VFX, and layered SFX. Gameplay state stays in match/network systems.
/// </summary>
public class GameFeelDirector : MonoBehaviour
{
    public static GameFeelDirector Instance { get; private set; }

    [Header("Kill Pop")]
    [SerializeField] private float killShakeDuration = 0.18f;
    [SerializeField] private float killShakeMagnitude = 0.32f;
    [SerializeField] private float offlineKillSlowMoScale = 0.22f;
    [SerializeField] private float offlineKillSlowMoDuration = 0.12f;

    [Header("Match End")]
    [SerializeField] private float endSlowMoScale = 0.16f;
    [SerializeField] private float endSlowMoHold = 0.38f;
    [SerializeField] private float endRecoveryDuration = 0.45f;
    [SerializeField] private float endShakeDuration = 0.42f;
    [SerializeField] private float endShakeMagnitude = 0.55f;

    private Coroutine _slowMoRoutine;
    private int _eventBusClearVersion = -1;

    private static bool IsOnlineMatch =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SubscribeEventBus();

    void OnDisable()
    {
        EventBus.OnEntityDied -= OnEntityDied;
        EventBus.OnMatchEnded -= OnMatchEnded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_eventBusClearVersion != EventBus.ClearVersion)
            SubscribeEventBus();
    }

    private void SubscribeEventBus()
    {
        EventBus.OnEntityDied -= OnEntityDied;
        EventBus.OnMatchEnded -= OnMatchEnded;

        EventBus.OnEntityDied += OnEntityDied;
        EventBus.OnMatchEnded += OnMatchEnded;

        _eventBusClearVersion = EventBus.ClearVersion;
    }

    private void OnEntityDied(int victimId, Vector3 position, int killerId)
    {
        bool isClone = victimId >= 100;
        if (isClone)
        {
            ArenaCamera.Instance?.Shake(killShakeDuration * 0.55f, killShakeMagnitude * 0.35f);
            return;
        }

        bool creditedKill = killerId >= 0 && killerId != victimId;

        VFXManager.Instance?.PlayFinisher(position, killerId, creditedKill);
        ArenaCamera.Instance?.Shake(killShakeDuration, creditedKill ? killShakeMagnitude : killShakeMagnitude * 0.55f);

        if (creditedKill)
            AudioManager.Instance?.PlayFinisher();
        else
            AudioManager.Instance?.PlayRingOut();

        if (!IsOnlineMatch && creditedKill)
            StartSlowMo(offlineKillSlowMoScale, offlineKillSlowMoDuration, 0.08f);
    }

    private void OnMatchEnded(Dictionary<int, int> scores)
    {
        Vector3 focus = GetArenaFocus();
        VFXManager.Instance?.PlayMatchEndCelebration(focus);
        ArenaCamera.Instance?.Shake(endShakeDuration, endShakeMagnitude);
        AudioManager.Instance?.PlayMatchEndStinger();

        StartSlowMo(endSlowMoScale, endSlowMoHold, endRecoveryDuration);
    }

    private void StartSlowMo(float scale, float holdSeconds, float recoverySeconds)
    {
        if (_slowMoRoutine != null)
            StopCoroutine(_slowMoRoutine);

        _slowMoRoutine = StartCoroutine(SlowMoRoutine(scale, holdSeconds, recoverySeconds));
    }

    private IEnumerator SlowMoRoutine(float scale, float holdSeconds, float recoverySeconds)
    {
        float originalScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
        Time.timeScale = Mathf.Clamp(scale, 0.05f, 1f);

        yield return new WaitForSecondsRealtime(holdSeconds);

        float elapsed = 0f;
        while (elapsed < recoverySeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = recoverySeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / recoverySeconds);
            Time.timeScale = Mathf.Lerp(scale, originalScale, t);
            yield return null;
        }

        Time.timeScale = originalScale;
        _slowMoRoutine = null;
    }

    private static Vector3 GetArenaFocus()
    {
        var players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
            return Vector3.up * 0.5f;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy) continue;
            sum += player.transform.position;
            count++;
        }

        return count > 0 ? sum / count : Vector3.up * 0.5f;
    }
}
