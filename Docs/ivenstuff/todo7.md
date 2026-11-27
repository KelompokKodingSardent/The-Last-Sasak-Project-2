Implementasi **Phase 7 — Persistence & Save/Load** memperluas penyimpanan JSON lokal (`tls_save.json`) menggunakan ID item permanen (`ItemId`), validasi resolusi aset melalui `ItemCatalogSO`, migrasi versi save (`SaveVersion`), dan urutan validasi 10 langkah saat *load* tanpa menyimpan referensi `ScriptableObject` maupun `FinalStats` secara langsung.

### Struktur Target Phase 7

```text
Assets/_Project/Scripts/Systems/Save/
├── InventorySaveData.cs
├── EquipmentSaveData.cs          (Mencakup EquipmentSaveData & ArtifactSaveData)
├── TLS_SaveData.cs               (Root schema tls_save.json & SaveVersion)
├── SaveVersionMigrator.cs        (Penanganan migrasi versi save lama — Test P06)
├── ItemPersistenceCoordinator.cs (Validasi 10 langkah, resolusi ItemCatalogSO & ownership check)
└── SaveSystem.cs                 (Read/Write JSON atomik & lifecycle coordinator)

```

---

### 1. Struktur Data Save (`Systems/Save/`)

#### `InventorySaveData.cs`

Menyimpan indeks slot (`SlotIndex`), `ItemId`, dan `Quantity` agar tata letak 20 slot grid pemain tetap konsisten setelah permainan dimuat ulang (**Test P01** & **P02**).

```csharp
using System;
using System.Collections.Generic;

namespace TheLastSasak.Systems.Save
{
    [Serializable]
    public struct InventorySlotSaveEntry
    {
        public int SlotIndex;
        public string ItemId;
        public int Quantity;

        public InventorySlotSaveEntry(int slotIndex, string itemId, int quantity)
        {
            SlotIndex = slotIndex;
            ItemId = itemId;
            Quantity = quantity;
        }
    }

    [Serializable]
    public class InventorySaveData
    {
        public int Capacity = 20;
        public List<InventorySlotSaveEntry> Items = new List<InventorySlotSaveEntry>();

        public void EnsureInitialized()
        {
            if (Capacity <= 0) Capacity = 20;
            Items ??= new List<InventorySlotSaveEntry>();
        }
    }
}

```

#### `EquipmentSaveData.cs`

Memisahkan data persistensi `EquipmentSaveData` (senjata & armor) dan `ArtifactSaveData` (slot artefak) dari state runtime `PlayerEquipmentState`.

```csharp
using System;
using System.Collections.Generic;

namespace TheLastSasak.Systems.Save
{
    [Serializable]
    public class ArtifactSaveData
    {
        public int MaxSlots = 2;
        public List<string> ArtifactIds = new List<string>();

        public void EnsureInitialized()
        {
            if (MaxSlots <= 0) MaxSlots = 2;
            ArtifactIds ??= new List<string>();
        }
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public string WeaponSlot1Id = string.Empty;
        public string WeaponSlot2Id = string.Empty;
        public int ActiveWeaponSlot = 0; // 0 = None, 1 = Slot1, 2 = Slot2
        public string ArmorId = string.Empty;
        public ArtifactSaveData Artifacts = new ArtifactSaveData();

        public void EnsureInitialized()
        {
            WeaponSlot1Id ??= string.Empty;
            WeaponSlot2Id ??= string.Empty;
            ArmorId ??= string.Empty;
            Artifacts ??= new ArtifactSaveData();
            Artifacts.EnsureInitialized();
        }
    }
}

```

#### `TLS_SaveData.cs`

Struktur root `tls_save.json` yang memisahkan `CurrencyData` (*Souls* permanen), `PlayerProgressionData`, `InventorySaveData`, `EquipmentSaveData`, dan `SettingsData` (**Point 10.2**).

```csharp
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

```

---

### 2. Migrasi Versi Save (`SaveVersionMigrator.cs`)

Menangani file save dari versi skema lama (misalnya `SaveVersion == 0` sebelum integrasi Item Domain v1.0) atau menolak versi masa depan yang tidak kompatibel (**Point 7.4** & **Test P06**).

```csharp
using UnityEngine;

namespace TheLastSasak.Systems.Save
{
    public static class SaveVersionMigrator
    {
        public static bool TryMigrateToLatest(TLS_SaveData data, out string migrationReport)
        {
            if (data == null)
            {
                migrationReport = "SaveData null.";
                return false;
            }

            data.EnsureValidStructure();

            if (data.SaveVersion > TLS_SaveData.LatestSaveVersion)
            {
                migrationReport = $"SaveVersion {data.SaveVersion} lebih baru dari versi aplikasi ({TLS_SaveData.LatestSaveVersion}).";
                Debug.LogError($"[SaveVersionMigrator] {migrationReport}");
                return false;
            }

            int originalVersion = data.SaveVersion;

            // Migrasi dari v0 (pre-Item Domain) ke v1
            if (data.SaveVersion < 1)
            {
                MigrateV0ToV1(data);
                data.SaveVersion = 1;
            }

            // Tambahkan langkah migrasi berikutnya di sini saat LatestSaveVersion naik:
            // if (data.SaveVersion == 1) { MigrateV1ToV2(data); data.SaveVersion = 2; }

            migrationReport = originalVersion == TLS_SaveData.LatestSaveVersion
                ? $"SaveVersion {data.SaveVersion} valid."
                : $"Migrasi SaveVersion {originalVersion} ke {data.SaveVersion} berhasil.";

            if (originalVersion != TLS_SaveData.LatestSaveVersion)
            {
                Debug.Log($"[SaveVersionMigrator] {migrationReport}");
            }

            return true;
        }

        private static void MigrateV0ToV1(TLS_SaveData data)
        {
            // Normalisasi seluruh ItemId lama ke format huruf kecil tanpa spasi
            for (int i = 0; i < data.InventorySaveData.Items.Count; i++)
            {
                InventorySlotSaveEntry entry = data.InventorySaveData.Items[i];
                if (!string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    entry.ItemId = entry.ItemId.Trim().ToLowerInvariant();
                    data.InventorySaveData.Items[i] = entry;
                }
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.WeaponSlot1Id))
            {
                data.EquipmentSaveData.WeaponSlot1Id = data.EquipmentSaveData.WeaponSlot1Id.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.WeaponSlot2Id))
            {
                data.EquipmentSaveData.WeaponSlot2Id = data.EquipmentSaveData.WeaponSlot2Id.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.ArmorId))
            {
                data.EquipmentSaveData.ArmorId = data.EquipmentSaveData.ArmorId.Trim().ToLowerInvariant();
            }
        }
    }
}

```

---

### 3. Penambahan Hook Restore pada Phase 2, 3 & 5

Agar pemulihan data dari save tidak terhalang oleh validasi *combat action state* maupun *starting items* default, tambahkan metode `Restore*` berikut ke masing-masing controller:

#### A. Tambahan pada `InventoryController.cs` (Phase 3)

```csharp
// Tambahkan di dalam class InventoryController:
public void RestoreFromSaveEntries(
    IReadOnlyList<(int slotIndex, ItemDefinitionSO item, int quantity)> entries,
    bool allowDuplicateEquipmentOverride)
{
    _model.ClearAll();

    if (entries != null)
    {
        IReadOnlyList<InventorySlot> slots = _model.Slots;
        for (int i = 0; i < entries.Count; i++)
        {
            var (slotIndex, item, quantity) = entries[i];
            if (item == null || quantity <= 0) continue;

            if (!allowDuplicateEquipmentOverride && item is EquipmentDefinitionSO && _model.HasItem(item))
            {
                continue;
            }

            if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].IsEmpty)
            {
                slots[slotIndex].Set(item, quantity);
            }
            else
            {
                TryAddItem(item, quantity);
            }
        }
    }

    OnInventoryChanged?.Invoke();
}

```

#### B. Tambahan pada `EquipmentController.cs` (Phase 2)

```csharp
// Tambahkan di dalam class EquipmentController:
public void RestoreEquipmentState(
    WeaponDefinitionSO slot1,
    WeaponDefinitionSO slot2,
    WeaponSlotType activeSlot,
    ArmorDefinitionSO armor)
{
    state.Clear();

    if (slot1 != null)
    {
        state.SetWeapon(WeaponSlotType.Slot1, slot1);
        OnWeaponEquipped?.Invoke(WeaponSlotType.Slot1, slot1);
    }

    if (slot2 != null && slot2 != slot1)
    {
        state.SetWeapon(WeaponSlotType.Slot2, slot2);
        OnWeaponEquipped?.Invoke(WeaponSlotType.Slot2, slot2);
    }

    if (armor != null)
    {
        state.SetArmor(armor);
        OnArmorEquipped?.Invoke(armor);
    }

    if (state.GetWeapon(activeSlot) != null)
    {
        state.SetActiveWeaponSlot(activeSlot);
    }
    else if (state.WeaponSlot1 != null)
    {
        state.SetActiveWeaponSlot(WeaponSlotType.Slot1);
    }
    else if (state.WeaponSlot2 != null)
    {
        state.SetActiveWeaponSlot(WeaponSlotType.Slot2);
    }
    else
    {
        state.SetActiveWeaponSlot(WeaponSlotType.None);
    }

    OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
    OnEquipmentChanged?.Invoke();
}

```

#### C. Tambahan pada `ArtifactController.cs` (Phase 5)

```csharp
// Tambahkan di dalam class ArtifactController:
public void RestoreArtifactState(IReadOnlyList<ArtifactDefinitionSO> artifacts)
{
    EnsureArrayInitialized();
    Array.Clear(equippedArtifacts, 0, equippedArtifacts.Length);

    if (artifacts != null)
    {
        int count = Mathf.Min(artifacts.Count, maxArtifactSlots);
        for (int i = 0; i < count; i++)
        {
            ArtifactDefinitionSO artifact = artifacts[i];
            if (artifact != null && !IsArtifactEquipped(artifact))
            {
                equippedArtifacts[i] = artifact;
                OnArtifactEquipped?.Invoke(i, artifact);
            }
        }
    }

    OnArtifactsChanged?.Invoke();
}

```

---

### 4. Item Persistence Coordinator (`ItemPersistenceCoordinator.cs`)

Menjalankan urutan **Point 10.3 (Langkah 3–10)**: memvalidasi struktur data, me-resolve `ItemId` via `ItemCatalogSO`, membuang ID tidak dikenal dengan aman (**Test P05**), memulihkan `Inventory`, memvalidasi kepemilikan sebelum memasang `Equipment` & `Artifacts`, dan memicu kalkulasi ulang `PlayerStatController`.

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Catalog;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Inventory;
using TheLastSasak.Player.Stats;

namespace TheLastSasak.Systems.Save
{
    public class ItemPersistenceCoordinator : MonoBehaviour
    {
        [Header("Catalog Reference (Required)")]
        [SerializeField] private ItemCatalogSO itemCatalog;

        [Header("Domain Controllers")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;
        [SerializeField] private PlayerStatController playerStatController;

        [Header("Persistence Policy (Point 10.4)")]
        [Tooltip("Aktifkan jika koleksi Inventory & Equipment bertahan antar sesi/run.")]
        [SerializeField] private bool persistInventoryAndEquipment = true;

        public bool PersistInventoryAndEquipment
        {
            get => persistInventoryAndEquipment;
            set => persistInventoryAndEquipment = value;
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<Inventory>();
            if (equipmentController == null) equipmentController = GetComponent<EquipmentController>();
            if (artifactController == null) artifactController = GetComponent<ArtifactController>();
            if (playerStatController == null) playerStatController = GetComponent<PlayerStatController>();
        }

        public void CaptureToSaveData(TLS_SaveData saveData)
        {
            if (saveData == null) return;
            saveData.EnsureValidStructure();

            if (!persistInventoryAndEquipment)
            {
                saveData.InventorySaveData.Items.Clear();
                saveData.EquipmentSaveData = new EquipmentSaveData();
                return;
            }

            CaptureInventory(saveData.InventorySaveData);
            CaptureEquipmentAndArtifacts(saveData.EquipmentSaveData);
        }

        public void ApplyFromSaveData(TLS_SaveData saveData)
        {
            if (saveData == null) return;
            saveData.EnsureValidStructure();

            if (itemCatalog == null)
            {
                Debug.LogError("[ItemPersistenceCoordinator] ItemCatalogSO belum dipasang! Pemulihan item dibatalkan.", this);
                return;
            }

            if (!persistInventoryAndEquipment)
            {
                return;
            }

            // Langkah 4, 5 & 6: Resolve ItemId via ItemCatalogSO & pulihkan Inventory
            RestoreInventory(saveData.InventorySaveData);

            // Langkah 7 & 8: Pulihkan Equipment & Artifacts dengan validasi kepemilikan Inventory
            RestoreEquipmentAndArtifacts(saveData.EquipmentSaveData);

            // Langkah 9: Hitung ulang FinalStats dari konfigurasi & modifier aktif (tidak pernah dari JSON)
            if (playerStatController != null)
            {
                playerStatController.RecalculateStats();
            }
        }

        private void CaptureInventory(InventorySaveData invSave)
        {
            invSave.Items.Clear();
            if (inventory == null) return;

            invSave.Capacity = inventory.Capacity;
            IReadOnlyList<InventorySlot> slots = inventory.GetSlots();

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && !slot.IsEmpty && slot.ItemDefinition != null)
                {
                    invSave.Items.Add(new InventorySlotSaveEntry(
                        slot.SlotIndex,
                        slot.ItemDefinition.ItemId,
                        slot.Quantity));
                }
            }
        }

        private void CaptureEquipmentAndArtifacts(EquipmentSaveData eqSave)
        {
            eqSave.WeaponSlot1Id = string.Empty;
            eqSave.WeaponSlot2Id = string.Empty;
            eqSave.ActiveWeaponSlot = 0;
            eqSave.ArmorId = string.Empty;
            eqSave.Artifacts.ArtifactIds.Clear();

            if (equipmentController != null)
            {
                PlayerEquipmentState state = equipmentController.State;
                eqSave.WeaponSlot1Id = state.WeaponSlot1 != null ? state.WeaponSlot1.ItemId : string.Empty;
                eqSave.WeaponSlot2Id = state.WeaponSlot2 != null ? state.WeaponSlot2.ItemId : string.Empty;
                eqSave.ActiveWeaponSlot = (int)state.ActiveWeaponSlot;
                eqSave.ArmorId = state.EquippedArmor != null ? state.EquippedArmor.ItemId : string.Empty;
            }

            if (artifactController != null)
            {
                eqSave.Artifacts.MaxSlots = artifactController.SlotCount;
                for (int i = 0; i < artifactController.SlotCount; i++)
                {
                    ArtifactDefinitionSO art = artifactController.GetArtifactInSlot(i);
                    eqSave.Artifacts.ArtifactIds.Add(art != null ? art.ItemId : string.Empty);
                }
            }
        }

        private void RestoreInventory(InventorySaveData invSave)
        {
            if (inventory == null) return;

            var resolvedEntries = new List<(int slotIndex, ItemDefinitionSO item, int quantity)>(invSave.Items.Count);

            for (int i = 0; i < invSave.Items.Count; i++)
            {
                InventorySlotSaveEntry entry = invSave.Items[i];
                if (string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0)
                {
                    continue;
                }

                // Langkah 4 & 5: Validasi ID di ItemCatalogSO; laporkan & lewati jika tidak ditemukan (P05)
                if (!itemCatalog.TryGetItem(entry.ItemId, out ItemDefinitionSO definition))
                {
                    Debug.LogWarning($"[ItemPersistence] ItemId tidak ditemukan di catalog dan dilewati: '{entry.ItemId}'.", this);
                    continue;
                }

                int clampedQuantity = Mathf.Clamp(entry.Quantity, 1, definition.MaxStackSize);
                resolvedEntries.Add((entry.SlotIndex, definition, clampedQuantity));
            }

            inventory.Controller.RestoreFromSaveEntries(
                resolvedEntries,
                inventory.Controller.Model.AllowDuplicateEquipment);
        }

        private void RestoreEquipmentAndArtifacts(EquipmentSaveData eqSave)
        {
            if (equipmentController != null)
            {
                WeaponDefinitionSO slot1 = ResolveOwnedEquipment<WeaponDefinitionSO>(eqSave.WeaponSlot1Id, "WeaponSlot1");
                WeaponDefinitionSO slot2 = ResolveOwnedEquipment<WeaponDefinitionSO>(eqSave.WeaponSlot2Id, "WeaponSlot2");
                ArmorDefinitionSO armor = ResolveOwnedEquipment<ArmorDefinitionSO>(eqSave.ArmorId, "ArmorSlot");

                WeaponSlotType activeSlot = eqSave.ActiveWeaponSlot switch
                {
                    1 => WeaponSlotType.Slot1,
                    2 => WeaponSlotType.Slot2,
                    _ => WeaponSlotType.None
                };

                equipmentController.RestoreEquipmentState(slot1, slot2, activeSlot, armor);
            }

            if (artifactController != null && eqSave.Artifacts != null)
            {
                List<string> savedIds = eqSave.Artifacts.ArtifactIds;
                ArtifactDefinitionSO[] resolvedArtifacts = new ArtifactDefinitionSO[savedIds.Count];

                for (int i = 0; i < savedIds.Count; i++)
                {
                    resolvedArtifacts[i] = ResolveOwnedEquipment<ArtifactDefinitionSO>(savedIds[i], $"ArtifactSlot[{i}]");
                }

                artifactController.RestoreArtifactState(resolvedArtifacts);
            }
        }

        private T ResolveOwnedEquipment<T>(string itemId, string slotLabel) where T : EquipmentDefinitionSO
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            if (!itemCatalog.TryGetItem<T>(itemId, out T definition))
            {
                Debug.LogWarning($"[ItemPersistence] Referensi {slotLabel} ('{itemId}') tidak valid atau tidak ada di catalog.", this);
                return null;
            }

            // Langkah 8: Validasi item yang terpasang benar-benar dimiliki di Inventory
            if (inventory != null && !inventory.HasItem(definition))
            {
                Debug.LogWarning($"[ItemPersistence] Item '{itemId}' pada {slotLabel} tidak dimiliki di Inventory; dilepas otomatis.", this);
                return null;
            }

            return definition;
        }
    }
}

```

---

### 5. SaveSystem Integration (`SaveSystem.cs`)

Mengelola pembacaan dan penulisan `tls_save.json` secara atomik (menulis ke file sementara `.tmp` terlebih dahulu agar file save tidak rusak jika aplikasi tertutup tiba-tiba) serta mengoordinasikan *currency*, *progression*, dan `ItemPersistenceCoordinator`.

```csharp
using System;
using System.IO;
using UnityEngine;

namespace TheLastSasak.Systems.Save
{
    public interface ISaveableSystem
    {
        void WriteToSave(TLS_SaveData data);
        void ReadFromSave(TLS_SaveData data);
    }

    public class SaveSystem : MonoBehaviour
    {
        [Header("File Settings")]
        [SerializeField] private string saveFileName = "tls_save.json";
        [SerializeField] private bool prettyPrintJson = true;
        [SerializeField] private bool autoLoadOnStart = false;

        [Header("Dependencies")]
        [SerializeField] private ItemPersistenceCoordinator itemPersistence;

        private readonly System.Collections.Generic.List<ISaveableSystem> _externalSaveables =
            new System.Collections.Generic.List<ISaveableSystem>();

        private TLS_SaveData _currentSaveData = new TLS_SaveData();

        public event Action<TLS_SaveData> OnGameSaved;
        public event Action<TLS_SaveData> OnGameLoaded;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public TLS_SaveData CurrentSaveData => _currentSaveData;

        private void Awake()
        {
            if (itemPersistence == null)
            {
                itemPersistence = FindFirstObjectByType<ItemPersistenceCoordinator>();
            }
        }

        private void Start()
        {
            if (autoLoadOnStart && HasSaveFile())
            {
                LoadGame();
            }
        }

        public void RegisterSaveable(ISaveableSystem saveable)
        {
            if (saveable != null && !_externalSaveables.Contains(saveable))
            {
                _externalSaveables.Add(saveable);
            }
        }

        public void UnregisterSaveable(ISaveableSystem saveable)
        {
            if (saveable != null)
            {
                _externalSaveables.Remove(saveable);
            }
        }

        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        [ContextMenu("Save Game")]
        public bool SaveGame()
        {
            try
            {
                _currentSaveData ??= new TLS_SaveData();
                _currentSaveData.SaveVersion = TLS_SaveData.LatestSaveVersion;
                _currentSaveData.LastSavedUtc = DateTime.UtcNow.ToString("o");
                _currentSaveData.EnsureValidStructure();

                for (int i = 0; i < _externalSaveables.Count; i++)
                {
                    _externalSaveables[i]?.WriteToSave(_currentSaveData);
                }

                if (itemPersistence != null)
                {
                    itemPersistence.CaptureToSaveData(_currentSaveData);
                }

                string json = JsonUtility.ToJson(_currentSaveData, prettyPrintJson);
                WriteAtomicText(SaveFilePath, json);

                OnGameSaved?.Invoke(_currentSaveData);
                Debug.Log($"[SaveSystem] Berhasil menyimpan ke: {SaveFilePath}", this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal menyimpan game: {ex.Message}", this);
                return false;
            }
        }

        [ContextMenu("Load Game")]
        public bool LoadGame()
        {
            if (!HasSaveFile())
            {
                Debug.LogWarning($"[SaveSystem] File save tidak ditemukan di: {SaveFilePath}", this);
                return false;
            }

            try
            {
                // Langkah 1: Baca JSON
                string json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogError("[SaveSystem] File tls_save.json kosong.", this);
                    return false;
                }

                TLS_SaveData loadedData = JsonUtility.FromJson<TLS_SaveData>(json);
                if (loadedData == null)
                {
                    Debug.LogError("[SaveSystem] Gagal parse JSON tls_save.json.", this);
                    return false;
                }

                // Langkah 2 & 3: Periksa SaveVersion & validasi struktur data
                if (!SaveVersionMigrator.TryMigrateToLatest(loadedData, out _))
                {
                    return false;
                }

                _currentSaveData = loadedData;

                // Pulihkan sistem eksternal (CurrencyWallet, PlayerProgression, Settings)
                for (int i = 0; i < _externalSaveables.Count; i++)
                {
                    _externalSaveables[i]?.ReadFromSave(_currentSaveData);
                }

                // Langkah 4–10: Resolusi ItemCatalogSO, Inventory, Equipment, Artifacts & RecalculateStats
                if (itemPersistence != null)
                {
                    itemPersistence.ApplyFromSaveData(_currentSaveData);
                }

                OnGameLoaded?.Invoke(_currentSaveData);
                Debug.Log("[SaveSystem] Berhasil memuat tls_save.json.", this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal memuat game: {ex.Message}", this);
                return false;
            }
        }

        [ContextMenu("Delete Save File")]
        public void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log($"[SaveSystem] File save dihapus: {SaveFilePath}", this);
            }
        }

        private static void WriteAtomicText(string targetPath, string content)
        {
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
        }
    }
}

```

---

### 6. Panduan Wiring & Validasi Phase 7 (P01–P06)

1. **Setup di Scene (`CombatTest.unity` / `Hub.unity`):**
* Pasang `ItemPersistenceCoordinator` pada root `Player` dan hubungkan `ItemCatalog_Main` ke field `Item Catalog`.
* Pasang `SaveSystem` pada manager scene (atau `GameBootstrap`) dan referensikan `ItemPersistenceCoordinator`.
* Implementasikan `ISaveableSystem` pada `CurrencyWallet` (membaca/menulis `data.CurrencyData.Souls`) dan `PlayerProgression` (membaca/menulis `data.PlayerProgressionData`).


2. **Checklist Pengujian P01–P06:**
* **P01 & P02:** Isi beberapa slot `Inventory` (misalnya `weapon.klewang`, `weapon.pedang_cepat`, `armor.besi_ambat`, `artifact.batu_ambang`), jalankan **Context Menu - Save Game**, bersihkan inventory, lalu jalankan **Load Game** — seluruh item dan indeks slot pulih.
* **P03 & P04:** Pasang senjata pada Slot 1 dan Slot 2, aktifkan Slot 2, pasang armor dan artefak, simpan lalu muat ulang — `EquipmentVisualController`, `WeaponCombatAdapter`, dan `PlayerStatController.FinalStats` pulih identik.
* **P05:** Edit manual salah satu `ItemId` di `tls_save.json` menjadi `"weapon.tidak_ada"`, lalu muat ulang — log peringatan muncul tanpa *exception* dan item lain tetap pulih.
* **P06:** Ubah `"SaveVersion": 0` dan isi `"ItemId": " WEAPON.KLEWANG "` di `tls_save.json`, lalu muat ulang — `SaveVersionMigrator` menormalisasi ID ke huruf kecil dan memperbarui versi ke `1`.