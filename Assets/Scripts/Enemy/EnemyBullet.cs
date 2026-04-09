using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private float damage;
    private float speed;
    private float range;
    private float traveled = 0f;

    public void Init(float dmg, float spd, float rng)
    {
        damage = dmg;
        speed = spd;
        range = rng;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += transform.forward * step;
        traveled += step;
        if (traveled >= range) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerStats>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
