using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 UI 요소 제어 담당.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── 상단 HUD ─────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private Image[] starImages;         // 별 3개

    [Header("Pop Score")]
    [SerializeField] private TextMeshProUGUI popScoreText;  // 획득 점수 팝업

    // ── 결과 패널 ─────────────────────────────────────────────
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private Image[] resultStarImages;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    [Header("All Clear")]
    [SerializeField] private GameObject allClearPanel;
    [SerializeField] private TextMeshProUGUI allClearBestText;

    // ── 별 색상 ───────────────────────────────────────────────
    [Header("Star Colors")]
    [SerializeField] private Color starOnColor  = new Color(1f, 0.85f, 0f);
    [SerializeField] private Color starOffColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private int _displayedScore;

    // ─────────────────────────────────────────────────────────
    public void SetLevelTitle(string title)
    {
        if (levelTitleText) levelTitleText.text = title;
    }

    public void UpdateScore(int score)
    {
        StopCoroutine("AnimateScore");
        StartCoroutine(AnimateScore(_displayedScore, score, 0.4f));
    }

    public void UpdateStars(int stars)
    {
        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
            if (starImages[i]) starImages[i].color = i < stars ? starOnColor : starOffColor;
    }

    public void ShowPopScore(int gained)
    {
        if (!popScoreText) return;
        StopCoroutine("PopScoreAnim");
        StartCoroutine(PopScoreAnim($"+{gained}"));
    }

    public void HideResultPanel()
    {
        if (resultPanel) resultPanel.SetActive(false);
        if (allClearPanel) allClearPanel.SetActive(false);
    }

    public void ShowResultPanel(bool win, int score, int stars, bool hasNext)
    {
        if (!resultPanel) return;
        resultPanel.SetActive(true);

        if (resultTitleText) resultTitleText.text = win ? "CLEAR!" : "GAME OVER";
        if (resultTitleText) resultTitleText.color = win
            ? new Color(0.3f, 0.9f, 0.4f)
            : new Color(0.9f, 0.3f, 0.3f);

        if (resultScoreText) resultScoreText.text = $"SCORE  {score:N0}";

        if (resultStarImages != null)
            for (int i = 0; i < resultStarImages.Length; i++)
                if (resultStarImages[i])
                    resultStarImages[i].color = i < stars ? starOnColor : starOffColor;

        if (nextButton) nextButton.gameObject.SetActive(win && hasNext);

        StartCoroutine(ResultPanelAnim());
    }

    public void ShowAllClearScreen(int bestScore)
    {
        if (allClearPanel) allClearPanel.SetActive(true);
        if (allClearBestText) allClearBestText.text = $"BEST  {bestScore:N0}";
    }

    // ─────────────────────────────────────────────────────────
    // 코루틴
    // ─────────────────────────────────────────────────────────

    private IEnumerator AnimateScore(int from, int to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            int cur = Mathf.RoundToInt(Mathf.Lerp(from, to, t / duration));
            _displayedScore = cur;
            if (scoreText) scoreText.text = cur.ToString("N0");
            yield return null;
        }
        _displayedScore = to;
        if (scoreText) scoreText.text = to.ToString("N0");

        int best = GameManager.Instance ? GameManager.Instance.BestScore : to;
        if (bestScoreText) bestScoreText.text = $"BEST  {best:N0}";
    }

    private IEnumerator PopScoreAnim(string text)
    {
        if (!popScoreText) yield break;
        popScoreText.text = text;
        popScoreText.gameObject.SetActive(true);

        Vector3 startPos = popScoreText.rectTransform.anchoredPosition;
        Color c = popScoreText.color;

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float ratio = t / 0.7f;
            popScoreText.rectTransform.anchoredPosition =
                startPos + new Vector3(0, Mathf.Lerp(0, 60f, ratio), 0);
            c.a = Mathf.Lerp(1f, 0f, ratio > 0.5f ? (ratio - 0.5f) / 0.5f : 0f);
            popScoreText.color = c;
            yield return null;
        }

        popScoreText.rectTransform.anchoredPosition = startPos;
        c.a = 1f;
        popScoreText.color = c;
        popScoreText.gameObject.SetActive(false);
    }

    private IEnumerator ResultPanelAnim()
    {
        if (!resultPanel) yield break;
        RectTransform rt = resultPanel.GetComponent<RectTransform>();
        rt.localScale = Vector3.zero;

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float s = EaseOutBack(Mathf.Clamp01(t / 0.35f));
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
