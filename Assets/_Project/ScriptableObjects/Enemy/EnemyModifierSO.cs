using UnityEngine;

public enum EnemyModifierType
{
    Swift,
    Armored,
    Aggressive,
    Empowered,
    Tactical,
    Custom
}

[CreateAssetMenu(fileName = "NewEnemyModifier", menuName = "TLS/Enemy/Enemy Modifier")]
public class EnemyModifierSO : ScriptableObject
{
    [Header("Identitas Modifier")]
    public string ModifierName = "Swift";
    public EnemyModifierType ModifierType = EnemyModifierType.Swift;

    [Header("Multiplier Ritme & Statistik")]
    [Min(0.5f)] public float MoveSpeedMultiplier = 1.0f;
    [Min(0.5f)] public float RepositionSpeedMultiplier = 1.0f;
    [Min(0.3f)] public float AttackCooldownMultiplier = 1.0f;
    [Min(0.3f)] public float DecisionDelayMultiplier = 1.0f;
    [Min(0.5f)] public float DamageMultiplier = 1.0f;

    [Header("Perubahan Perilaku Taktis")]
    [Range(0f, 0.6f)] public float BonusStaggerResistance = 0f;
    public bool GrantFrontalGuard = false;
    public bool GrantSideDash = false;
    [Range(0f, 1f)] public float BonusFlankRepositionChance = 0f;
    public bool ForceRepositionAfterAttack = false;
    [Range(0f, 0.5f)] public float BonusChainChance = 0f;

    [Header("Bonus Attack Pattern (Opsional)")]
    public EnemyAttackPatternSO ExtraAttackPattern;

    public static EnemyModifierSO CreateRuntime(EnemyModifierType type)
    {
        EnemyModifierSO mod = CreateInstance<EnemyModifierSO>();
        mod.ModifierType = type;
        mod.ModifierName = type.ToString();

        switch (type)
        {
            case EnemyModifierType.Swift:
                mod.MoveSpeedMultiplier = 1.30f;
                mod.RepositionSpeedMultiplier = 1.40f;
                mod.DecisionDelayMultiplier = 0.75f;
                mod.GrantSideDash = true;
                break;

            case EnemyModifierType.Armored:
                mod.MoveSpeedMultiplier = 0.90f;
                mod.BonusStaggerResistance = 0.30f;
                mod.GrantFrontalGuard = true;
                break;

            case EnemyModifierType.Aggressive:
                mod.MoveSpeedMultiplier = 1.15f;
                mod.AttackCooldownMultiplier = 0.65f;
                mod.DecisionDelayMultiplier = 0.55f;
                mod.DamageMultiplier = 1.15f;
                mod.BonusChainChance = 0.30f;
                break;

            case EnemyModifierType.Empowered:
                mod.MoveSpeedMultiplier = 1.15f;
                mod.DamageMultiplier = 1.25f;
                mod.AttackCooldownMultiplier = 0.85f;
                break;

            case EnemyModifierType.Tactical:
                mod.RepositionSpeedMultiplier = 1.25f;
                mod.GrantSideDash = true;
                mod.BonusFlankRepositionChance = 0.35f;
                mod.ForceRepositionAfterAttack = true;
                break;
        }

        return mod;
    }
}