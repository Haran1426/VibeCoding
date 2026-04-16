using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 분신 초기화를 담당합니다.
/// [버그4 픽스] Material 을 Init 당 1회 생성 후 캐싱 — 풀 재사용 시 VRAM 누수 방지.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerStats))]
public class CloneController : MonoBehaviour
{
    private PlayerController _pc;
    private PlayerStats      _stats;
    private CloneInput       _cloneInput;
    private Renderer[]       _renderers;
    private Material[]       _cachedMats; // [버그4 픽스] 캐싱

    void Awake()
    {
        _pc        = GetComponent<PlayerController>();
        _stats     = GetComponent<PlayerStats>();
        _renderers = GetComponentsInChildren<Renderer>();

        // [버그4 픽스] 최초 1회 인스턴스 머티리얼 생성 (Built-in / URP 자동 감지)
        _cachedMats = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _cachedMats[i] = CreateTransparentMaterial();
            _renderers[i].material = _cachedMats[i];
        }
    }

    /// <summary>분신 활성화. 기록 프레임 / 스폰 위치 / ID / 색상 전달.</summary>
    public void Init(List<InputFrame> frames, Vector3 spawnPos, int cloneId, Color color)
    {
        transform.position = spawnPos;
        _stats.isClone     = true;
        _stats.playerId    = cloneId;
        _stats.ResetKnockback();

        _cloneInput = new CloneInput(frames);
        _pc.SetInputProvider(_cloneInput);

        // [버그4 픽스] 기존 Material 재사용, 색상만 변경
        for (int i = 0; i < _cachedMats.Length; i++)
        {
            _cachedMats[i].color = color;
            _cachedMats[i].SetColor("_EmissionColor",
                new Color(color.r, color.g, color.b) * 0.4f);
        }

        GetComponent<DeathDetector>()?.ResetDead();
        gameObject.SetActive(true);
    }

    // [버그4 픽스] Built-in RP / URP 자동 감지 투명 머티리얼 생성
    private static Material CreateTransparentMaterial()
    {
        // URP 우선
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp != null)
        {
            var m = new Material(urp);
            m.SetFloat("_Surface", 1f);   // 0=Opaque, 1=Transparent
            m.SetFloat("_Blend",   0f);   // Alpha blending
            m.SetFloat("_AlphaClip", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_EMISSION");
            m.renderQueue = 3000;
            return m;
        }

        // Built-in Standard fallback
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

        // 최후 fallback (Sprites/Default는 항상 존재)
        return new Material(Shader.Find("Sprites/Default"));
    }

    public bool IsReplayFinished => _cloneInput != null && _cloneInput.IsFinished;
}
