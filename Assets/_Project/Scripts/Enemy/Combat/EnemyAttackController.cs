using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
    public EnemyDefinitionSO Definition;
    public ProjectilePool ProjectilePool;
    public Transform Muzzle;
    float cooldown;

    public bool CanAttack => cooldown <= 0f;

    public void Bind(EnemyDefinitionSO definition, ProjectilePool pool)
    {
        Definition = definition;
        ProjectilePool = pool;
        if (Muzzle == null)
            Muzzle = transform;
    }

    public void PerformAttack(Transform target)
    {
        if (Definition == null || target == null || cooldown > 0f)
            return;

        cooldown = Definition.AttackCooldown;

        if (Definition.Archetype == EnemyArchetype.Ranged)
        {
            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position + Vector3.up;
            Vector3 dir = (target.position + Vector3.up - origin);
            dir.y = 0f;
            ProjectilePool?.Spawn(origin, dir.normalized, gameObject, false, Definition.AttackDamage);
            return;
        }

        float dist = Vector3.Distance(Flatten(transform.position), Flatten(target.position));
        if (dist <= Definition.AttackRange + 0.35f)
        {
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            CombatSystem.Instance?.ApplyDamage(damageable, DamageInfo.Create(Definition.AttackDamage, gameObject));
        }
    }

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
