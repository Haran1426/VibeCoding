using System.Collections;
using UnityEngine;

/// <summary>
/// Arena camera for single-target and multi-target brawler framing.
/// Supports camera shake without owning any gameplay state.
/// </summary>
public class ArenaCamera : MonoBehaviour
{
    public static ArenaCamera Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -10f);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool lockToArena = false;
    [SerializeField] private float arenaRadius = 20f;

    private Transform[] _multiTargets;
    private Vector3 _shakeOffset;
    private Coroutine _shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        Vector3 focusPoint = GetFocusPoint();
        Vector3 desired = focusPoint + offset + _shakeOffset;

        if (lockToArena)
        {
            Vector2 flat = new Vector2(desired.x, desired.z);
            if (flat.magnitude > arenaRadius)
            {
                flat = flat.normalized * arenaRadius;
                desired.x = flat.x;
                desired.z = flat.y;
            }
        }

        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothSpeed);
        transform.LookAt(focusPoint + Vector3.up);
    }

    private Vector3 GetFocusPoint()
    {
        if (_multiTargets != null && _multiTargets.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (Transform t in _multiTargets)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                sum += t.position;
                count++;
            }

            if (count > 0) return sum / count;
        }

        if (target != null) return target.position;
        return Vector3.zero;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        _multiTargets = null;
    }

    public void SetTargets(Transform[] targets)
    {
        _multiTargets = targets;
        target = null;
    }

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
            float dampen = 1f - elapsed / duration;
            _shakeOffset = new Vector3(
                (Random.value * 2f - 1f) * magnitude * dampen,
                0f,
                (Random.value * 2f - 1f) * magnitude * dampen);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _shakeCoroutine = null;
    }
}
