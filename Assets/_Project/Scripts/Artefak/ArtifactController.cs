using UnityEngine;

public interface IStatModifier
{
    float Modify(float baseValue);
}

public class ArtifactController : MonoBehaviour, IStatModifier
{
    public ArtifactDefinitionSO Artifact;
    public float DamageMultiplier = 1f;
    public float HealthBonus;

    void Start()
    {
        if (Artifact != null)
            Equip(Artifact);
    }

    public void Equip(ArtifactDefinitionSO definition)
    {
        Artifact = definition;
        if (definition == null)
            return;

        DamageMultiplier = definition.DamageMultiplier;
        HealthBonus = definition.HealthBonus;
        Health health = GetComponent<Health>();
        if (health != null && HealthBonus > 0f)
            health.Configure(health.MaxHealth + HealthBonus);
    }

    public float Modify(float baseValue)
    {
        return baseValue * (DamageMultiplier <= 0f ? 1f : DamageMultiplier);
    }

    public float ApplyDamage(float amount)
    {
        return Modify(amount);
    }
}
