using UnityEngine;

/// <summary>
/// 실제 플레이어와 분신(Clone) 모두 이 컨트롤러를 사용합니다.
/// IInputProvider 를 교체하면 동작이 달라집니다 (DIP).
/// 기능: 이동 / 점프 / 대시 / 공격
///
/// [버그1 픽스] 넉백 면역 타이머(_knockbackTimer):
///   ApplyKnockback 이후 일정 시간 동안 FixedUpdate 의 velocity 덮어쓰기를 차단합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    // ── 컴포넌트 참조 ────────────────────────────────────────
    private Rigidbody      _rb;
    private PlayerStats    _stats;
    private IInputProvider _input;
    private Camera         _cam;

    // ── 공격 ─────────────────────────────────────────────────
    [SerializeField] private LayerMask attackTargetMask;
    private float _attackTimer;

    // ── 대시 ─────────────────────────────────────────────────
    private float _dashCooldownTimer;
    private float _dashActiveTimer;
    private bool  _isDashing;
    public  bool  IsDashing => _isDashing;

    // ── 점프 / 지면 ──────────────────────────────────────────
    [SerializeField] private float     groundCheckDist = 0.2f;
    [SerializeField] private LayerMask groundMask;
    private bool _isGrounded;

    // ── [버그1 픽스] 넉백 면역 ───────────────────────────────
    private float _knockbackTimer;
    private const float KnockbackImmuneDuration = 0.4f;

    /// <summary>KnockbackReceiver 에서 호출. 이 시간 동안 이동 코드가 velocity를 덮어쓰지 않습니다.</summary>
    public void NotifyKnockedBack() => _knockbackTimer = KnockbackImmuneDuration;

    // ── 방향 캐시 ────────────────────────────────────────────
    private Vector2 _moveInput;
    private Vector2 _aimInput;

    // ── 상태 ─────────────────────────────────────────────────
    private bool _alive       = true;
    private bool _matchPlaying;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        _rb    = GetComponent<Rigidbody>();
        _stats = GetComponent<PlayerStats>();
        _rb.freezeRotation = true;

        if (!_stats.isClone)
            _input = GetComponent<PlayerInput>();
    }

    void Start()
    {
        _cam = Camera.main;

        // MatchManager 가 없거나 이미 Playing 상태면 바로 활성화
        if (MatchManager.Instance == null)
            _matchPlaying = true;
        else if (MatchManager.Instance.CurrentState == MatchState.Playing)
            _matchPlaying = true;
    }

    void OnEnable()
    {
        EventBus.OnEntityDied        += OnEntityDied;
        EventBus.OnMatchStateChanged += OnMatchState;
        _alive = true;
    }

    void OnDisable()
    {
        EventBus.OnEntityDied        -= OnEntityDied;
        EventBus.OnMatchStateChanged -= OnMatchState;
    }

    private void OnMatchState(MatchState s) => _matchPlaying = (s == MatchState.Playing);
    private void OnEntityDied(int id, Vector3 _, int __)
    {
        if (id == _stats.playerId) _alive = false;
    }

    // ── DIP: 런타임에 입력 교체 (분신용) ─────────────────────
    public void SetInputProvider(IInputProvider provider) => _input = provider;

    // ════════════════════════════════════════════════════════
    void Update()
    {
        if (!_alive || !_matchPlaying || _input == null) return;

        _moveInput = _input.GetMoveInput();
        _aimInput  = _input.GetAimInput();

        // 회전: 조준 방향
        if (_aimInput.sqrMagnitude > 0.01f)
        {
            Vector3 look = new Vector3(_aimInput.x, 0f, _aimInput.y);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(look),
                Time.deltaTime * 20f);
        }

        // 타이머
        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;
        if (_attackTimer       > 0f) _attackTimer       -= Time.deltaTime;
        if (_knockbackTimer    > 0f) _knockbackTimer    -= Time.deltaTime;

        // 대시 지속
        if (_isDashing)
        {
            _dashActiveTimer -= Time.deltaTime;
            if (_dashActiveTimer <= 0f) _isDashing = false;
        }

        // 점프
        CheckGround();
        if (_input.GetJumpDown() && _isGrounded)
            _rb.AddForce(Vector3.up * _stats.jumpForce, ForceMode.Impulse);

        // 대시
        if (_input.GetDashDown() && !_isDashing && _dashCooldownTimer <= 0f)
            StartDash();

        // 공격
        if (_input.GetAttackDown() && _attackTimer <= 0f)
            DoAttack();

        // 분신: 한 프레임씩 전진
        if (_stats.isClone && _input is CloneInput ci)
            ci.Advance();
    }

    void FixedUpdate()
    {
        if (!_alive || !_matchPlaying) return;
        if (_isDashing) return;

        // [버그1 픽스] 넉백 중에는 이동 코드가 velocity를 덮어쓰지 않음
        if (_knockbackTimer > 0f) return;

        Vector3 move = CameraRelativeMove(_moveInput);
        _rb.linearVelocity = new Vector3(
            move.x * _stats.moveSpeed,
            _rb.linearVelocity.y,
            move.z * _stats.moveSpeed);
    }

    // ── 지면 체크 ─────────────────────────────────────────────
    private void CheckGround()
    {
        // groundMask 가 비어 있으면 모든 레이어 대상으로 체크
        int mask = groundMask == 0 ? ~0 : (int)groundMask;
        _isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.05f,
            Vector3.down,
            groundCheckDist + 0.05f,
            mask);
    }

    // ── 대시 ─────────────────────────────────────────────────
    private void StartDash()
    {
        _isDashing         = true;
        _dashActiveTimer   = _stats.dashDuration;
        _dashCooldownTimer = _stats.dashCooldown;

        Vector3 dir = _moveInput.sqrMagnitude > 0.01f
            ? CameraRelativeMove(_moveInput)
            : transform.forward;

        _rb.linearVelocity = new Vector3(dir.x, 0f, dir.z).normalized * _stats.dashSpeed;
        AudioManager.Instance?.PlayDash();
    }

    // ── 공격 ─────────────────────────────────────────────────
    private void DoAttack()
    {
        _attackTimer = _stats.attackCooldown;

        Vector3 aimDir = _aimInput.sqrMagnitude > 0.01f
            ? new Vector3(_aimInput.x, 0f, _aimInput.y).normalized
            : transform.forward;

        Vector3 center = transform.position + aimDir * _stats.attackRange;
        center.y = transform.position.y + 0.5f;

        Collider[] hits = Physics.OverlapSphere(center, _stats.attackRadius, attackTargetMask);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;

            var receiver = col.GetComponent<KnockbackReceiver>();
            if (receiver == null) continue;

            Vector3 dir = (col.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

            receiver.ApplyKnockback(dir.normalized, _stats.attackPower, _stats.playerId);
            ScoreSystem.Instance?.RegisterHit(_stats.playerId);
            AudioManager.Instance?.PlayAttackHit();
        }

        VFXManager.Instance?.PlayAttack(center);
        AudioManager.Instance?.PlayAttack();
    }

    // ── 카메라 기준 이동 방향 ─────────────────────────────────
    private Vector3 CameraRelativeMove(Vector2 input)
    {
        if (_cam == null) return new Vector3(input.x, 0f, input.y);

        Vector3 forward = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(_cam.transform.right,   Vector3.up).normalized;
        return (forward * input.y + right * input.x).normalized;
    }

    public Vector3 GetFacingDirection() => transform.forward;
}
