using UnityEngine;

/// <summary>
/// 플레이어(또는 분신)의 스탯과 넉백 데미지 누적을 관리합니다.
/// Smash Bros 방식: knockbackPercent 가 높을수록 더 멀리 날아갑니다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Identity")]
    public int  playerId = 0;
    public bool isClone  = false;

    [Header("이동")]
    public float moveSpeed   = 8f;
    public float jumpForce   = 10f;

    [Header("대시")]
    public float dashSpeed    = 20f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.9f;

    [Header("공격")]
    public float attackPower    = 10f;
    public float attackDamage   = 12f;
    public float attackRadius   = 1.6f;
    public float attackRange    = 1.4f;
    public float attackCooldown = 0.35f;

    // ── 런타임 상태 ──────────────────────────────────────────
    [HideInInspector] public float knockbackPercent = 0f;
    [HideInInspector] public int   lastHitBy        = -1;

    // ── 무적 ─────────────────────────────────────────────────
    private float         _invincibleTimer;
    private PlayerVisuals _visuals;

    public bool IsInvincible => _invincibleTimer > 0f;

    void Awake()
    {
        _visuals = GetComponent<PlayerVisuals>();
    }

    void Update()
    {
        if (_invincibleTimer <= 0f) return;

        _invincibleTimer -= Time.deltaTime;
        if (_invincibleTimer <= 0f)
        {
            _invincibleTimer = 0f;
            _visuals?.StopInvincibilityBlink();
        }
    }

    /// <summary>리스폰 후 호출. duration 초 동안 넉백 무시 + 깜빡이기.</summary>
    public void StartInvincibility(float duration)
    {
        _invincibleTimer = duration;
        _visuals?.StartInvincibilityBlink();
    }

    public void ResetKnockback()
    {
        knockbackPercent = 0f;
        lastHitBy        = -1;
        EventBus.RaiseKnockbackChanged(playerId, knockbackPercent);
    }

    public void AddKnockback(float damage, int attackerId)
    {
        knockbackPercent += damage;
        lastHitBy         = attackerId;
        EventBus.RaiseKnockbackChanged(playerId, knockbackPercent);
    }

    public float GetKnockbackForce(float basePower)
        => basePower * (1f + knockbackPercent / 60f);
}
