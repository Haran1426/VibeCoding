using UnityEngine;

/// <summary>
/// SRP: 넉백 물리 처리만 담당합니다.
///
/// [버그1 픽스] ApplyKnockback 후 PlayerController.NotifyKnockedBack() 을 호출해
/// 이동 코드가 velocity 를 덮어쓰지 못하도록 합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class KnockbackReceiver : MonoBehaviour
{
    private Rigidbody        _rb;
    private PlayerStats      _stats;
    private PlayerController _controller;

    [SerializeField] private float upwardBias = 0.4f;

    void Awake()
    {
        _rb         = GetComponent<Rigidbody>();
        _stats      = GetComponent<PlayerStats>();
        _controller = GetComponent<PlayerController>();
    }

    /// <summary>넉백 적용. direction: 수평 방향(normalized)</summary>
    public void ApplyKnockback(Vector3 direction, float basePower, int attackerId)
    {
        _stats.AddKnockback(basePower * 0.6f, attackerId);

        float   force = _stats.GetKnockbackForce(basePower);
        Vector3 dir   = (direction.normalized + Vector3.up * upwardBias).normalized;

        _rb.linearVelocity = Vector3.zero; // 기존 velocity 초기화
        _rb.AddForce(dir * force, ForceMode.Impulse);

        // [버그1 픽스] PlayerController 에게 넉백 발생을 알려 velocity 덮어쓰기를 막음
        _controller?.NotifyKnockedBack();

        AudioManager.Instance?.PlayPlayerHurt();
    }
}
