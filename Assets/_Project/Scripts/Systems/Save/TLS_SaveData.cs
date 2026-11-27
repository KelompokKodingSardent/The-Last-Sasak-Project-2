using System;
using System.Collections.Generic;

namespace TheLastSasak.Systems.Save
{
    [Serializable]
    public class PlayerProgressionSaveData
    {
        public int PermanentLevel = 1;
        public List<string> PermanentUpgradeIds = new List<string>();

        public void EnsureInitialized()
        {
            if (PermanentLevel < 1) PermanentLevel = 1;
            PermanentUpgradeIds ??= new List<string>();
        }
    }

    [Serializable]
    public class CurrencySaveData
    {
        public int Souls = 0;
    }

    [Serializable]
    public class SettingsSaveData
    {
        public float MasterVolume = 1.0f;
        public float SfxVolume = 1.0f;
        public float MusicVolume = 1.0f;
    }

    [Serializable]
    public class TLS_SaveData
    {
        public const int LatestSaveVersion = 1;

        public int SaveVersion = LatestSaveVersion;
        public string LastSavedUtc = string.Empty;

        public PlayerProgressionSaveData PlayerProgressionData = new PlayerProgressionSaveData();
        public CurrencySaveData CurrencyData = new CurrencySaveData();
        public InventorySaveData InventorySaveData = new InventorySaveData();
        public EquipmentSaveData EquipmentSaveData = new EquipmentSaveData();
        public SettingsSaveData SettingsData = new SettingsSaveData();

        public void EnsureValidStructure()
        {
            PlayerProgressionData ??= new PlayerProgressionSaveData();
            PlayerProgressionData.EnsureInitialized();

            CurrencyData ??= new CurrencySaveData();
            if (CurrencyData.Souls < 0) CurrencyData.Souls = 0;

            InventorySaveData ??= new InventorySaveData();
            InventorySaveData.EnsureInitialized();

            EquipmentSaveData ??= new EquipmentSaveData();
            EquipmentSaveData.EnsureInitialized();

            SettingsData ??= new SettingsSaveData();
        }
    }
}