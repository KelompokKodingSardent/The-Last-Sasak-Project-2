using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Legacy Fallback Archetype")]
    [Tooltip("Digunakan jika RoomController tidak memakai EncounterCompositionSO.")]
    public EnemyArchetype Archetype = EnemyArchetype.Melee;

    [Header("Phase 05 — Spatial Spawn Tags")]
    [Tooltip("Contoh: Center, OuterPerimeter, RangedPosition, ElitePosition, AmbushPosition")]
    public List<string> SpawnTags = new List<string>();

    [Header("Gizmo Visualization")]
    public bool ShowGizmos = true;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || SpawnTags == null)
            return false;

        string target = tag.Trim();
        for (int i = 0; i < SpawnTags.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(SpawnTags[i]) &&
                string.Equals(SpawnTags[i].Trim(), target, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAnyTag()
    {
        if (SpawnTags == null)
            return false;

        for (int i = 0; i < SpawnTags.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(SpawnTags[i]))
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (!ShowGizmos)
            return;

        Gizmos.color = ResolveGizmoColor();
        Vector3 pos = transform.position;

        Gizmos.DrawWireSphere(pos + Vector3.up * 0.25f, 0.55f);
        Gizmos.DrawLine(pos, pos + Vector3.up * 1.6f);
        Gizmos.DrawLine(pos + Vector3.up * 0.5f, pos + Vector3.up * 0.5f + transform.forward.normalized * 1.1f);
    }

    private Color ResolveGizmoColor()
    {
        if (HasTag("ElitePosition"))
            return new Color(0.85f, 0.2f, 0.95f, 0.9f);
        if (HasTag("RangedPosition"))
            return new Color(0.2f, 0.75f, 1f, 0.9f);
        if (HasTag("AmbushPosition") || HasTag("OuterPerimeter"))
            return new Color(1f, 0.55f, 0.15f, 0.9f);

        return new Color(1f, 0.25f, 0.25f, 0.9f);
    }
}