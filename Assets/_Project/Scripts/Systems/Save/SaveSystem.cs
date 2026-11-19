using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int SaveVersion = 1;
    public int SoulsCoin;
    public float PermanentHealthBonus;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    const string FileName = "tls_save.json";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Apply(Load());
    }

    public static void SaveNow()
    {
        if (Instance == null)
            new GameObject("SaveSystem").AddComponent<SaveSystem>();
        Instance.Save();
    }

    public void Save()
    {
        SaveData data = new SaveData
        {
            SaveVersion = SaveValidator.CurrentVersion,
            SoulsCoin = CurrencyWallet.Instance != null ? CurrencyWallet.Instance.SoulsCoin : 0,
            PermanentHealthBonus = PermanentHealth
        };

        File.WriteAllText(Path(), JsonUtility.ToJson(data));
    }

    public float PermanentHealth { get; private set; }

    public SaveData Load()
    {
        string path = Path();
        if (!File.Exists(path))
            return new SaveData();

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return SaveValidator.IsValid(data) ? data : new SaveData();
        }
        catch
        {
            return new SaveData();
        }
    }

    void Apply(SaveData data)
    {
        PermanentHealth = data.PermanentHealthBonus;
        CurrencyWallet.Instance?.Set(data.SoulsCoin);
    }

    public void AddPermanentHealth(float amount)
    {
        PermanentHealth += amount;
        Save();
    }

    static string Path() => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
