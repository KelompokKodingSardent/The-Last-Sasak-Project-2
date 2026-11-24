using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Equipment/Weapon Definition")]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string WeaponId = "weapon_default";
    public string WeaponName = "Sasak Blade";

    [Header("Visual & Animation")]
    public GameObject WeaponPrefab;
    public AnimatorOverrideController AnimatorOverride;

    [Header("Three-Stage Combo Chain")]
    public AttackDefinitionSO[] ComboChain;

    [Header("Weapon Modifiers")]
    [Min(0.1f)] public float AttackSpeedMultiplier = 1f;
    public float FlatAttackBonus = 0f;
    public float AttackPowerMultiplier = 1f;
    public float StaggerMultiplier = 1f;
    public float MoveSpeedMultiplier = 1f;
    public float ParryWindowBonus = 0f;
}