using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackController : MonoBehaviour, IParryable
{
    [Header("Referensi Komponen (Assign di Inspector)")]
    [SerializeField] private GroundTelegraphView telegraphView;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask playerLayerMask;

    private readonly Collider[] _hitBuffer = new Collider[8];
    private readonly HashSet<GameObject> _hitThisSwing = new HashSet<GameObject>();

    private EnemyAttackPatternSO _activePattern;
    private AttackSwingStep _activeStep;
    private ProjectilePool _projectilePool;
    private Transform _currentTarget;
    private Vector3 _lockedZonePosition;
    private bool _isExecutingHitbox;
    private bool _parriedThisSwing;
    private int _currentAttackId;

    public bool IsHitboxActive => _isExecutingHitbox;

    public event Action<float> OnAttackParried;

    private void Awake()
    {
        if (attackOrigin == null)
            attackOrigin = transform;
    }

    public void SetProjectilePool(ProjectilePool pool)
    {
        _projectilePool = pool;
    }

    public void BeginTelegraph(EnemyAttackPatternSO pattern, AttackSwingStep step, Transform target = null)
    {
        _activePattern = pattern;
        _activeStep = step;
        _currentTarget = target;
        _isExecutingHitbox = false;
        _parriedThisSwing = false;

        if (step.telegraphShape == TelegraphShape.TargetedZone)
        {
            if (step.deliveryType == AttackDeliveryType.SupportEmpower)
                _lockedZonePosition = transform.position;
            else
                _lockedZonePosition = target != null ? target.position : transform.position + transform.forward * 4f;

            if (telegraphView != null)
                telegraphView.Show(step, pattern.canBeParried, _lockedZonePosition);
        }
        else
        {
            if (telegraphView != null)
                telegraphView.Show(step, pattern.canBeParried);
        }
    }

    public void UpdateTelegraphProgress(float normalizedProgress)
    {
        if (_activeStep != null &&
            _activeStep.telegraphShape == TelegraphShape.TargetedZone &&
            _activeStep.deliveryType != AttackDeliveryType.SupportEmpower &&
            _activeStep.trackPlayerDuringTelegraph &&
            _currentTarget != null &&
            normalizedProgress < 0.8f) // Kunci posisi di 20% akhir agar player punya jendela dodge yang adil
        {
            _lockedZonePosition = _currentTarget.position;
            if (telegraphView != null)
                telegraphView.UpdateWorldPosition(_lockedZonePosition);
        }

        if (telegraphView != null)
            telegraphView.SetProgress(normalizedProgress);
    }

    public void BeginStartup()
    {
        if (telegraphView != null)
            telegraphView.TriggerStartupFlash();
    }

    public void BeginExecution()
    {
        _hitThisSwing.Clear();
        _parriedThisSwing = false;
        _currentAttackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 0;

        if (telegraphView != null)
            telegraphView.Hide();

        if (_activePattern == null || _activeStep == null) return;

        switch (_activeStep.deliveryType)
        {
            case AttackDeliveryType.Melee:
                _isExecutingHitbox = true;
                break;

            case AttackDeliveryType.Projectile:
                _isExecutingHitbox = false;
                FireProjectiles();
                break;

            case AttackDeliveryType.TargetedZone:
                _isExecutingHitbox = false;
                ExecuteTargetedZone();
                break;

            case AttackDeliveryType.SupportEmpower:
                _isExecutingHitbox = false;
                ExecuteSupportEmpower();
                break;
        }
    }

    public void EndExecution()
    {
        _isExecutingHitbox = false;
        _hitThisSwing.Clear();
    }

    public void AbortAttack()
    {
        _isExecutingHitbox = false;
        _hitThisSwing.Clear();
        _activePattern = null;
        _activeStep = null;

        if (telegraphView != null)
            telegraphView.Hide();
    }

    public void OnParried(Transform parrier)
    {
        NotifyParried();
    }

    public void NotifyParried()
    {
        if (_parriedThisSwing) return;
        _parriedThisSwing = true;

        float staggerBonus = _activePattern != null ? _activePattern.parryStaggerBonus : 35f;
        AbortAttack();
        OnAttackParried?.Invoke(staggerBonus);
    }

    private void FixedUpdate()
    {
        if (!_isExecutingHitbox || _activePattern == null || _activeStep == null)
            return;

        if (_activeStep.deliveryType == AttackDeliveryType.Melee)
            EvaluateMeleeHitbox();
    }

    private void FireProjectiles()
    {
        if (_projectilePool == null)
            _projectilePool = FindFirstObjectByType<ProjectilePool>();

        if (_projectilePool == null) return;

        Vector3 spawnPos = attackOrigin.position + Vector3.up * 0.9f;
        Vector3 baseDir = transform.forward;

        if (_currentTarget != null)
        {
            Vector3 toTarget = _currentTarget.position - attackOrigin.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                baseDir = toTarget.normalized;
        }

        int count = Mathf.Max(1, _activeStep.projectileCount);
        float finalDamage = GetScaledDamage();

        if (count == 1)
        {
            _projectilePool.Spawn(spawnPos, baseDir, gameObject, false, finalDamage);
            return;
        }

        float totalSpread = _activeStep.projectileSpreadAngle;
        float startAngle = -totalSpread * 0.5f;
        float stepAngle = count > 1 ? totalSpread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + stepAngle * i;
            Vector3 shotDir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            _projectilePool.Spawn(spawnPos, shotDir, gameObject, false, finalDamage);
        }
    }

    private void ExecuteTargetedZone()
    {
        float finalDamage = GetScaledDamage();

        if (_activeStep.zonePersistDuration > 0.05f)
        {
            // Controller — Summon Hazard (area berbahaya bertahan beberapa detik)
            ArenaHazardZone.Create(
                _lockedZonePosition,
                _activeStep.zoneRadius,
                finalDamage,
                _activeStep.zonePersistDuration,
                _activeStep.zoneTickInterval,
                gameObject,
                playerLayerMask,
                _activeStep.customHazardPrefab
            );
            return;
        }

        // Ranger Ground Mark / Controller Projectile Rain (ledakan area instan)
        int count = Physics.OverlapSphereNonAlloc(_lockedZonePosition, _activeStep.zoneRadius, _hitBuffer, playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            GameObject targetObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (_hitThisSwing.Contains(targetObj)) continue;

            _hitThisSwing.Add(targetObj);
            DeliverDamage(col, finalDamage, false);
        }
    }

    private void ExecuteSupportEmpower()
    {
        if (CombatDirector.Instance == null) return;

        IReadOnlyList<EnemyBrain> enemies = CombatDirector.Instance.ActiveEnemies;
        float radiusSqr = _activeStep.empowerRadius * _activeStep.empowerRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBrain ally = enemies[i];
            if (ally == null || ally.gameObject == gameObject || ally.CurrentState == EnemyBrainState.Dead)
                continue;

            float sqrDist = (ally.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= radiusSqr)
            {
                ally.ApplyEmpower(
                    _activeStep.empowerDuration,
                    _activeStep.empowerSpeedMultiplier,
                    _activeStep.empowerDamageMultiplier
                );
            }
        }
    }

    private float GetScaledDamage()
    {
        float mult = 1f;
        EnemyBrain brain = GetComponent<EnemyBrain>();
        if (brain != null)
            mult = brain.DamageMultiplier;

        return _activePattern.baseDamage * _activeStep.damageMultiplier * mult;
    }

    private void EvaluateMeleeHitbox()
    {
        Vector3 origin = attackOrigin.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        int count = Physics.OverlapSphereNonAlloc(origin, _activeStep.range, _hitBuffer, playerLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _hitBuffer[i];
            if (col == null) continue;

            GameObject targetObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (_hitThisSwing.Contains(targetObj)) continue;

            if (!IsInsideShape(origin, forward, targetObj.transform.position, _activeStep))
                continue;

            _hitThisSwing.Add(targetObj);
            DeliverDamage(col, GetScaledDamage(), _activePattern.canBeParried);
        }
    }

    private bool IsInsideShape(Vector3 origin, Vector3 forward, Vector3 targetPos, AttackSwingStep step)
    {
        Vector3 toTarget = targetPos - origin;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist > step.range) return false;

        switch (step.telegraphShape)
        {
            case TelegraphShape.Circle:
            case TelegraphShape.TargetedZone:
                return true;

            case TelegraphShape.Cone:
                if (dist < 0.01f) return true;
                float angle = Vector3.Angle(forward, toTarget.normalized);
                return angle <= step.angle * 0.5f;

            case TelegraphShape.Line:
                float forwardDot = Vector3.Dot(forward, toTarget);
                if (forwardDot < 0f || forwardDot > step.range) return false;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float lateralOffset = Mathf.Abs(Vector3.Dot(right, toTarget));
                return lateralOffset <= step.width * 0.5f;

            default:
                return true;
        }
    }

    private void DeliverDamage(Collider targetCol, float finalDamage, bool canBeParried)
    {
        DamageInfo info = new DamageInfo
        {
            Amount = finalDamage,
            Instigator = gameObject,
            HitPoint = attackOrigin,
            CanBeParried = canBeParried,
            Stagger = _activeStep.staggerDamage,
            AttackId = _currentAttackId
        };

        DamageResult result;
        if (CombatSystem.Instance != null)
        {
            result = CombatSystem.Instance.ApplyDamage(targetCol, info);
        }
        else
        {
            IDamageable damageable = targetCol.GetComponentInParent<IDamageable>();
            result = damageable != null ? damageable.TakeDamage(info) : DamageResult.Ignored;
        }

        if (result.Outcome == DamageOutcome.Parried)
        {
            NotifyParried();
        }
    }
}