using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Player.Stats;

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
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int NextAttackId()
    {
        return nextAttackId++;
    }

    public DamageResult ApplyDamage(IDamageable target, DamageInfo info)
    {
        if (target == null)
            return DamageResult.Ignored;

        if (resolver != null)
            return resolver.Resolve(target, info);

        return target.TakeDamage(info);
    }

    public DamageResult ApplyDamage(Collider targetCollider, DamageInfo info)
    {
        if (targetCollider == null)
            return DamageResult.Ignored;

        IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
        return ApplyDamage(damageable, info);
    }

    public DamageResult ApplyDamage(GameObject targetObject, DamageInfo info)
    {
        if (targetObject == null)
            return DamageResult.Ignored;

        IDamageable damageable = targetObject.GetComponentInParent<IDamageable>();
        return ApplyDamage(damageable, info);
    }
}

public class CombatResolver : MonoBehaviour
{
    private ParryResolver _parryResolver;

    private void Awake()
    {
        _parryResolver = GetComponent<ParryResolver>();
    }

    public DamageResult Resolve(IDamageable target, DamageInfo info)
    {
        if (target == null)
            return DamageResult.Ignored;

        MonoBehaviour targetBehaviour = target as MonoBehaviour;
        GameObject targetObject = targetBehaviour != null ? targetBehaviour.gameObject : null;

        if (_parryResolver != null && targetObject != null &&
            _parryResolver.TryResolve(targetObject, info, out IParryable parryable))
        {
            targetObject.GetComponent<ParryController>()?.OnParrySuccess(info.Instigator);
            targetObject.GetComponent<ArtifactController>()?.NotifyParrySuccess(info.Instigator);
            parryable?.OnParried(targetObject.transform);
            CombatFeedbackRouter.EmitParryFeedback(targetObject, info.Instigator);
            return DamageResult.Parried();
        }

        if (info.Instigator != null)
        {
            PlayerStatController attackerStats = info.Instigator.GetComponent<PlayerStatController>();
            if (attackerStats != null)
            {
                info.Amount = attackerStats.ComputeOutgoingDamage(info.Amount);
                info.Stagger *= attackerStats.FinalStaggerMultiplier;
            }
        }

        if (targetObject != null)
        {
            PlayerStatController defenderStats = targetObject.GetComponent<PlayerStatController>();
            if (defenderStats != null)
            {
                info.Amount = defenderStats.ComputeIncomingDamage(info.Amount);
            }
        }

        DamageResult result = target.TakeDamage(info);
        if (result.Outcome == DamageOutcome.Applied || result.Outcome == DamageOutcome.Killed)
        {
            if (targetObject != null && info.Stagger > 0f)
                targetObject.GetComponent<StaggerComponent>()?.Add(info.Stagger);

            CombatFeedbackRouter.EmitHitFeedback(info, result, targetObject);
        }

        return result;
    }
}