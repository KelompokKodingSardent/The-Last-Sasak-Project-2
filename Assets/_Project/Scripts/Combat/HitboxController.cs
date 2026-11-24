using System.Collections.Generic;
using UnityEngine;

public class HitboxController : MonoBehaviour
{
    readonly Collider[] hits = new Collider[24];
    int lastAttackId = -1;
    readonly HashSet<int> hitIds = new HashSet<int>();

    public void BeginAttack(int attackId)
    {
        lastAttackId = attackId;
        hitIds.Clear();
    }

    public int Overlap(Vector3 origin, float radius, LayerMask mask, Collider[] buffer)
    {
        return Physics.OverlapSphereNonAlloc(origin, radius, buffer, mask, QueryTriggerInteraction.Collide);
    }

    public bool TryHit(Collider col, int attackId)
    {
        if (col == null)
            return false;

        if (attackId != lastAttackId)
        {
            lastAttackId = attackId;
            hitIds.Clear();
        }

        Health targetHealth = col.GetComponentInParent<Health>();
        int id = targetHealth != null
            ? targetHealth.GetInstanceID()
            : (col.attachedRigidbody != null ? col.attachedRigidbody.GetInstanceID() : col.gameObject.GetInstanceID());

        return hitIds.Add(id);
    }

    public Collider[] Buffer => hits;
}