using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 시각 효과를 담당합니다.
/// - 플레이어 ID 기반 색상 설정 (GDD 팔레트)
/// - 피격 플래시 (흰색 번쩍)
/// - 무적 중 깜빡이기 (렌더러 on/off)
///
/// CloneController 가 있는 오브젝트(분신)는 자동으로 스킵합니다.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    // GDD 색상 팔레트 (playerId 순서)
    public static readonly Color[] PlayerColors =
    {
        new Color(0.00f, 0.75f, 1.00f, 1f),  // 0: 네온 블루   #00BFFF
        new Color(1.00f, 0.18f, 0.58f, 1f),  // 1: 네온 핑크   #FF2D95
        new Color(0.60f, 0.20f, 1.00f, 1f),  // 2: 네온 퍼플   #9933FF
        new Color(1.00f, 0.83f, 0.00f, 1f),  // 3: 네온 옐로우 #FFD400
    };

    private const float FlashDuration = 0.08f;
    private const float BlinkInterval = 0.1f;

    private Renderer[] _renderers;
    private Material[] _materials;
    private Color      _baseColor;
    private Coroutine  _flashCoroutine;
    private Coroutine  _blinkCoroutine;

    private bool _isCloneVisuals; // 분신이면 true → 아무것도 안 함

    void Awake()
    {
        // 분신은 CloneController 가 색상 관리 — 충돌 방지
        if (GetComponent<CloneController>() != null)
        {
            _isCloneVisuals = true;
            return;
        }

        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        // 인스턴스 Material 생성 (공유 Material 수정 방지)
        _materials = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].sharedMaterial != null)
            {
                _materials[i] = new Material(_renderers[i].sharedMaterial);
                _renderers[i].material = _materials[i];
            }
        }
    }

    void Start()
    {
        if (_isCloneVisuals) return;

        var stats = GetComponent<PlayerStats>();
        if (stats != null)
            ApplyPlayerColor(stats.playerId);
    }

    void OnEnable()
    {
        if (!_isCloneVisuals)
            EventBus.OnMatchStateChanged += OnMatchState;
    }

    void OnDisable()
    {
        if (!_isCloneVisuals)
            EventBus.OnMatchStateChanged -= OnMatchState;
    }

    // 매치 Playing 전환 시 색상 재적용 (멀티에서 늦게 playerId 가 확정될 때 대비)
    private void OnMatchState(MatchState state)
    {
        if (state != MatchState.Playing) return;
        var stats = GetComponent<PlayerStats>();
        if (stats != null) ApplyPlayerColor(stats.playerId);
    }

    // ════════════════════════════════════════════════════════
    //  공개 API
    // ════════════════════════════════════════════════════════

    public void ApplyPlayerColor(int playerId)
    {
        if (_isCloneVisuals || _materials == null) return;

        _baseColor = ColorOf(playerId);
        SetMaterialColor(_baseColor);
    }

    public void PlayHitFlash()
    {
        if (_isCloneVisuals || _materials == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlashCoroutine());
    }

    public void StartInvincibilityBlink()
    {
        if (_isCloneVisuals || _renderers == null) return;
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    public void StopInvincibilityBlink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        SetRenderersEnabled(true);
    }

    // ════════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════════

    private IEnumerator HitFlashCoroutine()
    {
        SetMaterialColor(Color.white);
        yield return new WaitForSeconds(FlashDuration);
        SetMaterialColor(_baseColor);
        _flashCoroutine = null;
    }

    private IEnumerator BlinkCoroutine()
    {
        bool show = true;
        while (true)
        {
            SetRenderersEnabled(show);
            show = !show;
            yield return new WaitForSeconds(BlinkInterval);
        }
    }

    private void SetMaterialColor(Color color)
    {
        if (_materials == null) return;
        foreach (var mat in _materials)
        {
            if (mat == null) continue;
            mat.color = color;

            // 에미션이 있는 머티리얼이면 같이 갱신
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", color * 0.5f);
        }
    }

    private void SetRenderersEnabled(bool on)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
            if (r != null) r.enabled = on;
    }

    // ── 유틸 ─────────────────────────────────────────────────

    public static Color ColorOf(int playerId)
    {
        if (PlayerColors.Length == 0) return Color.white;
        return PlayerColors[Mathf.Abs(playerId) % PlayerColors.Length];
    }
}
