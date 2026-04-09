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
    public float attackPower   = 10f;  // 기본 넉백 세기
    public float attackDamage  = 12f;  // knockbackPercent 증가량
    public float attackRadius  = 1.6f;
    public float attackRange   = 1.4f;
    public float attackCooldown = 0.35f;

    // ── 런타임 상태 ──────────────────────────────────────────
    [HideInInspector] public float knockbackPercent = 0f;
    [HideInInspector] public int   lastHitBy        = -1;

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

    /// <summary>넉백 퍼센트에 따른 실제 발사 힘 계산</summary>
    public float GetKnockbackForce(float basePower)
        => basePower * (1f + knockbackPercent / 60f);
}
