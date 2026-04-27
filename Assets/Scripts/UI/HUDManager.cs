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

    [Header("점수 (ScoreboardUI 로 대체됨 — 싱글 폴백용으로만 사용)")]
    [SerializeField] private TextMeshProUGUI scoreText;   // 비워도 무방

    [Header("넉백 %")]
    [SerializeField] private TextMeshProUGUI knockbackText;
    [SerializeField] private Image           knockbackFill;

    [Header("분신 수")]
    [SerializeField] private TextMeshProUGUI cloneCountText;

    [Header("카운트다운")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("리스폰 대기")]
    [SerializeField] private TextMeshProUGUI respawnText;
    [SerializeField] private float           respawnDelay = 2f;

    // ── 넉백 색상 단계 ──────────────────────────────────────
    private static readonly Color KbNormal  = Color.white;
    private static readonly Color KbWarning = new Color(1f, 0.90f, 0.15f);  // 50 %
    private static readonly Color KbDanger  = new Color(1f, 0.45f, 0.10f);  // 100 %
    private static readonly Color KbCrit    = new Color(1f, 0.15f, 0.15f);  // 150 %

    // ── 로컬 플레이어 ID ────────────────────────────────────
    private int  _localPlayerId;
    private bool _idResolved;

    // 멀티플레이어: 0.5초 간격으로 재시도, 10초 이후 포기
    private const float IdRetryInterval = 0.5f;
    private const float IdRetryTimeout  = 10f;
    private float _idRetryTimer;
    private float _idRetryElapsed;

    // ── 리스폰 코루틴 ───────────────────────────────────────
    private Coroutine _respawnCoroutine;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        _localPlayerId = 0;
        _idResolved    = false;

        if (respawnText != null) respawnText.gameObject.SetActive(false);
    }

    void Start()
    {
        // 싱글은 Awake 기본값(0)으로 충분. 멀티는 주기적으로 재시도.
        TryResolvePlayerId();
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
        // 멀티 플레이어 ID 지연 해석: 일정 간격으로만 탐색
        if (!_idResolved && _idRetryElapsed < IdRetryTimeout)
        {
            _idRetryTimer   += Time.deltaTime;
            _idRetryElapsed += Time.deltaTime;

            if (_idRetryTimer >= IdRetryInterval)
            {
                _idRetryTimer = 0f;
                TryResolvePlayerId();
            }
        }

        if (timerText == null) return;

        if (MatchManager.Instance != null)
            timerText.text = MatchManager.Instance.GetFormattedTime();
        else if (MatchNetworkManager.Instance != null)
            timerText.text = MatchNetworkManager.Instance.GetFormattedTime();
    }

    private void TryResolvePlayerId()
    {
        var netSync = FindFirstObjectByType<PlayerNetworkSync>();
        if (netSync != null && netSync.IsSpawned && netSync.IsOwner)
        {
            _localPlayerId = (int)netSync.OwnerClientId;
            _idResolved    = true;
        }
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
        // ScoreboardUI 가 있으면 그쪽에서 처리 — 폴백으로만 동작
        if (playerId != _localPlayerId) return;
        if (scoreText != null) scoreText.text = "SCORE  " + score;
    }

    private void OnKnockbackChanged(int entityId, float pct)
    {
        if (entityId != _localPlayerId) return;

        if (knockbackText != null)
        {
            knockbackText.text  = Mathf.RoundToInt(pct) + "%";
            knockbackText.color = KnockbackColor(pct);
        }

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
