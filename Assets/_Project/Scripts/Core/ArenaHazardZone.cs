using UnityEngine;

public class ArenaHazardZone : MonoBehaviour
{
    private GameObject _owner;
    private float _radius;
    private float _damagePerTick;
    private float _duration;
    private float _tickInterval;
    private LayerMask _playerLayerMask;
    private int _attackId;

    private float _elapsed;
    private float _tickTimer;
    private readonly Collider[] _hitBuffer = new Collider[4];

    public static ArenaHazardZone Create(
        Vector3 position,
        float radius,
        float damagePerTick,
        float duration,
        float tickInterval,
        GameObject owner,
        LayerMask playerMask,
        GameObject customPrefab = null)
    {
        GameObject root;
        if (customPrefab != null)
        {
            root = Instantiate(customPrefab, position, Quaternion.identity);
        }
        else
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "RuntimeHazardZone";
            Collider col = root.GetComponent<Collider>();
            if (col != null) Destroy(col);

            root.transform.position = new Vector3(position.x, position.y + 0.02f, position.z);
            root.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            Renderer rend = root.GetComponent<Renderer>();
            if (rend != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                Color hazardCol = new Color(0.65f, 0.15f, 0.9f, 0.55f);
                block.SetColor("_BaseColor", hazardCol);
                block.SetColor("_Color", hazardCol);
                rend.SetPropertyBlock(block);
            }
        }

        ArenaHazardZone zone = root.GetComponent<ArenaHazardZone>();
        if (zone == null)
            zone = root.AddComponent<ArenaHazardZone>();

        zone.Initialize(radius, damagePerTick, duration, tickInterval, owner, playerMask);
        return zone;
    }

    private void Initialize(float radius, float damage, float duration, float tickInterval, GameObject owner, LayerMask mask)
    {
        _radius = Mathf.Max(0.4f, radius);
        _damagePerTick = damage;
        _duration = Mathf.Max(0.2f, duration);
        _tickInterval = Mathf.Max(0.2f, tickInterval);
        _owner = owner;
        _playerLayerMask = mask;
        _tickTimer = 0.05f; // Tick awal segera setelah muncul
        _attackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 0;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _elapsed += dt;
        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        _tickTimer -= dt;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickInterval;
            ApplyZoneTick();
        }
    }

    private void ApplyZoneTick()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hitBuffer, _playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            DamageInfo info = new DamageInfo
            {
                Amount = _damagePerTick,
                Instigator = _owner != null ? _owner : gameObject,
                HitPoint = transform,
                CanBeParried = false,
                Stagger = 8f,
                AttackId = _attackId
            };

            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.ApplyDamage(col, info);
            }
            else
            {
                IDamageable dmg = col.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(info);
            }
        }
    }
}