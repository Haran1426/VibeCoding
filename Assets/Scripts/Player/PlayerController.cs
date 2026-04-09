using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerStats stats;
    private Camera mainCam;

    private Vector3 moveInput;
    private Vector3 aimDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;
        rb.useGravity = false;
    }

    void Start()
    {
        mainCam = Camera.main;
        stats.OnDeath += HandleDeath;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        ReadMovement();
        AimAtMouse();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        rb.linearVelocity = new Vector3(moveInput.x * stats.GetActualMoveSpeed(), 0f, moveInput.z * stats.GetActualMoveSpeed());
    }

    void ReadMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;
    }

    void AimAtMouse()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 hit = ray.GetPoint(dist);
            aimDir = (hit - transform.position);
            aimDir.y = 0f;
            aimDir.Normalize();
            if (aimDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir), Time.deltaTime * 20f);
        }
    }

    public Vector3 GetAimDirection() => aimDir;

    void HandleDeath()
    {
        GameManager.Instance?.TriggerGameOver();
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (stats != null) stats.OnDeath -= HandleDeath;
    }
}
