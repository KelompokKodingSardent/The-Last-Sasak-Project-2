using System.Collections;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public AttackDefinitionSO Definition;
    public Transform AttackPoint;
    public LayerMask TargetMask;

    public int CurrentComboStep { get; private set; }
    public bool IsInComboWindow { get; private set; }
    public bool CanChainNextAttack => IsInComboWindow && activeAttack != null && GetNextComboAttack() != null;

    PlayerActionController actions;
    PlayerInputReader input;
    PlayerFacingController facing;
    PlayerMotor motor;
    PlayerStatController stats;
    EquipmentController equipment;
    ArtifactController artifact;
    HitboxController hitbox;
    PlayerAnimationController anim;

    Coroutine routine;
    AttackDefinitionSO activeAttack;
    AttackDefinitionSO[] activeChain;

    void Awake()
    {
        actions = GetComponent<PlayerActionController>();
        input = GetComponent<PlayerInputReader>();
        facing = GetComponent<PlayerFacingController>();
        motor = GetComponent<PlayerMotor>();
        stats = GetComponent<PlayerStatController>();
        equipment = GetComponent<EquipmentController>();
        artifact = GetComponent<ArtifactController>();
        hitbox = GetComponent<HitboxController>();
        anim = GetComponent<PlayerAnimationController>();

        if (AttackPoint == null)
            AttackPoint = transform;
        if (Definition == null)
            Definition = AttackDefinitionSO.CreateRuntime("Slash", 20f, 1.4f);
        if (TargetMask == 0)
            TargetMask = GameplayLayers.EnemyMask;
    }

    public AttackDefinitionSO CurrentActiveDefinition => activeAttack != null ? activeAttack : Definition;

    public bool TryAttack()
    {
        if (actions == null)
            return false;

        if (!actions.CanEnter(PlayerActionState.Attacking))
            return false;

        AttackDefinitionSO nextToExecute;
        int nextStep;

        if (actions.State == PlayerActionState.Attacking && CanChainNextAttack)
        {
            nextToExecute = GetNextComboAttack();
            nextStep = CurrentComboStep + 1;
        }
        else
        {
            RefreshComboChainSource();
            nextToExecute = GetComboStage(0);
            nextStep = 1;
        }

        if (nextToExecute == null)
            return false;

        input?.ConsumeAttack();

        if (routine != null)
            StopCoroutine(routine);

        activeAttack = nextToExecute;
        CurrentComboStep = nextStep;
        IsInComboWindow = false;
        routine = StartCoroutine(AttackRoutine(activeAttack));
        return true;
    }

    void RefreshComboChainSource()
    {
        if (equipment != null && equipment.ActiveWeapon != null &&
            equipment.ActiveWeapon.ComboChain != null && equipment.ActiveWeapon.ComboChain.Length > 0)
        {
            activeChain = equipment.ActiveWeapon.ComboChain;
            return;
        }

        if (equipment != null && equipment.Loadout != null && equipment.Loadout.Length > 0)
        {
            activeChain = equipment.Loadout;
            return;
        }

        activeChain = null;
    }

    AttackDefinitionSO GetComboStage(int zeroBasedIndex)
    {
        if (activeChain != null && zeroBasedIndex >= 0 && zeroBasedIndex < activeChain.Length)
            return activeChain[zeroBasedIndex];
        return zeroBasedIndex == 0 ? Definition : null;
    }

    AttackDefinitionSO GetNextComboAttack()
    {
        if (activeAttack != null && activeAttack.NextAttack != null)
            return activeAttack.NextAttack;

        return GetComboStage(CurrentComboStep);
    }

    IEnumerator AttackRoutine(AttackDefinitionSO attackDef)
    {
        actions.SetState(PlayerActionState.Attacking);

        if (facing != null)
        {
            facing.SnapToAimDirection();
            facing.SetRotationLocked(true);
        }

        if (motor != null && attackDef.ForwardLungeSpeed > 0f)
        {
            motor.ApplyImpulse(transform.forward * attackDef.ForwardLungeSpeed, attackDef.ForwardLungeDuration);
        }

        anim?.PlayAttack();

        int attackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 1;
        hitbox?.BeginAttack(attackId);

        float speedMult = stats != null ? Mathf.Max(0.1f, stats.FinalAttackSpeed) : 1f;
        float startupDuration = attackDef.Startup / speedMult;
        float activeDuration = attackDef.Active / speedMult;
        float recoveryDuration = attackDef.Recovery / speedMult;
        float totalDuration = Mathf.Max(0.05f, startupDuration + activeDuration + recoveryDuration);
        float comboOpenElapsed = totalDuration * Mathf.Clamp01(attackDef.ComboWindowStartNormalized);

        float elapsed = 0f;

        // 1. Startup Phase
        while (elapsed < startupDuration && actions.State == PlayerActionState.Attacking)
        {
            elapsed += Time.deltaTime;
            UpdateComboWindow(elapsed, comboOpenElapsed);
            if (TryExecuteBufferedCombo())
                yield break;
            yield return null;
        }

        // 2. Active Phase
        float activeEnd = startupDuration + activeDuration;
        while (elapsed < activeEnd && actions.State == PlayerActionState.Attacking)
        {
            ResolveHits(attackDef, attackId);
            elapsed += Time.deltaTime;
            UpdateComboWindow(elapsed, comboOpenElapsed);
            if (TryExecuteBufferedCombo())
                yield break;
            yield return null;
        }

        // 3. Recovery Phase
        while (elapsed < totalDuration && actions.State == PlayerActionState.Attacking)
        {
            elapsed += Time.deltaTime;
            UpdateComboWindow(elapsed, comboOpenElapsed);
            if (TryExecuteBufferedCombo())
                yield break;
            yield return null;
        }

        CleanupAttackState();
    }

    void UpdateComboWindow(float elapsed, float comboOpenElapsed)
    {
        if (!IsInComboWindow && elapsed >= comboOpenElapsed)
        {
            IsInComboWindow = true;
            facing?.SetRotationLocked(false);
        }
    }

    bool TryExecuteBufferedCombo()
    {
        if (CanChainNextAttack && input != null && input.HasBufferedAction(BufferedActionType.Attack))
        {
            return TryAttack();
        }
        return false;
    }

    void CleanupAttackState()
    {
        IsInComboWindow = false;
        CurrentComboStep = 0;
        activeAttack = null;
        facing?.SetRotationLocked(false);

        if (actions != null && actions.State == PlayerActionState.Attacking)
            actions.SetState(PlayerActionState.Idle);

        routine = null;
    }

    public void CancelAttack()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        IsInComboWindow = false;
        CurrentComboStep = 0;
        activeAttack = null;
        facing?.SetRotationLocked(false);
    }

    void ResolveHits(AttackDefinitionSO attackDef, int attackId)
    {
        if (hitbox == null)
            return;

        Vector3 origin = (AttackPoint != null && AttackPoint != transform)
            ? AttackPoint.position
            : transform.position + Vector3.up * 0.9f + transform.forward * attackDef.Range;

        int count = hitbox.Overlap(origin, attackDef.Radius, TargetMask, hitbox.Buffer);
        for (int i = 0; i < count; i++)
        {
            Collider col = hitbox.Buffer[i];
            if (col == null || col.transform.IsChildOf(transform))
                continue;
            if (!hitbox.TryHit(col, attackId))
                continue;

            float damage = attackDef.Damage;
            if (artifact != null)
                damage = artifact.EvaluateContextualDamage(damage, col.gameObject);

            DamageInfo info = DamageInfo.Create(damage, gameObject);
            info.AttackId = attackId;
            info.Stagger = attackDef.Stagger;

            DamageResult result = CombatSystem.Instance != null
                ? CombatSystem.Instance.ApplyDamage(col, info)
                : DamageResult.Ignored;

            if (result.Outcome == DamageOutcome.Applied || result.Outcome == DamageOutcome.Killed)
            {
                if (attackDef.HitstopDuration > 0f)
                    TimeEffectController.Play(attackDef.HitstopDuration);
                if (attackDef.CameraShakeIntensity > 0f)
                    CameraEventManager.TriggerCameraShake(attackDef.CameraShakeIntensity);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        AttackDefinitionSO def = activeAttack != null ? activeAttack : Definition;
        if (def == null)
            return;

        Vector3 origin = (AttackPoint != null && AttackPoint != transform)
            ? AttackPoint.position
            : transform.position + Vector3.up * 0.9f + transform.forward * def.Range;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, def.Radius);
    }
}