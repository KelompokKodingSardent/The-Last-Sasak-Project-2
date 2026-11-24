using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Equipment/Armor Definition")]
public class ArmorDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string ArmorId = "armor_default";
    public string ArmorName = "Sasak Tunic";

    [Header("Visual (Optional)")]
    public Material ArmorMaterial;
    public GameObject ArmorVisualPrefab;

    [Header("Defense & Mobility Modifiers")]
    public float MaxHealthBonus = 0f;
    [Range(0f, 0.75f)] public float DamageReduction = 0f;
    [Range(0f, 1f)] public float PoiseResistance = 0f;
    [Min(0.5f)] public float MoveSpeedMultiplier = 1f;
    [Min(0.5f)] public float DodgeRecoveryMultiplier = 1f;
}