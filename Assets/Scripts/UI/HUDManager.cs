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

    void OnEnable()
    {
        EventBus.OnMatchStateChanged += OnMatchState;
        EventBus.OnScoreChanged      += OnScoreChanged;
        EventBus.OnKnockbackChanged  += OnKnockbackChanged;
        EventBus.OnCloneSpawned      += OnCloneSpawned;
    }

    // [버그7 픽스] CancelInvoke 추가해 씬 언로드 시 오류 방지
    void OnDisable()
    {
        CancelInvoke(nameof(HideCountdown));
        EventBus.OnMatchStateChanged -= OnMatchState;
        EventBus.OnScoreChanged      -= OnScoreChanged;
        EventBus.OnKnockbackChanged  -= OnKnockbackChanged;
        EventBus.OnCloneSpawned      -= OnCloneSpawned;
    }

    void Update()
    {
        if (MatchManager.Instance == null) return;
        if (timerText != null)
            timerText.text = MatchManager.Instance.GetFormattedTime();
    }

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

    private void OnScoreChanged(int playerId, int score)
    {
        if (playerId != 0) return;
        if (scoreText != null) scoreText.text = "SCORE  " + score;
    }

    private void OnKnockbackChanged(int entityId, float pct)
    {
        if (entityId != 0) return;
        if (knockbackText != null)
            knockbackText.text = Mathf.RoundToInt(pct) + "%";
        if (knockbackFill != null)
            knockbackFill.fillAmount = Mathf.Clamp01(pct / 200f);
    }

    private void OnCloneSpawned(int count)
    {
        if (cloneCountText != null)
            cloneCountText.text = "CLONES  " + count;
    }
}
