using UnityEngine;

/// <summary>
/// 실제 플레이어(PlayerInput) 와 분신(CloneInput) 이 공통으로 구현하는 입력 인터페이스.
/// DIP: PlayerController 는 구체 입력 방식에 의존하지 않습니다.
/// </summary>
public interface IInputProvider
{
    Vector2 GetMoveInput();
    Vector2 GetAimInput();   // XZ 평면 방향 (normalized)
    bool    GetJumpDown();
    bool    GetDashDown();
    bool    GetAttackDown();
}
