using UnityEngine;
using TheLastSasak.Items.Loot;
using TheLastSasak.Systems.Save;

public class CurrencyWallet : MonoBehaviour, ICurrencyReceiver, ISaveableSystem
{
    public static CurrencyWallet Instance { get; private set; }

    public int SoulsCoin { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TryRegisterSaveable();
    }

    private void Start()
    {
        TryRegisterSaveable();
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
        data.CurrencyData.Souls = Mathf.Max(0, SoulsCoin);
    }

    public void ReadFromSave(TLS_SaveData data)
    {
        if (data == null)
            return;

        data.EnsureValidStructure();
        Set(data.CurrencyData.Souls);
    }

    public void Set(int amount)
    {
        SoulsCoin = Mathf.Max(0, amount);
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
    }

    public void AddCurrency(int amount) => Add(amount);

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        SoulsCoin += amount;
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
        SaveSystem.SaveNow();
    }

    public bool Spend(int amount)
    {
        if (amount <= 0 || SoulsCoin < amount)
            return false;

        SoulsCoin -= amount;
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
        SaveSystem.SaveNow();
        return true;
    }

    public void ResetRun()
    {
        // Souls bersifat meta-currency permanen antar-run
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}