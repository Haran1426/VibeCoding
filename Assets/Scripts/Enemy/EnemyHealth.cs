using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 30f;
    private float currentHealth;

    public int expReward = 5;
    public int scoreReward = 10;

    public GameObject deathVFXPrefab;

    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void SetStats(float hp, int exp, int score)
    {
        maxHealth = hp;
        currentHealth = hp;
        expReward = exp;
        scoreReward = score;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        GameManager.Instance?.AddExp(expReward);
        GameManager.Instance?.AddScore(scoreReward);
        if (GameManager.Instance != null) GameManager.Instance.enemiesKilled++;

        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Exp orb drop
        ExpOrb.SpawnAt(transform.position, expReward);

        Destroy(gameObject);
    }
}
