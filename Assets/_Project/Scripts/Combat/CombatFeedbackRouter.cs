using UnityEngine;

public static class CombatFeedbackRouter
{
    private const float DefaultHitHitstop = 0.03f;
    private const float DefaultParryHitstop = 0.07f;
    private const float DefaultParryShake = 1.2f;
    private const float KillHitstopMultiplier = 1.35f;
    private const float KillShakeMultiplier = 1.25f;

    public static void EmitParryFeedback(GameObject defender, GameObject instigator)
    {
        float shake = DefaultParryShake;
        float hitstop = DefaultParryHitstop;

        AttackDefinitionSO attackDef = ResolveAttackDefinition(instigator);
        if (attackDef != null)
        {
            if (attackDef.CameraShakeIntensity > 0f)
                shake = Mathf.Max(DefaultParryShake, attackDef.CameraShakeIntensity * 1.35f);
            if (attackDef.HitstopDuration > 0f)
                hitstop = Mathf.Max(DefaultParryHitstop, attackDef.HitstopDuration * 1.5f);
        }

        CameraEventManager.TriggerCameraShake(shake);
        TimeEffectController.PlayRequest(new HitstopRequest(hitstop, 0.03f, priority: 2));
    }

    public static void EmitHitFeedback(DamageInfo info, DamageResult result, GameObject targetObject)
    {
        if (result.Outcome != DamageOutcome.Applied && result.Outcome != DamageOutcome.Killed)
            return;

        bool isKill = result.Outcome == DamageOutcome.Killed;
        AttackDefinitionSO attackDef = ResolveAttackDefinition(info.Instigator);

        float hitstop = attackDef != null ? attackDef.HitstopDuration : DefaultHitHitstop;
        float shake = attackDef != null ? attackDef.CameraShakeIntensity : 0f;

        if (shake <= 0f && targetObject != null && targetObject.CompareTag("Player"))
        {
            shake = 0.45f;
        }

        if (isKill)
        {
            hitstop *= KillHitstopMultiplier;
            shake *= KillShakeMultiplier;
        }

        if (shake > 0f)
        {
            CameraEventManager.TriggerCameraShake(shake);
        }

        if (hitstop > 0f)
        {
            int priority = isKill ? 1 : 0;
            TimeEffectController.PlayRequest(new HitstopRequest(hitstop, 0.04f, priority));
        }
    }

    private static AttackDefinitionSO ResolveAttackDefinition(GameObject instigator)
    {
        if (instigator == null)
            return null;

        AttackController playerAttack = instigator.GetComponentInParent<AttackController>();
        if (playerAttack != null && playerAttack.CurrentActiveDefinition != null)
            return playerAttack.CurrentActiveDefinition;

        return null;
    }
}