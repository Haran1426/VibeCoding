using System.Collections;
using UnityEngine;

/// <summary>
/// 아이소메트릭 고정 카메라. 플레이어를 부드럽게 추적합니다.
/// Shake(duration, magnitude) 로 카메라 쉐이크를 재생할 수 있습니다.
/// </summary>
public class ArenaCamera : MonoBehaviour
{
    public static ArenaCamera Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3   offset      = new Vector3(0f, 16f, -10f);
    [SerializeField] private float     smoothSpeed = 5f;
    [SerializeField] private bool      lockToArena = false;
    [SerializeField] private float     arenaRadius = 20f;

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
        if (target == null) return;

        Vector3 desired = target.position + offset + _shakeOffset;

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
        transform.LookAt(target.position + Vector3.up * 1f);
    }

    /// <summary>멀티: 내 플레이어 스폰 후 카메라 타겟 교체.</summary>
    public void SetTarget(Transform t) => target = t;

    /// <summary>
    /// 카메라 쉐이크. 이미 쉐이크 중이면 더 강한 쪽으로 덮어씁니다.
    /// </summary>
    /// <param name="duration">쉐이크 지속 시간(초)</param>
    /// <param name="magnitude">최대 오프셋 크기(유닛)</param>
    public void Shake(float duration, float magnitude)
    {
        // 진행 중인 쉐이크보다 약하면 무시
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t       = elapsed / duration;
            float dampen  = 1f - t;   // 시간이 지날수록 약해짐
            float x = (Random.value * 2f - 1f) * magnitude * dampen;
            float z = (Random.value * 2f - 1f) * magnitude * dampen;
            _shakeOffset = new Vector3(x, 0f, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _shakeOffset    = Vector3.zero;
        _shakeCoroutine = null;
    }
}
