using UnityEngine;

public class CombatResolver : MonoBehaviour
{
    ParryResolver parryResolver;

    void Awake()
    {
        parryResolver = GetComponent<ParryResolver>();
        if (parryResolver == null)
            parryResolver = gameObject.AddComponent<ParryResolver>();
    }

    public DamageResult Resolve(IDamageable target, DamageInfo info)
    {
        if (target == null)
            return DamageResult.Ignored;

        MonoBehaviour targetBehaviour = target as MonoBehaviour;
        GameObject targetObject = targetBehaviour != null ? targetBehaviour.gameObject : null;

        if (parryResolver != null && targetObject != null &&
            parryResolver.TryResolve(targetObject, info, out IParryable parryable))
        {
            parryable?.OnParried(targetObject.transform);
            CameraEventManager.TriggerCameraShake(1.2f);
            TimeEffectController.Play(0.04f);
            return DamageResult.Parried();
        }

        DamageResult result = target.TakeDamage(info);
        if (result.Outcome == DamageOutcome.Applied || result.Outcome == DamageOutcome.Killed)
        {
            if (targetObject != null && info.Stagger > 0f)
                targetObject.GetComponent<StaggerComponent>()?.Add(info.Stagger);
            TimeEffectController.Play(0.03f);
        }

        return result;
    }
}
