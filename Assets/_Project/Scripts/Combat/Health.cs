using System;
using UnityEngine;
using TheLastSasak.Player.Stats;

public class Health : MonoBehaviour, IDamageable, IHealthStatReceiver, IPoolable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public PoolLeaseId CurrentLeaseId { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private InvulnerabilityHandler _invulnerability;

    private void Awake()
    {
        _invulnerability = GetComponent<InvulnerabilityHandler>();
        if (currentHealth <= 0f)
            currentHealth = maxHealth;
    }

    public void Configure(float max)
    {
        maxHealth = Mathf.Max(1f, max);
        ResetHealth();
    }

    public void SyncInitialMaxHealth(float initialMax, bool fillCurrentToMax)
    {
        maxHealth = Mathf.Max(1f, initialMax);
        if (!IsDead)
        {
            currentHealth = fillCurrentToMax
                ? maxHealth
                : Mathf.Clamp(currentHealth, 1f, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ApplyMaxHealthChange(float previousMaxHealth, float newMaxHealth, MaxHealthRecalculationPolicy policy)
    {
        float clampedMax = Mathf.Max(1f, newMaxHealth);
        float effectivePrevMax = previousMaxHealth > 0f ? previousMaxHealth : maxHealth;
        maxHealth = clampedMax;

        if (!IsDead)
        {
            float resolved = HealthPolicyUtility.ResolveNewCurrentHealth(
                currentHealth,
                effectivePrevMax,
                maxHealth,
                policy);
            currentHealth = Mathf.Clamp(resolved, 1f, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMax, bool healDifference = false)
    {
        float clampedMax = Mathf.Max(1f, newMax);
        float diff = clampedMax - maxHealth;
        maxHealth = clampedMax;

        if (!IsDead)
        {
            if (healDifference && diff > 0f)
                currentHealth = Mathf.Min(maxHealth, currentHealth + diff);
            else
                currentHealth = Mathf.Clamp(currentHealth, 1f, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void OnRentFromPool(PoolLeaseId leaseId)
    {
        CurrentLeaseId = leaseId;
        ResetHealth();
    }

    public void OnReturnToPool()
    {
        CurrentLeaseId = default;
    }

    public DamageResult TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
            return DamageResult.Ignored;

        if (_invulnerability != null && _invulnerability.IsInvulnerable)
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
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}