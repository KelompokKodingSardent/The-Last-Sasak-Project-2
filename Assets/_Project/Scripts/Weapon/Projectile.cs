using UnityEngine;

public class Projectile : MonoBehaviour, IParryable
{
    public float Speed = 10f;
    public float Damage = 8f;
    public float Life = 4f;
    public float DeflectSpeedMultiplier = 1.45f;
    public float DeflectDamageMultiplier = 1.5f;
    public GameObject Owner;
    public bool FromPlayer;

    Vector3 direction;
    float currentSpeed;
    float age;
    bool spent;
    Collider col;
    TrailRenderer trail;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        trail = GetComponentInChildren<TrailRenderer>();
    }

    public void Launch(Vector3 dir, GameObject owner, bool fromPlayer, float damage)
    {
        direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        Owner = owner;
        FromPlayer = fromPlayer;
        Damage = damage;
        currentSpeed = Speed;
        age = 0f;
        spent = false;

        if (trail != null)
            trail.Clear();

        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.position += direction * currentSpeed * Time.deltaTime;
        if (direction.sqrMagnitude > 0.01f)
            transform.forward = direction;

        age += Time.deltaTime;
        if (age >= Life)
            Despawn();
    }

    void OnTriggerEnter(Collider other)
    {
        if (spent || other == null)
            return;

        if (Owner != null && (other.gameObject == Owner || other.transform.IsChildOf(Owner.transform)))
            return;

        int envLayer = GameplayLayers.Environment;
        if (envLayer >= 0 && other.gameObject.layer == envLayer && !other.isTrigger)
        {
            spent = true;
            Despawn();
            return;
        }

        if (!FromPlayer)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null && health.CompareTag("Player"))
            {
                DamageInfo info = DamageInfo.Create(Damage, gameObject);
                info.CanBeParried = true;
                DamageResult result = CombatSystem.Instance != null
                    ? CombatSystem.Instance.ApplyDamage(health, info)
                    : health.TakeDamage(info);

                if (result.Outcome == DamageOutcome.Parried)
                    return;

                spent = true;
                Despawn();
            }
            return;
        }

        Health enemyHealth = other.GetComponentInParent<Health>();
        if (other.CompareTag("Enemy") || (enemyHealth != null && !enemyHealth.CompareTag("Player")))
        {
            DamageInfo info = DamageInfo.Create(Damage, Owner != null ? Owner : gameObject, false);
            info.Stagger = 15f;
            CombatSystem.Instance?.ApplyDamage(other, info);
            spent = true;
            Despawn();
        }
    }

    public void OnParried(Transform parrySource)
    {
        GameObject originalShooter = Owner;
        FromPlayer = true;
        Owner = parrySource != null ? parrySource.gameObject : Owner;
        Damage *= DeflectDamageMultiplier;
        currentSpeed = Speed * DeflectSpeedMultiplier;
        spent = false;
        age = 0f;

        if (parrySource != null)
        {
            PlayerFacingController facing = parrySource.GetComponent<PlayerFacingController>();
            if (facing != null)
            {
                direction = facing.GetAimDirection();
            }
            else if (originalShooter != null)
            {
                Vector3 toShooter = originalShooter.transform.position - transform.position;
                toShooter.y = 0f;
                direction = toShooter.sqrMagnitude > 0.01f ? toShooter.normalized : -direction;
            }
            else
            {
                direction = -direction;
            }
        }
        else
        {
            direction = -direction;
        }
    }

    void Despawn()
    {
        Owner = null;
        FromPlayer = false;
        spent = true;

        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}