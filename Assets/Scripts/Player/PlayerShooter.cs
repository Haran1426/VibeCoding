using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerController controller;
    private float fireTimer = 0f;

    public GameObject bulletPrefab;
    public Transform firePoint;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            Shoot();
            fireTimer = stats.GetActualFireRate();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;
        Vector3 dir = controller.GetAimDirection();
        if (dir == Vector3.zero) dir = transform.forward;

        SpawnBullet(origin, dir);

        if (stats.hasTripleShot)
        {
            SpawnBullet(origin, Quaternion.Euler(0, 15, 0) * dir);
            SpawnBullet(origin, Quaternion.Euler(0, -15, 0) * dir);
        }

        if (stats.hasSplitShot)
        {
            SpawnBullet(origin, Quaternion.Euler(0, 90, 0) * dir);
            SpawnBullet(origin, Quaternion.Euler(0, -90, 0) * dir);
        }
    }

    void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        GameObject b = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Init(stats.GetActualDamage(), stats.bulletSpeed, stats.bulletRange, stats.pierceCount);
        }
    }
}
