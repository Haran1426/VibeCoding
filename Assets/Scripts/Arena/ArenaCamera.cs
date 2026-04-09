using UnityEngine;

/// <summary>
/// 아이소메트릭 고정 카메라. 플레이어를 부드럽게 추적합니다.
/// </summary>
public class ArenaCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3   offset      = new Vector3(0f, 16f, -10f);
    [SerializeField] private float     smoothSpeed = 5f;
    [SerializeField] private bool      lockToArena = false;
    [SerializeField] private float     arenaRadius = 20f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

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
        transform.LookAt(target.position + Vector3.up * 1f);
    }

    /// <summary>멀티: 내 플레이어 스폰 후 카메라 타겟 교체</summary>
    public void SetTarget(Transform t) => target = t;
}
