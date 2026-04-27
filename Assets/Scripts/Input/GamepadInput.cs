using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게임패드 입력을 IInputProvider / ISnapshotCapture 로 래핑합니다.
/// gamepadIndex 로 여러 패드를 독립 할당할 수 있습니다 (로컬 멀티 지원).
///
/// 버튼 매핑:
///   Left Stick          → 이동
///   Right Stick         → 조준
///   South (A / Cross)   → 점프
///   West  (X / Square)  → 대시
///   Right Trigger       → 공격  (없으면 Right Shoulder 대체)
/// </summary>
public class GamepadInput : MonoBehaviour, IInputProvider, ISnapshotCapture
{
    [Tooltip("사용할 게임패드 인덱스 (0=첫 번째 패드, 1=두 번째 패드 ...)")]
    public int gamepadIndex = 0;

    private bool _jumpPending;
    private bool _dashPending;
    private bool _attackPending;

    private Vector2    _aimDir;
    private InputFrame _snapshot;

    private Gamepad GetPad()
    {
        var all = Gamepad.all;
        return gamepadIndex < all.Count ? all[gamepadIndex] : null;
    }

    void Update()
    {
        Gamepad gp = GetPad();
        if (gp == null) return;

        if (gp.buttonSouth.wasPressedThisFrame)                                     _jumpPending   = true;
        if (gp.buttonWest.wasPressedThisFrame)                                      _dashPending   = true;
        if (gp.rightTrigger.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame)
            _attackPending = true;

        Vector2 aim = gp.rightStick.ReadValue();
        if (aim.sqrMagnitude > 0.04f)
            _aimDir = aim.normalized;

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
        Gamepad gp = GetPad();
        if (gp == null) return Vector2.zero;
        Vector2 v = gp.leftStick.ReadValue();
        return v.sqrMagnitude > 0.04f ? v.normalized : Vector2.zero;
    }

    public Vector2 GetAimInput() => _aimDir;

    public bool GetJumpDown()   { bool v = _jumpPending;   _jumpPending   = false; return v; }
    public bool GetDashDown()   { bool v = _dashPending;   _dashPending   = false; return v; }
    public bool GetAttackDown() { bool v = _attackPending; _attackPending = false; return v; }

    // ── ISnapshotCapture ─────────────────────────────────────────
    public InputFrame GetSnapshot() => _snapshot;
}
