// PermanentUpgradeSystem.cs
using UnityEngine;
using TheLastSasak.Systems.Save;
using TheLastSasak.Player.Stats;

public class PermanentUpgradeSystem : MonoBehaviour
{
    public int HealthCost = 15;
    public float HealthBonus = 15f;

    float accumulatedBonus;

    public void BuyForPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            TryBuyHealth(player.GetComponent<Health>());
    }

    public bool TryBuyHealth(Health playerHealth)
    {
        if (playerHealth == null || CurrencyWallet.Instance == null)
            return false;
        if (!CurrencyWallet.Instance.Spend(HealthCost))
            return false;

        accumulatedBonus += HealthBonus;
        PlayerStatController statController = playerHealth.GetComponent<PlayerStatController>();
        if (statController != null)
            statController.SetPermanentHealthBonus(accumulatedBonus);
        else
            playerHealth.SetMaxHealth(playerHealth.MaxHealth + HealthBonus, healDifference: true);

        SaveSystem.Instance?.AddPermanentHealth(HealthBonus);
        return true;
    }
}