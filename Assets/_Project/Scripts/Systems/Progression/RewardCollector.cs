using UnityEngine;

public class RewardCollector : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
    }

    void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
    }

    void HandleEntityDied(Health health)
    {
        if (health == null || health.CompareTag("Player"))
            return;

        EnemyBrain brain = health.GetComponent<EnemyBrain>();
        int exp = brain != null && brain.Definition != null ? brain.Definition.ExpReward : 10;
        int souls = brain != null && brain.Definition != null ? brain.Definition.SoulsReward : 1;
        ExperienceSystem.Instance?.Add(exp);
        CurrencyWallet.Instance?.Add(souls);
    }
}
