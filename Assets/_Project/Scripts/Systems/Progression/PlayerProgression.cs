using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public float HealthPerLevel = 8f;
    Health health;
    int appliedLevel = 1;

    void Awake()
    {
        health = GetComponent<Health>();
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
        if (health == null || level <= appliedLevel)
            return;

        int gained = level - appliedLevel;
        appliedLevel = level;
        health.Configure(health.MaxHealth + HealthPerLevel * gained);
    }
}
