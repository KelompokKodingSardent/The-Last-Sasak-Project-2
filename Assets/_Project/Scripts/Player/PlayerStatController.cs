using System;
using UnityEngine;

public class PlayerStatController : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] float baseMaxHealth = 100f;
    [SerializeField] float baseAttackMultiplier = 1f;
    [SerializeField] float baseAttackSpeed = 1f;
    [SerializeField] float baseMoveSpeed = 6f;
    [SerializeField] float baseStaggerMultiplier = 1f;

    [Header("Runtime Computed Stats (Read-Only)")]
    [SerializeField] float finalMaxHealth;
    [SerializeField] float finalAttackMultiplier;
    [SerializeField] float finalFlatAttackBonus;
    [SerializeField] float finalAttackSpeed;
    [SerializeField] float finalMoveSpeed;
    [SerializeField] float finalDamageReduction;
    [SerializeField] float finalPoiseResistance;
    [SerializeField] float finalStaggerMultiplier;
    [SerializeField] float finalDodgeRecoveryMultiplier;
    [SerializeField] float finalParryWindowBonus;

    public float FinalMaxHealth => finalMaxHealth;
    public float FinalAttackMultiplier => finalAttackMultiplier;
    public float FinalFlatAttackBonus => finalFlatAttackBonus;
    public float FinalAttackSpeed => finalAttackSpeed;
    public float FinalMoveSpeed => finalMoveSpeed;
    public float FinalDamageReduction => finalDamageReduction;
    public float FinalPoiseResistance => finalPoiseResistance;
    public float FinalStaggerMultiplier => finalStaggerMultiplier;
    public float FinalDodgeRecoveryMultiplier => finalDodgeRecoveryMultiplier;
    public float FinalParryWindowBonus => finalParryWindowBonus;

    public event Action OnStatsRecalculated;

    Health health;
    EquipmentController equipment;
    ArtifactController artifacts;

    float permanentHealthBonus;
    float runLevelHealthBonus;
    bool initialized;

    void Awake()
    {
        health = GetComponent<Health>();
        equipment = GetComponent<EquipmentController>();
        artifacts = GetComponent<ArtifactController>();
    }

    void Start()
    {
        RecalculateStats(resetHealthToFull: true);
        initialized = true;
    }

    public void SetPermanentHealthBonus(float bonus)
    {
        permanentHealthBonus = Mathf.Max(0f, bonus);
        RecalculateStats(resetHealthToFull: !initialized);
    }

    public void SetRunLevelHealthBonus(float bonus)
    {
        runLevelHealthBonus = Mathf.Max(0f, bonus);
        RecalculateStats(resetHealthToFull: false);
    }

    public void RecalculateStats(bool resetHealthToFull = false)
    {
        float hpFlat = baseMaxHealth + permanentHealthBonus + runLevelHealthBonus;
        float atkMult = baseAttackMultiplier;
        float atkFlat = 0f;
        float atkSpeed = baseAttackSpeed;
        float moveSpeed = baseMoveSpeed;
        float dmgRed = 0f;
        float poise = 0f;
        float staggerMult = baseStaggerMultiplier;
        float dodgeRecMult = 1f;
        float parryBonus = 0f;

        if (equipment != null)
        {
            WeaponDefinitionSO weapon = equipment.ActiveWeapon;
            if (weapon != null)
            {
                atkFlat += weapon.FlatAttackBonus;
                atkMult *= Mathf.Max(0.1f, weapon.AttackPowerMultiplier);
                atkSpeed *= Mathf.Max(0.1f, weapon.AttackSpeedMultiplier);
                moveSpeed *= Mathf.Max(0.1f, weapon.MoveSpeedMultiplier);
                staggerMult *= Mathf.Max(0.1f, weapon.StaggerMultiplier);
                parryBonus += weapon.ParryWindowBonus;
            }

            ArmorDefinitionSO armor = equipment.EquippedArmor;
            if (armor != null)
            {
                hpFlat += armor.MaxHealthBonus;
                dmgRed = Mathf.Clamp(dmgRed + armor.DamageReduction, 0f, 0.8f);
                poise = Mathf.Clamp01(poise + armor.PoiseResistance);
                moveSpeed *= Mathf.Max(0.1f, armor.MoveSpeedMultiplier);
                dodgeRecMult *= Mathf.Max(0.2f, armor.DodgeRecoveryMultiplier);
            }
        }

        if (artifacts != null)
        {
            hpFlat += artifacts.GetTotalMaxHealthBonus();
            atkMult *= artifacts.GetPassiveDamageMultiplier();
        }

        finalMaxHealth = Mathf.Max(1f, hpFlat);
        finalFlatAttackBonus = atkFlat;
        finalAttackMultiplier = atkMult;
        finalAttackSpeed = atkSpeed;
        finalMoveSpeed = moveSpeed;
        finalDamageReduction = dmgRed;
        finalPoiseResistance = poise;
        finalStaggerMultiplier = staggerMult;
        finalDodgeRecoveryMultiplier = dodgeRecMult;
        finalParryWindowBonus = parryBonus;

        if (health != null)
        {
            if (resetHealthToFull)
                health.Configure(finalMaxHealth);
            else
                health.SetMaxHealth(finalMaxHealth, healDifference: true);
        }

        OnStatsRecalculated?.Invoke();
    }

    public float ComputeOutgoingDamage(float rawAttackDamage)
    {
        return Mathf.Max(0f, (rawAttackDamage + finalFlatAttackBonus) * finalAttackMultiplier);
    }

    public float ComputeIncomingDamage(float rawIncomingDamage)
    {
        return Mathf.Max(0f, rawIncomingDamage * (1f - finalDamageReduction));
    }
}