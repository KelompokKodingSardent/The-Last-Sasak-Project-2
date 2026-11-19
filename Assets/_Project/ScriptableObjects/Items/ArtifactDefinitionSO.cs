using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Items/Artifact Definition")]
public class ArtifactDefinitionSO : ScriptableObject
{
    public string Id = "vitality";
    public string DisplayName = "Vitality Charm";
    public float HealthBonus = 20f;
    public float DamageMultiplier = 1.1f;
}
