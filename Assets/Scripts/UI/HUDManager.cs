using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// EventBus 이벤트로만 갱신됩니다 — 싱글턴 직접 참조 없음.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("타이머")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("점수")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("넉백 %")]
    [SerializeField] private TextMeshProUGUI knockbackText;
    [SerializeField] private Image           knockbackFill;

    [Header("분신 수")]
    [SerializeField] private TextMeshProUGUI cloneCountText;

    [Header("카운트다운")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("리스폰 대기")]
    [SerializeField] private TextMeshProUGUI respawnText;   // "RESPAWNING  1.5s"
    [SerializeField] private float           respawnDelay = 2f;

    // ── 넉백 색상 단계 ──────────────────────────────────────
    private static readonly Color KbNormal  = Color.white;
    private static readonly Color KbWarning = new Color(1f, 0.90f, 0.15f);  // 50 %
    private static readonly Color KbDanger  = new Color(1f, 0.45f, 0.10f);  // 100 %
    private static readonly Color KbCrit    = new Color(1f, 0.15f, 0.15f);  // 150 %

    // ── 로컬 플레이어 ID ────────────────────────────────────
    private int  _localPlayerId;
    private bool _idResolved;      // 멀티플레이어 지연 해석용

    // ── 리스폰 코루틴 ───────────────────────────────────────
    private Coroutine _respawnCoroutine;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        // 싱글 또는 호스트: 기본값 0
        _localPlayerId = 0;
        _idResolved    = false;

        if (respawnText != null) respawnText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        EventBus.OnMatchStateChanged += OnMatchState;
        EventBus.OnScoreChanged      += OnScoreChanged;
        EventBus.OnKnockbackChanged  += OnKnockbackChanged;
        EventBus.OnCloneSpawned      += OnCloneSpawned;
        EventBus.OnEntityDied        += OnEntityDied;
    }

    void OnDisable()
    {
        CancelInvoke(nameof(HideCountdown));
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);

        EventBus.OnMatchStateChanged -= OnMatchState;
        EventBus.OnScoreChanged      -= OnScoreChanged;
        EventBus.OnKnockbackChanged  -= OnKnockbackChanged;
        EventBus.OnCloneSpawned      -= OnCloneSpawned;
        EventBus.OnEntityDied        -= OnEntityDied;
    }

    void Update()
    {
        // 멀티플레이어 OwnerClientId 지연 해석 (OnNetworkSpawn 이후에 생김)
        if (!_idResolved)
        {
            var netSync = FindFirstObjectByType<PlayerNetworkSync>();
            if (netSync != null && netSync.IsSpawned && netSync.IsOwner)
            {
                _localPlayerId = (int)netSync.OwnerClientId;
                _idResolved    = true;
            }
        }

        if (timerText == null) return;
        if (MatchManager.Instance != null)
            timerText.text = MatchManager.Instance.GetFormattedTime();
        else if (MatchNetworkManager.Instance != null)
            timerText.text = MatchNetworkManager.Instance.GetFormattedTime();
    }

    // ════════════════════════════════════════════════════════
    //  Match 상태
    // ════════════════════════════════════════════════════════
    private void OnMatchState(MatchState s)
    {
        if (countdownText == null) return;

        if (s == MatchState.Countdown)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "READY...";
        }
        else if (s == MatchState.Playing)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "FIGHT!";
            Invoke(nameof(HideCountdown), 0.8f);
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void HideCountdown()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════
    //  사망 → 리스폰 카운트다운
    // ════════════════════════════════════════════════════════
    private void OnEntityDied(int entityId, UnityEngine.Vector3 pos, int hitBy)
    {
        if (entityId != _localPlayerId) return;

        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnCountdown());
    }

    private IEnumerator RespawnCountdown()
    {
        if (respawnText == null) yield break;

        respawnText.gameObject.SetActive(true);
        float t = respawnDelay;

        while (t > 0f)
        {
            respawnText.text = $"RESPAWNING  {t:F1}s";
            yield return new WaitForSecondsRealtime(0.1f);
            t -= 0.1f;
        }

        respawnText.text = "RESPAWNING...";
        yield return new WaitForSecondsRealtime(0.3f);
        respawnText.gameObject.SetActive(false);
        _respawnCoroutine = null;
    }

    // ════════════════════════════════════════════════════════
    //  점수 / 넉백 / 분신
    // ════════════════════════════════════════════════════════
    private void OnScoreChanged(int playerId, int score)
    {
        if (playerId != _localPlayerId) return;
        if (scoreText != null) scoreText.text = "SCORE  " + score;
    }

    private void OnKnockbackChanged(int entityId, float pct)
    {
        if (entityId != _localPlayerId) return;

        // 텍스트
        if (knockbackText != null)
        {
            knockbackText.text  = Mathf.RoundToInt(pct) + "%";
            knockbackText.color = KnockbackColor(pct);
        }

        // 게이지 바
        if (knockbackFill != null)
        {
            knockbackFill.fillAmount = Mathf.Clamp01(pct / 200f);
            knockbackFill.color      = KnockbackColor(pct);
        }
    }

    private static Color KnockbackColor(float pct)
    {
        if (pct >= 150f) return KbCrit;
        if (pct >= 100f) return KbDanger;
        if (pct >=  50f) return KbWarning;
        return KbNormal;
    }

    private void OnCloneSpawned(int count)
    {
        if (cloneCountText != null)
            cloneCountText.text = "CLONES  " + count;
    }
}
