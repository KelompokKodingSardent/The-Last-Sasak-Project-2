using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    CombatResolver resolver;
    int nextAttackId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        resolver = GetComponent<CombatResolver>();
        if (resolver == null)
            resolver = gameObject.AddComponent<CombatResolver>();
    }

    public int NextAttackId()
    {
        return nextAttackId++;
    }

    public DamageResult ApplyDamage(IDamageable target, DamageInfo info)
    {
        return resolver.Resolve(target, info);
    }

    public DamageResult ApplyDamage(Collider col, DamageInfo info)
    {
        if (col == null)
            return DamageResult.Ignored;

        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        return ApplyDamage(damageable, info);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
