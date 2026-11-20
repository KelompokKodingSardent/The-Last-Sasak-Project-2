using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype
{
    // Legacy archetypes (dipakai EnemySpawnPoint & RoomEncounterController)
    Melee,
    Ranged,
    Elite,

    // Phase 1+ archetypes
    Vanguard,
    Skirmisher,
    Ranger,
    Bulwark,
    Controller
}

public enum PreferredCombatZone
{
    Front,
    Flank,
    Rear,
    Ranged
}

[CreateAssetMenu(fileName = "NewEnemyDefinition", menuName = "TLS/Enemy/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Header("Identitas & Statistik Dasar (Legacy + Core)")]
    public string DisplayName = "Enemy";
    public EnemyArchetype Archetype = EnemyArchetype.Vanguard;
    public float MaxHealth = 40f;
    public float MoveSpeed = 3.5f;
    public float DetectRange = 12f;
    public float AttackRange = 1.8f;
    public float AttackDamage = 12f;
    public float AttackCooldown = 1.2f;
    public int ExpReward = 10;
    public int SoulsReward = 2;
    public AttackDefinitionSO Attack;

    [Header("Phase 1 — Positioning & Movement")]
    public float IdealCombatDistance = 2.0f;
    public float SpacingTolerance = 0.45f;
    public float StrafeSpeed = 2.2f;
    public float RepositionSpeed = 3.6f;
    public float RotationSpeed = 12f;

    [Header("Phase 1 — Stagger & Decision Rhythm")]
    public float StaggerPunishDuration = 2.0f;
    public Vector2 AttackDecisionDelay = new Vector2(0.25f, 0.55f);

    [Header("Phase 2 — Tactical Flanking & Spacing")]
    public PreferredCombatZone PreferredZone = PreferredCombatZone.Front;
    [Range(30f, 160f)] public float FlankAngle = 95f;
    public bool AllowRearFlank = false;
    [Range(15f, 90f)] public float PlayerFacingAvoidAngle = 45f;
    [Range(0f, 1f)] public float FlankRepositionChance = 0.65f;

    [Header("Phase 2 — Skirmisher Side Dash & Separation")]
    public bool CanSideDash = false;
    public float SideDashDistance = 2.2f;
    public float SideDashDuration = 0.18f;
    public float SideDashCooldown = 2.6f;
    public float SeparationRadius = 1.35f;
    public float SeparationStrength = 2.4f;

    [Header("Phase 3 — Ranger Spacing & Line of Sight")]
    [Tooltip("Jika player mendekat lebih dari jarak ini, Ranger akan mundur / Retreat Shot")]
    public float MinSafeDistance = 0f;
    public bool RequireLineOfSight = false;
    public LayerMask ObstacleLayerMask;

    [Header("Phase 3 — Bulwark Heavy Defense")]
    [Range(0f, 0.85f)] public float BaseStaggerResistance = 0f;
    public bool HasFrontalGuard = false;
    [Range(45f, 160f)] public float FrontalGuardAngle = 110f;

    [Header("Phase 1-3 — Attack Patterns")]
    public List<EnemyAttackPatternSO> AttackPatterns = new List<EnemyAttackPatternSO>();

    public static EnemyDefinitionSO CreateRuntime(string name, EnemyArchetype type)
    {
        EnemyDefinitionSO so = CreateInstance<EnemyDefinitionSO>();
        so.name = name;
        so.DisplayName = name;
        so.Archetype = type;

        if (type == EnemyArchetype.Skirmisher)
        {
            so.MaxHealth = 32f;
            so.MoveSpeed = 4.3f;
            so.StrafeSpeed = 3.1f;
            so.RepositionSpeed = 4.6f;
            so.AttackRange = 1.7f;
            so.IdealCombatDistance = 2.1f;
            so.AttackDamage = 10f;
            so.PreferredZone = PreferredCombatZone.Flank;
            so.FlankAngle = 100f;
            so.AllowRearFlank = true;
            so.CanSideDash = true;
            so.AttackDecisionDelay = new Vector2(0.20f, 0.42f);
        }
        else if (type == EnemyArchetype.Ranged || type == EnemyArchetype.Ranger)
        {
            so.AttackRange = 8.5f;
            so.IdealCombatDistance = 7.5f;
            so.MinSafeDistance = 3.8f;
            so.RequireLineOfSight = true;
            so.DetectRange = 14f;
            so.AttackDamage = 9f;
            so.MaxHealth = 30f;
            so.MoveSpeed = 3.4f;
            so.StrafeSpeed = 2.8f;
            so.RepositionSpeed = 4.0f;
            so.PreferredZone = PreferredCombatZone.Ranged;
        }
        else if (type == EnemyArchetype.Elite || type == EnemyArchetype.Bulwark)
        {
            so.MaxHealth = 140f;
            so.AttackDamage = 20f;
            so.MoveSpeed = 2.4f;
            so.StrafeSpeed = 1.1f;
            so.RepositionSpeed = 2.2f;
            so.RotationSpeed = 6.5f;
            so.AttackRange = 2.4f;
            so.IdealCombatDistance = 2.2f;
            so.BaseStaggerResistance = 0.35f;
            so.HasFrontalGuard = true;
            so.PreferredZone = PreferredCombatZone.Front;
        }
        else if (type == EnemyArchetype.Controller)
        {
            so.MaxHealth = 36f;
            so.AttackDamage = 10f;
            so.MoveSpeed = 3.1f;
            so.AttackRange = 9.5f;
            so.IdealCombatDistance = 8.5f;
            so.MinSafeDistance = 4.2f;
            so.DetectRange = 15f;
            so.PreferredZone = PreferredCombatZone.Ranged;
        }

        return so;
    }
}