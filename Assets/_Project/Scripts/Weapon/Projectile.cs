using UnityEngine;

public class Projectile : MonoBehaviour, IParryable, IPoolable
{
    public float Speed = 10f;
    public float Damage = 8f;
    public float Life = 4f;
    public float DeflectSpeedMultiplier = 1.45f;
    public float DeflectDamageMultiplier = 1.5f;
    public GameObject Owner;
    public bool FromPlayer;

    public PoolLeaseId CurrentLeaseId { get; private set; }

    private Vector3 _direction;
    private float _currentSpeed;
    private float _age;
    private bool _spent;
    private Collider _col;
    private TrailRenderer _trail;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col != null)
            _col.isTrigger = true;
        _trail = GetComponentInChildren<TrailRenderer>(true);
    }

    public void OnRentFromPool(PoolLeaseId leaseId)
    {
        CurrentLeaseId = leaseId;
        _age = 0f;
        _spent = false;
        if (_trail != null)
            _trail.Clear();
    }

    public void OnReturnToPool()
    {
        CurrentLeaseId = default;
        Owner = null;
        FromPlayer = false;
        _spent = true;
        _age = 0f;
        if (_trail != null)
            _trail.Clear();
    }

    public void Launch(Vector3 dir, GameObject owner, bool fromPlayer, float damage)
    {
        _direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        Owner = owner;
        FromPlayer = fromPlayer;
        Damage = damage;
        _currentSpeed = Speed;
        _age = 0f;
        _spent = false;

        if (_trail != null)
            _trail.Clear();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_spent)
            return;

        transform.position += _direction * _currentSpeed * Time.deltaTime;
        if (_direction.sqrMagnitude > 0.01f)
            transform.forward = _direction;

        _age += Time.deltaTime;
        if (_age >= Life)
            Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_spent || other == null)
            return;

        if (Owner != null && (other.gameObject == Owner || other.transform.IsChildOf(Owner.transform)))
            return;

        int envLayer = GameplayLayers.Environment;
        if (envLayer >= 0 && other.gameObject.layer == envLayer && !other.isTrigger)
        {
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
            Despawn();
        }
    }

    public void OnParried(Transform parrySource)
    {
        if (_spent)
            return;

        GameObject originalShooter = Owner;
        FromPlayer = true;
        Owner = parrySource != null ? parrySource.gameObject : Owner;
        Damage *= DeflectDamageMultiplier;
        _currentSpeed = Speed * DeflectSpeedMultiplier;
        _spent = false;
        _age = 0f;

        if (parrySource != null)
        {
            PlayerFacingController facing = parrySource.GetComponent<PlayerFacingController>();
            if (facing != null)
            {
                _direction = facing.GetAimDirection();
            }
            else if (originalShooter != null)
            {
                Vector3 toShooter = originalShooter.transform.position - transform.position;
                toShooter.y = 0f;
                _direction = toShooter.sqrMagnitude > 0.01f ? toShooter.normalized : -_direction;
            }
            else
            {
                _direction = -_direction;
            }
        }
        else
        {
            _direction = -_direction;
        }
    }

    private void Despawn()
    {
        if (_spent)
            return;

        _spent = true;
        Owner = null;
        FromPlayer = false;

        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}