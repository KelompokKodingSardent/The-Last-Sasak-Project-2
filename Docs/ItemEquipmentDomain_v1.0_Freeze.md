# The Last Sasak — Item & Equipment Domain v1.0 (Domain Freeze Baseline)

**Status:** FROZEN (v1.0 Baseline)  
**Target Milestone:** Phase 7 Gameplay Roadmap (Demo Arsenal: 2 Weapons, 1 Armor, 1–2 Artifacts)  
**Scope:** Item Definitions, Catalog, 20-Slot Inventory, Equipment & Switching, Stat Modifiers, Loot & Reward Routing, JSON Persistence (`tls_save.json`), dan Inventory/Equipment UI.

---

## 1. Arsitektur & Invarian Sistem (Locked Rules)

1. **Single Source of Truth:**
   - `InventoryController` memegang kepemilikan item (`InventorySlot[20]`).
   - `EquipmentController` memegang state perlengkapan aktif (`WeaponSlot1`, `WeaponSlot2`, `ActiveWeaponSlot`, `EquippedArmor`). `ItemSwitching` murni berfungsi sebagai *input adapter* tanpa menyimpan salinan `AttackDefinitionSO[]` maupun indeks terpisah.
   - `ArtifactController` memegang slot artefak (`maxArtifactSlots = 2`).
   - `PlayerStatController` merupakan satu-satunya kalkulator `FinalStats`.
2. **Pemisahan Resource:**
   - *Souls* (`CurrencyWallet`) dan *EXP* (`ExperienceSystem`) tidak pernah disimpan sebagai slot `Inventory`.
3. **Immutable Definitions:**
   - Seluruh aset turunan `ItemDefinitionSO` bersifat *read-only* saat runtime; modifier disalin ke buffer runtime dengan `SourceId` otomatis.
4. **Scene-Authored Presentation:**
   - Pergantian visual senjata dan armor mengaktifkan/menonaktifkan child socket di Hierarchy (`WeaponSocket`, `ArmorSocket`) atau mengganti `sharedMaterial`, tanpa `Instantiate`/`Destroy` saat bertarung.
5. **Deterministic Stat Formula & MaxHealth Policy:**
   - Kalkulasi mengikuti urutan:
     $$\text{FinalStat} = (\text{Base} + \sum \text{Additive}) \times \prod (1 + \text{Multiplicative})$$
   - Perubahan `MaxHealth` menggunakan `MaxHealthRecalculationPolicy.KeepCurrentAndClamp` secara default (meningkatkan `MaxHealth` tidak memicu *auto-heal*; menurunkan `MaxHealth` melakukan *clamp* pada `CurrentHealth`).
6. **ID-Based Persistence:**
   - `tls_save.json` hanya menyimpan `ItemId` (string huruf kecil) dan `Quantity`. `FinalStats` selalu dihitung ulang saat *load* setelah validasi terhadap `ItemCatalogSO`.

---

## 2. Ringkasan API Publik Domain

| Komponen | Namespace | Metode / Event Kunci |
|---|---|---|
| `ItemCatalogSO` | `TheLastSasak.Items.Catalog` | `TryGetItem(id, out item)`, `TryGetItem<T>(id, out item)`, `ValidateCatalog()` |
| `Inventory` / `InventoryController` | `TheLastSasak.Items.Inventory` | `CanAddItem()`, `TryAddItem()`, `TryRemoveItem()`, `HasItem()`, `GetSlots()`, `OnInventoryChanged` |
| `EquipmentController` | `TheLastSasak.Items.Equipment` | `TryEquipWeapon()`, `TryUnequipWeapon()`, `TrySwitchWeaponSlot()`, `TryToggleWeaponSlot()`, `TryEquipArmor()`, `TryUnequipArmor()`, `OnWeaponSwitched`, `OnEquipmentChanged` |
| `EquipmentTransactionHandler` | `TheLastSasak.Items.Equipment` | `TryEquipFromInventory(item, preferredSlot)`, `IsItemEquipped(item)` |
| `ArtifactController` | `TheLastSasak.Items.Artifact` | `TryEquipArtifact()`, `TryEquipArtifactAuto()`, `TryUnequipArtifact()`, `GetArtifactModifiers()`, `OnArtifactsChanged` |
| `PlayerStatController` | `TheLastSasak.Player.Stats` | `GetStat(StatType)`, `RecalculateStats()`, `PreviewStatsWithCandidate()`, `OnStatsRecalculated`, `OnMaxHealthChanged` |
| `LootCollector` & `LootDropController` | `TheLastSasak.Items.Loot` | `TryCollectLoot()`, `SpawnLoot()`, `RollAndSpawnDrops()`, `ReturnPickupToPool()` |
| `SaveSystem` & `ItemPersistenceCoordinator` | `TheLastSasak.Systems.Save` | `SaveGame()`, `LoadGame()`, `CaptureToSaveData()`, `ApplyFromSaveData()` |

---

## 3. Baseline Content & Prefab Wiring

- **Baseline Assets (`Assets/_Project/ScriptableObjects/`):**
  - `weapon.klewang` (`Weapon_Klewang`)
  - `weapon.pedang_cepat` (`Weapon_PedangCepat`)
  - `armor.besi_ambat` (`Armor_BesiAmbat`)
  - `artifact.batu_ambang` (`Artifact_BatuAmbang`)
  - `artifact.pecahan_arwah` (`Artifact_PecahanArwah`)
  - `ItemCatalog_Main` (mendaftarkan seluruh ID di atas dengan 0 duplikat)
- **Player Prefab Contract:**
  - Root `Player` wajib memiliki: `Inventory`, `EquipmentController`, `EquipmentTransactionHandler`, `EquipmentVisualController`, `WeaponCombatAdapter`, `ArtifactController`, `PlayerStatController`, `LootCollector`, `ItemPersistenceCoordinator`, serta implementasi `IWeaponSwitchGate` dan `IHealthStatReceiver`.

---

## 4. Verifikasi Test Matrix (Phase 9)

- **Inventory (`I01`–`I09`):** Terverifikasi pada `InventoryDomainTests.cs`.
- **Equipment (`E01`–`E10`):** Terverifikasi pada `EquipmentDomainTests.cs`.
- **Stats & Artifact (`S01`–`S09`):** Terverifikasi pada `StatAndArtifactDomainTests.cs`.
- **Loot & Persistence (`L01`–`L06`, `P01`–`P06`):** Terverifikasi pada `LootAndPersistenceDomainTests.cs`.
- **Release Gate:** Seluruh test lulus di Unity Test Runner (`EditMode`), bebas error di Console pada scene `CombatTest.unity`, dan lolos pengujian build Windows standalone.

---

## 5. Out-of-Scope & Backlog Pasca-v1.0

Fitur berikut sengaja dikecualikan dari v1.0 dan baru dibuka setelah *Gameplay Vertical Slice* stabil:
- Instance ID per senjata untuk *Weapon/Armor Upgrade* dan *Randomized Stats*.
- *Crafting*, *Shop & Trading* di `Hub.unity`, *Consumable System* aktif, dan *Item Set Bonus*.
- Kebijakan *Run-Only Inventory* vs *Permanent Collection* saat pemain gugur (saat ini dikendalikan flag `ItemPersistenceCoordinator.PersistInventoryAndEquipment`).