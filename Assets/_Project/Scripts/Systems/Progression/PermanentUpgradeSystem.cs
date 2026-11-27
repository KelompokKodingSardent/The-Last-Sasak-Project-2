using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TheLastSasak.Player.Stats;
using TheLastSasak.Systems.Save;

public class PermanentUpgradeSystem : MonoBehaviour, ISaveableSystem
{
    private const string HealthUpgradePrefix = "max_health_";

    public int HealthCost = 15;
    public float HealthBonus = 15f;

    private readonly List<float> _purchasedHealthBonuses = new List<float>();
    private float _accumulatedBonus;

    public float AccumulatedHealthBonus => _accumulatedBonus;

    private void OnEnable()
    {
        TryRegisterSaveable();
    }

    private void Start()
    {
        TryRegisterSaveable();
        ApplyBonusToActivePlayer();
    }

    private void OnDisable()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.UnregisterSaveable(this);
    }

    private void TryRegisterSaveable()
    {
        SaveSystem save = SaveSystem.Instance != null
            ? SaveSystem.Instance
            : FindFirstObjectByType<SaveSystem>();

        if (save != null)
            save.RegisterSaveable(this);
    }

    public void WriteToSave(TLS_SaveData data)
    {
        if (data == null)
            return;

        data.EnsureValidStructure();
        List<string> ids = data.PlayerProgressionData.PermanentUpgradeIds;
        ids.RemoveAll(id => !string.IsNullOrEmpty(id) && id.StartsWith(HealthUpgradePrefix));

        for (int i = 0; i < _purchasedHealthBonuses.Count; i++)
        {
            string formatted = _purchasedHealthBonuses[i].ToString(CultureInfo.InvariantCulture);
            ids.Add($"{HealthUpgradePrefix}{formatted}");
        }
    }

    public void ReadFromSave(TLS_SaveData data)
    {
        if (data == null)
            return;

        data.EnsureValidStructure();
        _purchasedHealthBonuses.Clear();
        _accumulatedBonus = 0f;

        List<string> ids = data.PlayerProgressionData.PermanentUpgradeIds;
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id) || !id.StartsWith(HealthUpgradePrefix))
                continue;

            string rawNumber = id.Substring(HealthUpgradePrefix.Length);
            if (float.TryParse(rawNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedBonus) && parsedBonus > 0f)
            {
                _purchasedHealthBonuses.Add(parsedBonus);
                _accumulatedBonus += parsedBonus;
            }
        }

        ApplyBonusToActivePlayer();
    }

    public void BuyForPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            TryBuyHealth(player.GetComponent<Health>());
    }

    public bool TryBuyHealth(Health playerHealth)
    {
        if (playerHealth == null || CurrencyWallet.Instance == null || HealthBonus <= 0f)
            return false;

        if (!CurrencyWallet.Instance.Spend(HealthCost))
            return false;

        _purchasedHealthBonuses.Add(HealthBonus);
        _accumulatedBonus += HealthBonus;

        ApplyBonusToPlayer(playerHealth);
        SaveSystem.SaveNow();
        return true;
    }

    private void ApplyBonusToActivePlayer()
    {
        if (_accumulatedBonus <= 0f)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ApplyBonusToPlayer(player.GetComponent<Health>());
        }
    }

    private void ApplyBonusToPlayer(Health playerHealth)
    {
        if (playerHealth == null)
            return;

        PlayerStatController statController = playerHealth.GetComponent<PlayerStatController>();
        if (statController != null)
            statController.SetPermanentHealthBonus(_accumulatedBonus);
        else
            playerHealth.SetMaxHealth(playerHealth.MaxHealth + HealthBonus, healDifference: false);
    }
}