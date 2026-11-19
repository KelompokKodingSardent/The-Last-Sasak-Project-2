using UnityEngine;

public class Projectile : MonoBehaviour, IParryable
{
    public float Speed = 10f;
    public float Damage = 8f;
    public float Life = 4f;
    public GameObject Owner;
    public bool FromPlayer;

    Vector3 direction;
    float age;
    bool spent;
    Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    public void Launch(Vector3 dir, GameObject owner, bool fromPlayer, float damage)
    {
        direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        Owner = owner;
        FromPlayer = fromPlayer;
        Damage = damage;
        age = 0f;
        spent = false;
        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.position += direction * Speed * Time.deltaTime;
        age += Time.deltaTime;
        if (age >= Life)
            Despawn();
    }

    void OnTriggerEnter(Collider other)
    {
        if (spent || other == null)
            return;
        if (Owner != null && other.transform.root == Owner.transform)
            return;

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

        if (other.CompareTag("Enemy") || other.GetComponentInParent<Health>() != null)
        {
            CombatSystem.Instance?.ApplyDamage(other, DamageInfo.Create(Damage, Owner != null ? Owner : gameObject, false));
            spent = true;
            Despawn();
        }
    }

    public void OnParried(Transform parrySource)
    {
        FromPlayer = true;
        Owner = parrySource != null ? parrySource.gameObject : Owner;
        direction = -direction;
        spent = false;
        age = 0f;
    }

    void Despawn()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}
