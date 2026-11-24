// PlayerProgression.cs
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public float HealthPerLevel = 8f;
    Health health;
    PlayerStatController statController;
    int appliedLevel = 1;

    void Awake()
    {
        health = GetComponent<Health>();
        statController = GetComponent<PlayerStatController>();
    }

    void OnEnable()
    {
        GameEvents.OnExperienceChanged += Handle;
    }

    void OnDisable()
    {
        GameEvents.OnExperienceChanged -= Handle;
    }

    void Handle(int exp, int level)
    {
        if (level <= appliedLevel)
            return;

        appliedLevel = level;
        float totalLevelBonus = HealthPerLevel * (appliedLevel - 1);

        if (statController != null)
            statController.SetRunLevelHealthBonus(totalLevelBonus);
        else if (health != null)
            health.SetMaxHealth(health.MaxHealth + HealthPerLevel, healDifference: true);
    }
}