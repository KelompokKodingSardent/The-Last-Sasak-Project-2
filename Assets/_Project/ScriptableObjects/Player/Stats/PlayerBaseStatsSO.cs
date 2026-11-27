using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Player.Stats
{
    [CreateAssetMenu(fileName = "PlayerBaseStats_Default", menuName = "The Last Sasak/Stats/Player Base Stats")]
    public class PlayerBaseStatsSO : ScriptableObject
    {
        [Header("Core Combat & Survival Stats")]
        [Min(1f)]
        [SerializeField] private float maxHealth = 100f;
        [Min(0f)]
        [SerializeField] private float attackDamage = 25f;
        [Min(0.1f)]
        [SerializeField] private float attackSpeed = 1.0f;

        [Header("Defense & Poise")]
        [Range(0f, 0.85f)]
        [SerializeField] private float damageReduction = 0f;
        [Min(0f)]
        [SerializeField] private float poiseResistance = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0f;

        [Header("Mobility & Stamina")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 5.0f;
        [Min(0f)]
        [SerializeField] private float staminaRegen = 15.0f;

        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float DamageReduction => damageReduction;
        public float PoiseResistance => poiseResistance;
        public float KnockbackResistance => knockbackResistance;
        public float MoveSpeed => moveSpeed;
        public float StaminaRegen => staminaRegen;

        public void PopulateDictionary(Dictionary<StatType, float> target)
        {
            target[StatType.MaxHealth] = maxHealth;
            target[StatType.AttackDamage] = attackDamage;
            target[StatType.AttackSpeed] = attackSpeed;
            target[StatType.DamageReduction] = damageReduction;
            target[StatType.PoiseResistance] = poiseResistance;
            target[StatType.KnockbackResistance] = knockbackResistance;
            target[StatType.MoveSpeed] = moveSpeed;
            target[StatType.StaminaRegen] = staminaRegen;
        }
    }
}