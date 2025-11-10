using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class HealthChangedEvent : UnityEvent<int, int> {} // (current, max)
[System.Serializable]
public class SimpleEvent : UnityEvent {}

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [Min(1)] public int maxHealth = 5;
    [SerializeField] int currentHealth;

    [Header("Damage Rules")]
    [Tooltip("Seconds of invulnerability after taking damage.")]
    [Min(0f)] public float invulnSeconds = 0.3f;
    [Tooltip("Ignore further damage while invulnerable.")]
    public bool useIFrames = true;

    [Header("Events")]
    public HealthChangedEvent onHealthChanged; // fires on any change
    public SimpleEvent onDamaged;              // fires when damage applied
    public SimpleEvent onHealed;               // fires when healed
    public SimpleEvent onDied;                 // fires once when reaches 0

    float invulnTimer = 0f;
    bool isDead = false;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    public int Current => currentHealth;
    public int Max => maxHealth;
    public bool IsAlive => !isDead;

    public void SetMax(int newMax, bool refill = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        currentHealth = Mathf.Clamp(refill ? maxHealth : currentHealth, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (currentHealth != before)
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onHealed?.Invoke();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;
        if (useIFrames && invulnTimer > 0f) return; // ignore during i-frames

        int before = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        invulnTimer = invulnSeconds;
        if (currentHealth != before)
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onDamaged?.Invoke();
        }

        if (currentHealth == 0 && !isDead)
        {
            isDead = true;
            onDied?.Invoke();
        }
    }

    public void Kill()
    {
        if (isDead) return;
        currentHealth = 0;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        isDead = true;
        onDied?.Invoke();
    }
}