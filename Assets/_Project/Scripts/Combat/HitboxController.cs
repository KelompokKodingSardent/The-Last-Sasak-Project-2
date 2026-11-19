using UnityEngine;

public class HitboxController : MonoBehaviour
{
    readonly Collider[] hits = new Collider[16];
    int lastAttackId = -1;
    readonly System.Collections.Generic.HashSet<int> hitIds = new System.Collections.Generic.HashSet<int>();

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

        int id = col.transform.root.GetInstanceID();
        if (attackId != lastAttackId)
        {
            lastAttackId = attackId;
            hitIds.Clear();
        }

        return hitIds.Add(id);
    }

    public Collider[] Buffer => hits;
}
