using UnityEngine;

public class PermanentUpgradeSystem : MonoBehaviour
{
    public int HealthCost = 15;
    public float HealthBonus = 15f;

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

        playerHealth.Configure(playerHealth.MaxHealth + HealthBonus);
        SaveSystem.Instance?.AddPermanentHealth(HealthBonus);
        return true;
    }
}
