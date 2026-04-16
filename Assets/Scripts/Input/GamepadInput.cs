using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게임패드 입력을 IInputProvider / ISnapshotCapture 로 래핑합니다.
/// Unity Input System 1.x 사용.
///
/// 버튼 매핑:
///   Left Stick          → 이동
///   Right Stick         → 조준
///   South (A / Cross)   → 점프
///   West  (X / Square)  → 대시
///   Right Trigger       → 공격  (없으면 Right Shoulder 대체)
///
/// PlayerInput 와 동일하게 버튼을 Update 에서 버퍼링하여
/// FixedUpdate 의 InputRecorder 가 누락 없이 기록합니다.
/// </summary>
public class GamepadInput : MonoBehaviour, IInputProvider, ISnapshotCapture
{
    private bool _jumpPending;
    private bool _dashPending;
    private bool _attackPending;

    private Vector2    _aimDir;      // 오른쪽 스틱 방향 (정규화)
    private InputFrame _snapshot;    // InputRecorder 용 소비 전 스냅샷

    void Update()
    {
        Gamepad gp = Gamepad.current;
        if (gp == null) return;

        // ── 버튼 press 버퍼링 ────────────────────────────────────
        if (gp.buttonSouth.wasPressedThisFrame)                              _jumpPending   = true;
        if (gp.buttonWest.wasPressedThisFrame)                               _dashPending   = true;
        if (gp.rightTrigger.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame)
            _attackPending = true;

        // ── 조준 방향: 오른쪽 스틱 ──────────────────────────────
        Vector2 aim = gp.rightStick.ReadValue();
        if (aim.sqrMagnitude > 0.04f)            // 데드존 0.2
            _aimDir = aim.normalized;

        // ── 스냅샷 저장 (PlayerController 소비 이전!) ───────────
        Vector2 move = gp.leftStick.ReadValue();
        _snapshot = new InputFrame
        {
            moveX      = move.x,
            moveY      = move.y,
            aimX       = _aimDir.x,
            aimZ       = _aimDir.y,
            jumpDown   = _jumpPending,
            dashDown   = _dashPending,
            attackDown = _attackPending,
        };
    }

    // ── IInputProvider ───────────────────────────────────────────
    public Vector2 GetMoveInput()
    {
        Gamepad gp = Gamepad.current;
        if (gp == null) return Vector2.zero;
        Vector2 v = gp.leftStick.ReadValue();
        return v.sqrMagnitude > 0.04f ? v.normalized : Vector2.zero;
    }

    public Vector2 GetAimInput() => _aimDir;

    public bool GetJumpDown()
    {
        bool v = _jumpPending;
        _jumpPending = false;
        return v;
    }

    public bool GetDashDown()
    {
        bool v = _dashPending;
        _dashPending = false;
        return v;
    }

    public bool GetAttackDown()
    {
        bool v = _attackPending;
        _attackPending = false;
        return v;
    }

    // ── ISnapshotCapture (InputRecorder 전용) ────────────────────
    public InputFrame GetSnapshot() => _snapshot;
}
