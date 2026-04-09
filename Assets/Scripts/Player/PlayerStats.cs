using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 8f;
    public float bulletDamage = 25f;
    public float fireRate = 0.2f;      // seconds between shots
    public float bulletSpeed = 20f;
    public float bulletRange = 15f;
    public int pierceCount = 0;        // extra enemies bullet passes through
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public bool hasTripleShot = false;
    public bool hasSplitShot = false;
    public float magnetRadius = 3f;
    public int maxBullets = 1;

    public System.Action<float, float> OnHealthChanged; // current, max
    public System.Action OnDeath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f)
            OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetActualFireRate() => fireRate / fireRateMultiplier;
    public float GetActualMoveSpeed() => moveSpeed * speedMultiplier;
    public float GetActualDamage() => bulletDamage * damageMultiplier;
}
