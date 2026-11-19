using UnityEngine;

public class CurrencyWallet : MonoBehaviour
{
    public static CurrencyWallet Instance { get; private set; }

    public int SoulsCoin { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Set(int amount)
    {
        SoulsCoin = Mathf.Max(0, amount);
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
    }

    public void Add(int amount)
    {
        SoulsCoin += Mathf.Max(0, amount);
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
        SaveSystem.SaveNow();
    }

    public bool Spend(int amount)
    {
        if (SoulsCoin < amount)
            return false;
        SoulsCoin -= amount;
        GameEvents.RaiseCurrencyChanged(SoulsCoin);
        SaveSystem.SaveNow();
        return true;
    }

    public void ResetRun()
    {
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
