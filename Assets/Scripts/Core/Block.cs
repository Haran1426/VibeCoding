using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 개별 블록. UI Image 기반. BoardManager가 생성·관리.
/// </summary>
public class Block : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── 색상 팔레트 ──────────────────────────────────────────
    public static readonly Color[] Palette = new Color[]
    {
        new Color(0.96f, 0.26f, 0.21f), // Red
        new Color(0.13f, 0.59f, 0.95f), // Blue
        new Color(0.30f, 0.69f, 0.31f), // Green
        new Color(1.00f, 0.76f, 0.03f), // Yellow
        new Color(0.61f, 0.15f, 0.69f), // Purple
    };

    // ── 데이터 ───────────────────────────────────────────────
    public int ColorIndex { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsMarked { get; set; }  // 제거 예정 표시

    // ── 참조 ─────────────────────────────────────────────────
    [SerializeField] private Image bgImage;
    [SerializeField] private Image glowImage;   // 반짝임 오버레이 (alpha)

    private RectTransform _rect;
    private Coroutine _moveCoroutine;

    // ── 이벤트 ───────────────────────────────────────────────
    public static event Action<Block> OnBlockClicked;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Init(int colorIndex, int row, int col)
    {
        ColorIndex = colorIndex;
        Row = row;
        Col = col;
        IsMarked = false;
        bgImage.color = Palette[colorIndex];
        if (glowImage) glowImage.color = new Color(1, 1, 1, 0);
        transform.localScale = Vector3.one;
    }

    // ── 클릭 ─────────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        OnBlockClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(Vector3.one * 1.08f, 0.08f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(Vector3.one, 0.08f));
    }

    // ── 하이라이트 ────────────────────────────────────────────
    public void SetHighlight(bool on)
    {
        if (glowImage == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeGlow(on ? 0.35f : 0f, 0.1f));
    }

    // ── 낙하 애니메이션 ───────────────────────────────────────
    public void MoveToPosition(Vector2 targetAnchoredPos, float duration, Action onDone = null)
    {
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveAnim(targetAnchoredPos, duration, onDone));
    }

    // ── 팝 애니메이션 ─────────────────────────────────────────
    public void PlayPopAnimation(Action onDone)
    {
        StartCoroutine(PopAnim(onDone));
    }

    // ── 생성 애니메이션 ───────────────────────────────────────
    public void PlaySpawnAnimation()
    {
        StartCoroutine(SpawnAnim());
    }

    // ─────────────────────────────────────────────────────────
    // 코루틴
    // ─────────────────────────────────────────────────────────

    private IEnumerator PopAnim(Action onDone)
    {
        // 확대 후 소멸
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 1.35f, t / 0.12f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.35f, 0f, t / 0.1f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        onDone?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator SpawnAnim()
    {
        transform.localScale = Vector3.zero;
        float t = 0f;
        float dur = 0.18f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(t / dur));
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator MoveAnim(Vector2 target, float duration, Action onDone)
    {
        Vector2 start = _rect.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _rect.anchoredPosition = Vector2.Lerp(start, target, EaseInQuad(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        _rect.anchoredPosition = target;
        onDone?.Invoke();
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator FadeGlow(float targetAlpha, float duration)
    {
        Color c = glowImage.color;
        float start = c.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, targetAlpha, t / duration);
            glowImage.color = c;
            yield return null;
        }
        c.a = targetAlpha;
        glowImage.color = c;
    }

    // ── 이징 함수 ─────────────────────────────────────────────
    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInQuad(float t) => t * t;
}
