using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 분신 초기화와 속도 가속을 담당합니다.
///
/// [버그4 픽스] Material 을 Init 당 1회 생성 후 캐싱 — 풀 재사용 시 VRAM 누수 방지.
///
/// 속도 가속: 매치 시작 후 경과 시간에 따라 moveSpeed, dashSpeed 를 점진적으로 올립니다.
///   - 0초: 기본 속도 (100%)
///   - 60초: 기본 속도 × speedMultiplierAt60s
///   - 120초: 기본 속도 × speedMultiplierAt120s
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerStats))]
public class CloneController : MonoBehaviour
{
    [Header("속도 가속 (GDD: 시간 지날수록 빨라짐)")]
    [SerializeField] private float speedMultiplierAt60s  = 1.5f;   // 1분 시점 배율
    [SerializeField] private float speedMultiplierAt120s = 2.2f;   // 2분 시점 배율

    private PlayerController _pc;
    private PlayerStats      _stats;
    private CloneInput       _cloneInput;
    private Renderer[]       _renderers;
    private Material[]       _cachedMats;

    private float _baseMoveSpeed;
    private float _baseDashSpeed;
    private float _spawnTime;       // 분신 생성 시점 (Time.time)
    private float _matchStartTime;  // 매치 시작 시점

    void Awake()
    {
        _pc        = GetComponent<PlayerController>();
        _stats     = GetComponent<PlayerStats>();
        _renderers = GetComponentsInChildren<Renderer>();

        _cachedMats = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _cachedMats[i] = CreateTransparentMaterial();
            _renderers[i].material = _cachedMats[i];
        }
    }

    void OnEnable()  => EventBus.OnMatchStarted += OnMatchStarted;
    void OnDisable() => EventBus.OnMatchStarted -= OnMatchStarted;

    private void OnMatchStarted() => _matchStartTime = Time.time;

    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (_cloneInput == null || _cloneInput.IsFinished) return;

        // 경과 시간 기반 속도 배율 보간
        float elapsed     = Time.time - _matchStartTime;
        float t           = Mathf.Clamp01(elapsed / 120f);
        float t60         = Mathf.Clamp01(elapsed / 60f);

        float multiplier;
        if (elapsed <= 60f)
            multiplier = Mathf.Lerp(1f, speedMultiplierAt60s, t60);
        else
            multiplier = Mathf.Lerp(speedMultiplierAt60s, speedMultiplierAt120s,
                                    (elapsed - 60f) / 60f);

        _stats.moveSpeed  = _baseMoveSpeed * multiplier;
        _stats.dashSpeed  = _baseDashSpeed * multiplier;
    }

    /// <summary>분신 활성화. 기록 프레임 / 스폰 위치 / ID / 색상 전달.</summary>
    public void Init(List<InputFrame> frames, Vector3 spawnPos, int cloneId, Color color)
    {
        transform.position = spawnPos;
        _stats.isClone     = true;
        _stats.playerId    = cloneId;
        _stats.ResetKnockback();

        // 기본 속도 저장 (가속 계산 기준)
        _baseMoveSpeed = _stats.moveSpeed;
        _baseDashSpeed = _stats.dashSpeed;
        _spawnTime     = Time.time;

        _cloneInput = new CloneInput(frames);
        _pc.SetInputProvider(_cloneInput);

        // 색상 적용 (캐싱된 Material 재사용)
        for (int i = 0; i < _cachedMats.Length; i++)
        {
            _cachedMats[i].color = color;
            if (_cachedMats[i].HasProperty("_EmissionColor"))
                _cachedMats[i].SetColor("_EmissionColor",
                    new Color(color.r, color.g, color.b) * 0.4f);
        }

        GetComponent<DeathDetector>()?.ResetDead();
        gameObject.SetActive(true);
    }

    // Built-in RP / URP 자동 감지 투명 머티리얼 생성
    private static Material CreateTransparentMaterial()
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp != null)
        {
            var m = new Material(urp);
            m.SetFloat("_Surface",   1f);
            m.SetFloat("_Blend",     0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_EMISSION");
            m.renderQueue = 3000;
            return m;
        }

        Shader builtin = Shader.Find("Standard");
        if (builtin != null)
        {
            var m = new Material(builtin);
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.EnableKeyword("_EMISSION");
            return m;
        }

        return new Material(Shader.Find("Sprites/Default"));
    }

    public bool IsReplayFinished => _cloneInput != null && _cloneInput.IsFinished;
}
