using System.Collections;
using UnityEngine;

/// <summary>
/// 아이소메트릭 고정 카메라.
/// - 단일 타겟: SetTarget(t) → 그 오브젝트를 부드럽게 추적
/// - 다중 타겟: SetTargets(t[]) → 모든 활성 플레이어의 중심을 추적
/// - Shake(duration, magnitude) 로 카메라 쉐이크 재생
/// </summary>
public class ArenaCamera : MonoBehaviour
{
    public static ArenaCamera Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3   offset      = new Vector3(0f, 16f, -10f);
    [SerializeField] private float     smoothSpeed = 5f;
    [SerializeField] private bool      lockToArena = false;
    [SerializeField] private float     arenaRadius = 20f;

    private Transform[] _multiTargets;

    private Vector3   _shakeOffset;
    private Coroutine _shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        Vector3 focusPoint = GetFocusPoint();
        Vector3 desired    = focusPoint + offset + _shakeOffset;

        if (lockToArena)
        {
            Vector2 flat = new Vector2(desired.x, desired.z);
            if (flat.magnitude > arenaRadius)
            {
                flat      = flat.normalized * arenaRadius;
                desired.x = flat.x;
                desired.z = flat.y;
            }
        }

        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothSpeed);
        transform.LookAt(focusPoint + Vector3.up * 1f);
    }

    private Vector3 GetFocusPoint()
    {
        // 다중 타겟: 활성 플레이어 중심
        if (_multiTargets != null && _multiTargets.Length > 0)
        {
            Vector3 sum   = Vector3.zero;
            int     count = 0;
            foreach (var t in _multiTargets)
            {
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    sum += t.position;
                    count++;
                }
            }
            if (count > 0) return sum / count;
        }

        // 단일 타겟
        if (target != null) return target.position;

        return Vector3.zero;
    }

    // ── 공개 API ─────────────────────────────────────────────────

    /// <summary>단일 타겟 설정 (멀티: 스폰 후 호출).</summary>
    public void SetTarget(Transform t)
    {
        target        = t;
        _multiTargets = null;
    }

    /// <summary>다중 타겟 설정 (로컬 멀티플레이어용).</summary>
    public void SetTargets(Transform[] targets)
    {
        _multiTargets = targets;
        target        = null;
    }

    /// <summary>카메라 쉐이크. 진행 중이면 덮어씁니다.</summary>
    public void Shake(float duration, float magnitude)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t      = elapsed / duration;
            float dampen = 1f - t;
            _shakeOffset = new Vector3(
                (Random.value * 2f - 1f) * magnitude * dampen,
                0f,
                (Random.value * 2f - 1f) * magnitude * dampen);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _shakeOffset    = Vector3.zero;
        _shakeCoroutine = null;
    }
}
