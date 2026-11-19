using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    InvulnerabilityHandler invulnerability;

    void Awake()
    {
        invulnerability = GetComponent<InvulnerabilityHandler>();
        if (currentHealth <= 0f)
            currentHealth = maxHealth;
    }

    public void Configure(float max)
    {
        maxHealth = Mathf.Max(1f, max);
        ResetHealth();
    }

    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public DamageResult TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
            return DamageResult.Ignored;

        if (invulnerability != null && invulnerability.IsInvulnerable)
            return DamageResult.Blocked;

        currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            IsDead = true;
            GameEvents.RaiseEntityDied(this);
            OnDied?.Invoke();
            return DamageResult.Killed(damageInfo.Amount);
        }

        return DamageResult.Applied(damageInfo.Amount, currentHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
