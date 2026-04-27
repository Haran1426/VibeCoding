using UnityEngine;

/// <summary>
/// SRP: 넉백 물리 처리만 담당합니다.
///
/// [버그1 픽스] ApplyKnockback 후 PlayerController.NotifyKnockedBack() 호출 →
///   이동 코드가 velocity 를 덮어쓰지 못하도록 차단.
///
/// 멀티: ApplyKnockback 후 PlayerNetworkSync.ServerUpdateKnockback() 호출 →
///   NetKnockback NetworkVariable 동기화.
///
/// 무적: PlayerStats.IsInvincible 이 true 이면 넉백 완전 무시.
/// 피격 플래시: PlayerVisuals.PlayHitFlash() 호출.
/// 카메라 쉐이크: 힘이 shakeThreshold 이상이면 ArenaCamera.Instance.Shake().
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class KnockbackReceiver : MonoBehaviour
{
    private Rigidbody         _rb;
    private PlayerStats       _stats;
    private PlayerController  _controller;
    private PlayerNetworkSync _netSync;
    private PlayerVisuals     _visuals;

    [SerializeField] private float upwardBias      = 0.4f;
    [SerializeField] private float shakeThreshold  = 12f;   // 이 힘 이상이면 카메라 쉐이크

    void Awake()
    {
        _rb         = GetComponent<Rigidbody>();
        _stats      = GetComponent<PlayerStats>();
        _controller = GetComponent<PlayerController>();
        _netSync    = GetComponent<PlayerNetworkSync>();
        _visuals    = GetComponent<PlayerVisuals>();
    }

    /// <summary>넉백 적용. direction: 수평 방향(normalized)</summary>
    public void ApplyKnockback(Vector3 direction, float basePower, int attackerId)
    {
        // 무적 중이면 완전 무시
        if (_stats != null && _stats.IsInvincible) return;

        _stats.AddKnockback(basePower * 0.6f, attackerId);

        float   force = _stats.GetKnockbackForce(basePower);
        Vector3 dir   = (direction.normalized + Vector3.up * upwardBias).normalized;

        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(dir * force, ForceMode.Impulse);

        _controller?.NotifyKnockedBack();

        // 멀티: NetKnockback 동기화
        _netSync?.ServerUpdateKnockback(_stats.knockbackPercent);

        // 피격 플래시
        _visuals?.PlayHitFlash();

        // 카메라 쉐이크 (강한 넉백만)
        if (force >= shakeThreshold)
        {
            float shakeMag = Mathf.Clamp(force * 0.012f, 0.05f, 0.35f);
            ArenaCamera.Instance?.Shake(0.22f, shakeMag);
        }

        AudioManager.Instance?.PlayPlayerHurt();
    }
}
