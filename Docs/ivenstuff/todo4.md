Implementasi **Phase 4 — Equipment Integration & Visual Management** menghubungkan `Inventory` (Phase 3) dengan `EquipmentController` (Phase 2), menyinkronkan `AttackController` dan `Animator` melalui `WeaponCombatAdapter`, serta memastikan visual senjata dan armor *scene-authored* selalu selaras sejak frame pertama.

### Struktur Target Phase 4

```text
Assets/_Project/Scripts/Items/Equipment/
├── EquipmentTransactionHandler.cs   (Baru: Pipeline validasi equip/unequip & sinkronisasi Inventory)
├── IWeaponCombatReceiver.cs         (Baru: Kontrak adapter ke AttackController)
├── WeaponCombatAdapter.cs           (Diperbarui: Sinkronisasi ComboChain, AnimatorOverride & initial state)
├── EquipmentVisualController.cs     (Diperbarui: Sinkronisasi WeaponSocket, ArmorRenderer & initial state)
├── PlayerActionGateAdapter.cs       (Baru: Jembatan validasi PlayerActionController ke IWeaponSwitchGate)
└── EquipmentController.cs           (Penambahan: ForceUnequip untuk validasi kepemilikan)

```

---

### 1. Pipeline Transaksi Inventory ke Equipment (`EquipmentTransactionHandler.cs`)

Menjalankan alur validasi **Point 4.1** secara berurutan (`Select Item` → `Check Ownership` → `Check Category` → `Validate Slot` → `Check Action State` → `Apply Equipment` → `Notify Systems`) dan otomatis melepas equipment apabila item tersebut dihapus dari `Inventory`.

```csharp
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

namespace TheLastSasak.Items.Equipment
{
    public class EquipmentTransactionHandler : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Inventory.Inventory inventory;
        [SerializeField] private EquipmentController equipmentController;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory.Inventory>();
            }

            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }

            if (equipmentController != null && inventory != null)
            {
                equipmentController.SetOwnershipQuery(inventory);
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnItemRemoved += HandleInventoryItemRemoved;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnItemRemoved -= HandleInventoryItemRemoved;
            }
        }

        /// <summary>
        /// Titik masuk utama dari UI atau shortcut untuk memasang item dari Inventory.
        /// Jika preferredWeaponSlot adalah None, sistem memilih slot kosong atau slot aktif.
        /// </summary>
        public bool TryEquipFromInventory(ItemDefinitionSO item, WeaponSlotType preferredWeaponSlot = WeaponSlotType.None)
        {
            // 1. Validasi referensi item
            if (item == null || equipmentController == null)
            {
                return false;
            }

            // 2. Check Ownership
            if (inventory != null && !inventory.HasItem(item))
            {
                Debug.LogWarning($"[EquipmentTransaction] Gagal equip: '{item.ItemId}' tidak ada di Inventory.", this);
                return false;
            }

            // 3. Check Item Category & 4. Validate Equipment Slot
            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    if (item is not WeaponDefinitionSO weapon)
                    {
                        return false;
                    }

                    WeaponSlotType targetSlot = ResolveTargetWeaponSlot(weapon, preferredWeaponSlot);
                    return equipmentController.TryEquipWeapon(targetSlot, weapon);

                case ItemCategory.Armor:
                    if (item is not ArmorDefinitionSO armor)
                    {
                        return false;
                    }

                    return equipmentController.TryEquipArmor(armor);

                case ItemCategory.Artifact:
                    // Akan diarahkan ke ArtifactController pada Phase 5
                    Debug.Log($"[EquipmentTransaction] '{item.ItemId}' adalah Artifact; gunakan ArtifactController.", this);
                    return false;

                default:
                    Debug.LogWarning($"[EquipmentTransaction] Kategori '{item.Category}' tidak dapat dipasang sebagai equipment.", this);
                    return false;
            }
        }

        public bool TryUnequipWeaponSlot(WeaponSlotType slot)
        {
            if (equipmentController == null) return false;
            return equipmentController.TryUnequipWeapon(slot);
        }

        public bool TryUnequipArmor()
        {
            if (equipmentController == null) return false;
            return equipmentController.TryUnequipArmor();
        }

        public bool IsItemEquipped(ItemDefinitionSO item)
        {
            if (item == null || equipmentController == null) return false;

            if (item is WeaponDefinitionSO weapon)
            {
                return equipmentController.State.IsWeaponEquipped(weapon);
            }

            if (item is ArmorDefinitionSO armor)
            {
                return equipmentController.EquippedArmor == armor;
            }

            return false;
        }

        private WeaponSlotType ResolveTargetWeaponSlot(WeaponDefinitionSO weapon, WeaponSlotType preferredSlot)
        {
            PlayerEquipmentState state = equipmentController.State;

            // Jika sudah terpasang di salah satu slot, pertahankan slot tersebut
            WeaponSlotType existingSlot = state.FindSlotOfWeapon(weapon);
            if (existingSlot != WeaponSlotType.None)
            {
                return existingSlot;
            }

            if (preferredSlot == WeaponSlotType.Slot1 || preferredSlot == WeaponSlotType.Slot2)
            {
                return preferredSlot;
            }

            // Otomatis pilih slot kosong terlebih dahulu
            if (state.WeaponSlot1 == null) return WeaponSlotType.Slot1;
            if (state.WeaponSlot2 == null) return WeaponSlotType.Slot2;

            // Jika kedua slot terisi, timpa slot yang sedang aktif
            return state.ActiveWeaponSlot != WeaponSlotType.None
                ? state.ActiveWeaponSlot
                : WeaponSlotType.Slot1;
        }

        /// <summary>
        /// Menjamin equipment otomatis dilepas apabila item dibuang/hilang dari Inventory.
        /// </summary>
        private void HandleInventoryItemRemoved(ItemDefinitionSO removedItem, int _)
        {
            if (removedItem == null || equipmentController == null || inventory == null)
            {
                return;
            }

            if (inventory.HasItem(removedItem))
            {
                return;
            }

            if (removedItem is WeaponDefinitionSO weapon)
            {
                WeaponSlotType equippedSlot = equipmentController.State.FindSlotOfWeapon(weapon);
                if (equippedSlot != WeaponSlotType.None)
                {
                    equipmentController.ForceUnequipWeapon(equippedSlot);
                }
            }
            else if (removedItem is ArmorDefinitionSO armor && equipmentController.EquippedArmor == armor)
            {
                equipmentController.ForceUnequipArmor();
            }
        }
    }
}

```

#### Penambahan pada `EquipmentController.cs` (Phase 2)

Tambahkan dua metode berikut ke `EquipmentController` agar penghapusan paksa saat item hilang dari Inventory tidak terblokir oleh *combat action state*:

```csharp
// Tambahkan di dalam class EquipmentController (Assets/_Project/Scripts/Items/Equipment/EquipmentController.cs)
public void ForceUnequipWeapon(WeaponSlotType slot)
{
    if (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2) return;

    WeaponDefinitionSO currentWeapon = state.GetWeapon(slot);
    if (currentWeapon == null) return;

    state.SetWeapon(slot, null);
    OnWeaponUnequipped?.Invoke(slot, currentWeapon);

    if (state.ActiveWeaponSlot == slot)
    {
        WeaponSlotType fallbackSlot = slot == WeaponSlotType.Slot1
            ? WeaponSlotType.Slot2
            : WeaponSlotType.Slot1;

        WeaponSlotType nextActive = state.GetWeapon(fallbackSlot) != null
            ? fallbackSlot
            : WeaponSlotType.None;

        state.SetActiveWeaponSlot(nextActive);
        OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
    }

    OnEquipmentChanged?.Invoke();
}

public void ForceUnequipArmor()
{
    ArmorDefinitionSO previousArmor = state.EquippedArmor;
    if (previousArmor == null) return;

    state.SetArmor(null);
    OnArmorUnequipped?.Invoke(previousArmor);
    OnEquipmentChanged?.Invoke();
}

```

---

### 2. Combat & Animator Adapter (`IWeaponCombatReceiver.cs` & `WeaponCombatAdapter.cs`)

Menghubungkan `EquipmentController` dengan `AttackController` dan `Animator` secara deterministik (Point 4.2). Setiap kali senjata aktif dipasang atau diganti, `ComboChain` dan `AnimatorOverrideController` diperbarui secara bersamaan.

#### `IWeaponCombatReceiver.cs`

```csharp
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    /// <summary>
    /// Implementasikan interface ini pada AttackController milik proyek Anda.
    /// </summary>
    public interface IWeaponCombatReceiver
    {
        void SetWeaponLoadout(WeaponDefinitionSO weapon);
        void ClearWeaponLoadout();
    }
}

```

#### `WeaponCombatAdapter.cs`

```csharp
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class WeaponCombatAdapter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private Animator playerAnimator;
        [Tooltip("Opsional: Komponen AttackController yang mengimplementasikan IWeaponCombatReceiver.")]
        [SerializeField] private MonoBehaviour combatReceiverComponent;

        private RuntimeAnimatorController _baseAnimatorController;
        private IWeaponCombatReceiver _combatReceiver;

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }

            if (playerAnimator != null)
            {
                _baseAnimatorController = playerAnimator.runtimeAnimatorController;
            }

            _combatReceiver = combatReceiverComponent != null
                ? combatReceiverComponent as IWeaponCombatReceiver
                : GetComponent<IWeaponCombatReceiver>();
        }

        private void OnEnable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched += HandleWeaponSwitched;
            }
        }

        private void OnDisable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched -= HandleWeaponSwitched;
            }
        }

        private void Start()
        {
            // Jaminan sinkronisasi awal terlepas dari urutan Start() antar komponen
            if (equipmentController != null)
            {
                SyncCombatWithWeapon(equipmentController.ActiveWeapon);
            }
        }

        private void HandleWeaponSwitched(WeaponSlotType activeSlot, WeaponDefinitionSO activeWeapon)
        {
            SyncCombatWithWeapon(activeWeapon);
        }

        public void SyncCombatWithWeapon(WeaponDefinitionSO weapon)
        {
            ApplyAnimatorOverride(weapon);
            ApplyCombatLoadout(weapon);
        }

        private void ApplyAnimatorOverride(WeaponDefinitionSO weapon)
        {
            if (playerAnimator == null)
            {
                return;
            }

            if (weapon != null && weapon.AnimatorOverride != null)
            {
                playerAnimator.runtimeAnimatorController = weapon.AnimatorOverride;
            }
            else if (_baseAnimatorController != null)
            {
                playerAnimator.runtimeAnimatorController = _baseAnimatorController;
            }
        }

        private void ApplyCombatLoadout(WeaponDefinitionSO weapon)
        {
            if (_combatReceiver == null)
            {
                return;
            }

            if (weapon != null)
            {
                if (weapon.ComboChain == null || weapon.ComboChain.Count == 0)
                {
                    Debug.LogError($"[WeaponCombatAdapter] Senjata '{weapon.ItemId}' tidak memiliki ComboChain yang valid!", weapon);
                    _combatReceiver.ClearWeaponLoadout();
                    return;
                }

                _combatReceiver.SetWeaponLoadout(weapon);
            }
            else
            {
                _combatReceiver.ClearWeaponLoadout();
            }
        }
    }
}

```

#### Contoh Penerapan `IWeaponCombatReceiver` di `AttackController.cs`

Tambahkan implementasi interface ini pada `AttackController` yang sudah ada agar menerima `ComboChain` langsung dari `WeaponDefinitionSO` tanpa menyimpan salinan loadout sendiri:

```csharp
// Contoh integrasi di dalam AttackController.cs proyek Anda:
// public class AttackController : MonoBehaviour, IWeaponCombatReceiver
// {
//     private WeaponDefinitionSO _currentWeapon;
//     private IReadOnlyList<AttackDefinitionSO> _activeComboChain;
//     private int _comboIndex;
//
//     public void SetWeaponLoadout(WeaponDefinitionSO weapon)
//     {
//         _currentWeapon = weapon;
//         _activeComboChain = weapon.ComboChain;
//         _comboIndex = 0;
//     }
//
//     public void ClearWeaponLoadout()
//     {
//         _currentWeapon = null;
//         _activeComboChain = null;
//         _comboIndex = 0;
//     }
// }

```

---

### 3. Scene-Authored Visual Controller (`EquipmentVisualController.cs`)

Pembaruan penuh untuk **Point 4.3** dan **Point 4.4**:

* Menyinkronkan visual pada `Start()` agar tidak terjadi *desync* jika `EquipmentController.Start()` berjalan lebih dulu.
* Mendukung pergantian `Material` pada satu atau banyak `Renderer` armor sekaligus, serta pergantian child mesh (`ArmorVisualId`) di dalam `ArmorSocket`.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class EquipmentVisualController : MonoBehaviour
    {
        [Serializable]
        public struct VisualEntry
        {
            public string visualId;
            public GameObject visualObject;
        }

        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;

        [Header("Weapon Sockets (Scene-Authored)")]
        [SerializeField] private GameObject weaponSlot01Root;
        [SerializeField] private GameObject weaponSlot02Root;

        [Tooltip("Daftarkan semua child visual senjata di bawah WeaponSlot_01 beserta WeaponVisualId-nya.")]
        [SerializeField] private List<VisualEntry> slot01Visuals = new List<VisualEntry>();

        [Tooltip("Daftarkan semua child visual senjata di bawah WeaponSlot_02 beserta WeaponVisualId-nya.")]
        [SerializeField] private List<VisualEntry> slot02Visuals = new List<VisualEntry>();

        [Header("Armor Socket (Scene-Authored)")]
        [Tooltip("Renderer tubuh/armor yang material-nya diganti saat armor dipasang.")]
        [SerializeField] private Renderer[] armorRenderers;
        [SerializeField] private Material defaultArmorMaterial;
        [Tooltip("Opsional: Child mesh khusus di dalam ArmorSocket berdasarkan ArmorVisualId.")]
        [SerializeField] private List<VisualEntry> armorVisuals = new List<VisualEntry>();

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }
        }

        private void OnEnable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched += HandleWeaponSwitched;
                equipmentController.OnWeaponEquipped += HandleWeaponEquipped;
                equipmentController.OnWeaponUnequipped += HandleWeaponUnequipped;
                equipmentController.OnArmorEquipped += HandleArmorChanged;
                equipmentController.OnArmorUnequipped += HandleArmorChanged;
            }
        }

        private void OnDisable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched -= HandleWeaponSwitched;
                equipmentController.OnWeaponEquipped -= HandleWeaponEquipped;
                equipmentController.OnWeaponUnequipped -= HandleWeaponUnequipped;
                equipmentController.OnArmorEquipped -= HandleArmorChanged;
                equipmentController.OnArmorUnequipped -= HandleArmorChanged;
            }
        }

        private void Start()
        {
            RefreshAllVisuals();
        }

        public void RefreshAllVisuals()
        {
            RefreshWeaponVisuals();
            RefreshArmorVisuals();
        }

        private void HandleWeaponEquipped(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleWeaponUnequipped(WeaponSlotType slot, WeaponDefinitionSO previousWeapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleWeaponSwitched(WeaponSlotType activeSlot, WeaponDefinitionSO activeWeapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleArmorChanged(ArmorDefinitionSO _)
        {
            RefreshArmorVisuals();
        }

        public void RefreshWeaponVisuals()
        {
            if (equipmentController == null)
            {
                return;
            }

            PlayerEquipmentState state = equipmentController.State;
            WeaponSlotType activeSlot = state.ActiveWeaponSlot;

            bool isSlot1Active = activeSlot == WeaponSlotType.Slot1 && state.WeaponSlot1 != null;
            bool isSlot2Active = activeSlot == WeaponSlotType.Slot2 && state.WeaponSlot2 != null;

            if (weaponSlot01Root != null)
            {
                weaponSlot01Root.SetActive(isSlot1Active);
                if (isSlot1Active)
                {
                    ActivateMatchingVisual(slot01Visuals, state.WeaponSlot1.WeaponVisualId);
                }
            }

            if (weaponSlot02Root != null)
            {
                weaponSlot02Root.SetActive(isSlot2Active);
                if (isSlot2Active)
                {
                    ActivateMatchingVisual(slot02Visuals, state.WeaponSlot2.WeaponVisualId);
                }
            }
        }

        public void RefreshArmorVisuals()
        {
            if (equipmentController == null)
            {
                return;
            }

            ArmorDefinitionSO armor = equipmentController.EquippedArmor;
            Material targetMaterial = (armor != null && armor.ArmorMaterial != null)
                ? armor.ArmorMaterial
                : defaultArmorMaterial;

            if (armorRenderers != null && targetMaterial != null)
            {
                for (int i = 0; i < armorRenderers.Length; i++)
                {
                    if (armorRenderers[i] != null)
                    {
                        armorRenderers[i].sharedMaterial = targetMaterial;
                    }
                }
            }

            string targetVisualId = armor != null ? armor.ArmorVisualId : string.Empty;
            ActivateMatchingVisual(armorVisuals, targetVisualId);
        }

        private static void ActivateMatchingVisual(List<VisualEntry> entries, string targetVisualId)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject obj = entries[i].visualObject;
                if (obj == null)
                {
                    continue;
                }

                bool shouldBeActive = !string.IsNullOrWhiteSpace(targetVisualId) &&
                                      string.Equals(entries[i].visualId, targetVisualId, StringComparison.OrdinalIgnoreCase);

                if (obj.activeSelf != shouldBeActive)
                {
                    obj.SetActive(shouldBeActive);
                }
            }
        }
    }
}

```

---

### 4. Action State Gate Adapter (`PlayerActionGateAdapter.cs`)

Menghubungkan `EquipmentController` dengan `PlayerActionController` (Point 2.4 & Point 4.1) sehingga pergantian senjata maupun armor ditolak ketika pemain sedang menyerang, *dodge*, terkena *stagger*, atau mati.

```csharp
using UnityEngine;

namespace TheLastSasak.Items.Equipment
{
    public class PlayerActionGateAdapter : MonoBehaviour, IWeaponSwitchGate
    {
        // Hubungkan ke PlayerActionController Anda:
        // [SerializeField] private PlayerActionController actionController;

        [Header("Debug Override (Testing)")]
        [SerializeField] private bool lockEquipmentActionsInEditor = false;

        public bool CanSwitchWeapon()
        {
            if (lockEquipmentActionsInEditor)
            {
                return false;
            }

            // Ganti dengan pemeriksaan state PlayerActionController Anda, misalnya:
            // return actionController == null || actionController.CanSwitchWeapon();
            return true;
        }

        public bool CanChangeEquipment()
        {
            if (lockEquipmentActionsInEditor)
            {
                return false;
            }

            // Misalnya: hanya izinkan equip/unequip saat pemain tidak dalam state combat terkunci
            // return actionController == null || (!actionController.IsAttacking && !actionController.IsStaggered && !actionController.IsDead);
            return CanSwitchWeapon();
        }
    }
}

```

---

### 5. Panduan Setup Prefab `Player` untuk Validasi Phase 4 (E01–E10)

1. **Susunan Hierarchy `Player`:**
```text
Player
├── WeaponSocket
│   ├── WeaponSlot_01
│   │   ├── KlewangVisual       (VisualId: "KlewangVisual")
│   │   └── PedangCepatVisual   (VisualId: "PedangCepatVisual")
│   └── WeaponSlot_02
│       ├── KlewangVisual       (VisualId: "KlewangVisual")
│       └── PedangCepatVisual   (VisualId: "PedangCepatVisual")
└── ArmorSocket
    └── ArmorVisual             (Renderer dipasang ke Armor Renderers)

```


*Dengan menaruh kedua model visual di `WeaponSlot_01` dan `WeaponSlot_02`, pemain bebas memasang `Weapon_Klewang` maupun `Weapon_PedangCepat` ke slot mana saja dari Inventory tanpa perlu `Instantiate`.*
2. **Komponen pada Root `Player`:**
* `Inventory` (isi `Starting Items` dengan `Weapon_Klewang`, `Weapon_PedangCepat`, dan `Armor_BesiAmbat` untuk uji coba).
* `EquipmentController` (matikan `Allow Equip Without Inventory In Editor` agar kepemilikan wajib divalidasi lewat `Inventory`).
* `EquipmentTransactionHandler` (menghubungkan `Inventory` dan `EquipmentController`).
* `EquipmentVisualController` (isi `Slot 01 Visuals`, `Slot 02 Visuals`, dan `Armor Renderers`).
* `WeaponCombatAdapter` (referensikan `Player Animator` dan `AttackController`).
* `PlayerActionGateAdapter` (aktif otomatis lewat `GetComponent<IWeaponSwitchGate>()` di `EquipmentController.Awake()`).