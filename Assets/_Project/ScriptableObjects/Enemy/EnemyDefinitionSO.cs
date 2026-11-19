using UnityEngine;

public enum EnemyArchetype
{
    Melee,
    Ranged,
    Elite
}

[CreateAssetMenu(menuName = "TLS/Enemy/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string DisplayName = "Enemy";
    public EnemyArchetype Archetype = EnemyArchetype.Melee;
    public float MaxHealth = 40f;
    public float MoveSpeed = 3.5f;
    public float DetectRange = 12f;
    public float AttackRange = 1.8f;
    public float AttackDamage = 12f;
    public float AttackCooldown = 1.2f;
    public int ExpReward = 10;
    public int SoulsReward = 2;
    public AttackDefinitionSO Attack;

    public static EnemyDefinitionSO CreateRuntime(string name, EnemyArchetype type)
    {
        EnemyDefinitionSO so = CreateInstance<EnemyDefinitionSO>();
        so.name = name;
        so.DisplayName = name;
        so.Archetype = type;
        if (type == EnemyArchetype.Ranged)
        {
            so.AttackRange = 8f;
            so.DetectRange = 14f;
            so.AttackDamage = 8f;
            so.MaxHealth = 28f;
        }
        else if (type == EnemyArchetype.Elite)
        {
            so.MaxHealth = 160f;
            so.AttackDamage = 22f;
            so.MoveSpeed = 3.2f;
        }

        return so;
    }
}
