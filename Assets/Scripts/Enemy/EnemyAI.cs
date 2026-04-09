using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float damagePerSecond = 15f;
    public float contactDamageInterval = 0.5f;

    private Rigidbody rb;
    private Transform player;
    private float contactTimer = 0f;

    public enum EnemyType { Chaser, Shooter, Splitter }
    public EnemyType enemyType = EnemyType.Chaser;

    // For Shooter type
    public GameObject enemyBulletPrefab;
    private float shootTimer = 0f;
    public float shootInterval = 2f;
    public float shootRange = 12f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;
        rb.useGravity = false;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > 0.1f)
        {
            Vector3 dir = toPlayer.normalized;
            rb.linearVelocity = new Vector3(dir.x * moveSpeed, 0f, dir.z * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }

        if (enemyType == EnemyType.Shooter && dist <= shootRange)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                FireAtPlayer(toPlayer.normalized);
                shootTimer = shootInterval;
            }
        }
    }

    void FireAtPlayer(Vector3 dir)
    {
        if (enemyBulletPrefab == null) return;
        Vector3 pos = transform.position + Vector3.up * 0.5f;
        GameObject b = Instantiate(enemyBulletPrefab, pos, Quaternion.LookRotation(dir));
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        if (eb != null) eb.Init(10f, 8f, 15f);
    }

    void OnCollisionStay(Collision col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        contactTimer -= Time.deltaTime;
        if (contactTimer <= 0f)
        {
            PlayerStats ps = col.gameObject.GetComponent<PlayerStats>();
            ps?.TakeDamage(damagePerSecond * contactDamageInterval);
            contactTimer = contactDamageInterval;
        }
    }
}
