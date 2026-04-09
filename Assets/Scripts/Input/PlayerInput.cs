using UnityEngine;

/// <summary>
/// PC 키보드+마우스 입력을 IInputProvider 로 래핑합니다.
/// WASD 이동 / Space 점프 / Shift 대시 / LMB 공격.
///
/// [버그2 픽스]
/// 매 Update 시작 시 스냅샷(_snapshot)을 먼저 저장한 뒤 버튼 소비.
/// InputRecorder 는 소비 전 스냅샷을 읽어 분신에 정확히 전달합니다.
/// </summary>
public class PlayerInput : MonoBehaviour, IInputProvider
{
    private bool _jumpPending;
    private bool _dashPending;
    private bool _attackPending;

    private Vector2    _aimDir;
    private Camera     _cam;
    private InputFrame _snapshot; // 소비 전 스냅샷 (InputRecorder 용)

    void Awake() => _cam = Camera.main;

    void Update()
    {
        // ── 1. 버튼 press 감지 ─────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Space))     _jumpPending   = true;
        if (Input.GetKeyDown(KeyCode.LeftShift)) _dashPending   = true;
        if (Input.GetMouseButtonDown(0))         _attackPending = true;

        // ── 2. 조준 방향 계산 ──────────────────────────────────
        if (_cam != null)
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                Vector3 delta = ray.GetPoint(dist) - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.01f)
                    _aimDir = new Vector2(delta.x, delta.z).normalized;
            }
        }

        // ── 3. 스냅샷 저장 (PlayerController 소비 이전!) ───────
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
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

    // ── IInputProvider (PlayerController 용, 소비 방식) ────────
    public Vector2 GetMoveInput() =>
        new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

    public Vector2 GetAimInput() => _aimDir;

    public bool GetJumpDown()   { bool v = _jumpPending;   _jumpPending   = false; return v; }
    public bool GetDashDown()   { bool v = _dashPending;   _dashPending   = false; return v; }
    public bool GetAttackDown() { bool v = _attackPending; _attackPending = false; return v; }

    // ── InputRecorder 전용: 소비 전 스냅샷 반환 ────────────────
    public InputFrame GetSnapshot() => _snapshot;
}
