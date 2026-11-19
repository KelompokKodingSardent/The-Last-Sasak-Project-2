using System.Collections;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    public AttackDefinitionSO Definition;
    public Transform AttackPoint;
    public LayerMask TargetMask;

    PlayerActionController actions;
    HitboxController hitbox;
    PlayerAnimationController anim;
    Coroutine routine;

    void Awake()
    {
        actions = GetComponent<PlayerActionController>();
        hitbox = GetComponent<HitboxController>();
        anim = GetComponent<PlayerAnimationController>();
        if (AttackPoint == null)
            AttackPoint = transform;
        if (Definition == null)
            Definition = AttackDefinitionSO.CreateRuntime("Slash", 20f, 1.4f);
        if (TargetMask == 0)
            TargetMask = GameplayLayers.EnemyMask;
    }

    public bool TryAttack()
    {
        if (Definition == null || actions == null)
            return false;
        if (!actions.CanEnter(PlayerActionState.Attacking))
            return false;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AttackRoutine());
        return true;
    }

    IEnumerator AttackRoutine()
    {
        actions.SetState(PlayerActionState.Attacking);
        anim?.PlayAttack();
        int attackId = CombatSystem.Instance != null ? CombatSystem.Instance.NextAttackId() : 1;
        hitbox.BeginAttack(attackId);

        yield return new WaitForSeconds(Definition.Startup);

        float active = Definition.Active;
        while (active > 0f && actions.State == PlayerActionState.Attacking)
        {
            ResolveHits(attackId);
            active -= Time.deltaTime;
            yield return null;
        }

        if (actions.State == PlayerActionState.Attacking)
            yield return new WaitForSeconds(Definition.Recovery);

        if (actions.State == PlayerActionState.Attacking)
            actions.SetState(PlayerActionState.Idle);

        routine = null;
    }

    void ResolveHits(int attackId)
    {
        if (AttackPoint == null)
            return;

        int count = hitbox.Overlap(AttackPoint.position, Definition.Radius, TargetMask, hitbox.Buffer);
        for (int i = 0; i < count; i++)
        {
            Collider col = hitbox.Buffer[i];
            if (col.transform.root == transform)
                continue;
            if (!hitbox.TryHit(col, attackId))
                continue;

            float damage = Definition.Damage;
            ArtifactController artifact = GetComponent<ArtifactController>();
            if (artifact != null)
                damage = artifact.ApplyDamage(damage);
            DamageInfo info = DamageInfo.Create(damage, gameObject);
            info.AttackId = attackId;
            info.Stagger = Definition.Stagger;
            DamageResult result = CombatSystem.Instance != null
                ? CombatSystem.Instance.ApplyDamage(col, info)
                : DamageResult.Ignored;

            if (result.Outcome == DamageOutcome.Applied || result.Outcome == DamageOutcome.Killed)
                CameraEventManager.TriggerCameraShake(0.6f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (AttackPoint == null || Definition == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackPoint.position, Definition.Radius);
    }
}
