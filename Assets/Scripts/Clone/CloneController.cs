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

        // 최초 1회 인스턴스 머티리얼 생성
        _cachedMats = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _cachedMats[i] = new Material(Shader.Find("Standard"));
            SetupTransparentMaterial(_cachedMats[i]);
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

    private static void SetupTransparentMaterial(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.EnableKeyword("_EMISSION");
    }

    public bool IsReplayFinished => _cloneInput != null && _cloneInput.IsFinished;
}
