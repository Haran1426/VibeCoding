using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float damage;
    private float speed;
    private float range;
    private int pierceLeft;
    private float traveled = 0f;
    private Vector3 dir;

    public GameObject hitVFXPrefab;

    public void Init(float dmg, float spd, float rng, int pierce)
    {
        damage = dmg;
        speed = spd;
        range = rng;
        pierceLeft = pierce;
        dir = transform.forward;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += dir * step;
        traveled += step;
        if (traveled >= range) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth eh = other.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);

            SpawnHitVFX();

            if (pierceLeft <= 0)
                Destroy(gameObject);
            else
                pierceLeft--;
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Untagged"))
        {
            // 벽이나 환경에 닿으면 제거 (Floor 제외)
            if (other.gameObject.name.StartsWith("Wall_"))
            {
                SpawnHitVFX();
                Destroy(gameObject);
            }
        }
    }

    void SpawnHitVFX()
    {
        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
    }
}
