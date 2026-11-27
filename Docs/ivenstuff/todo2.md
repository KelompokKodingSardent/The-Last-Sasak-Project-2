Implementasi **Phase 2 — Equipment Core Refactoring** memusatkan seluruh state perlengkapan di `EquipmentController` sebagai *single source of truth*, memisahkan data runtime di `PlayerEquipmentState`, mengubah `ItemSwitching` menjadi *input adapter* murni, dan mengganti `Instantiate`/`Destroy` dengan aktivasi objek *scene-authored* di `EquipmentVisualController`.

### Struktur Target Phase 2

```text
Assets/_Project/Scripts/Items/Equipment/
├── PlayerEquipmentState.cs
├── EquipmentContracts.cs
├── EquipmentController.cs
├── ItemSwitching.cs
├── EquipmentVisualController.cs
└── WeaponCombatAdapter.cs

```

---

### 1. Runtime State & Kontrak Integrasi (`Items/Equipment/`)

#### `PlayerEquipmentState.cs`

Menyimpan state perlengkapan aktif saat runtime tanpa mencampurkan struktur *save/load* maupun logika UI.

```csharp
using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public enum WeaponSlotType
    {
        None = 0,
        Slot1 = 1,
        Slot2 = 2
    }

    [Serializable]
    public class PlayerEquipmentState
    {
        [SerializeField] private WeaponDefinitionSO weaponSlot1;
        [SerializeField] private WeaponDefinitionSO weaponSlot2;
        [SerializeField] private WeaponSlotType activeWeaponSlot = WeaponSlotType.None;
        [SerializeField] private ArmorDefinitionSO equippedArmor;

        public WeaponDefinitionSO WeaponSlot1 => weaponSlot1;
        public WeaponDefinitionSO WeaponSlot2 => weaponSlot2;
        public WeaponSlotType ActiveWeaponSlot => activeWeaponSlot;
        public ArmorDefinitionSO EquippedArmor => equippedArmor;

        public WeaponDefinitionSO GetWeapon(WeaponSlotType slot)
        {
            return slot switch
            {
                WeaponSlotType.Slot1 => weaponSlot1,
                WeaponSlotType.Slot2 => weaponSlot2,
                _ => null
            };
        }

        public WeaponDefinitionSO GetActiveWeapon()
        {
            return GetWeapon(activeWeaponSlot);
        }

        public void SetWeapon(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            switch (slot)
            {
                case WeaponSlotType.Slot1:
                    weaponSlot1 = weapon;
                    break;
                case WeaponSlotType.Slot2:
                    weaponSlot2 = weapon;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot senjata tidak valid.");
            }
        }

        public void SetActiveWeaponSlot(WeaponSlotType slot)
        {
            activeWeaponSlot = slot;
        }

        public void SetArmor(ArmorDefinitionSO armor)
        {
            equippedArmor = armor;
        }

        public bool IsWeaponEquipped(WeaponDefinitionSO weapon)
        {
            if (weapon == null) return false;
            return weaponSlot1 == weapon || weaponSlot2 == weapon;
        }

        public WeaponSlotType FindSlotOfWeapon(WeaponDefinitionSO weapon)
        {
            if (weapon == null) return WeaponSlotType.None;
            if (weaponSlot1 == weapon) return WeaponSlotType.Slot1;
            if (weaponSlot2 == weapon) return WeaponSlotType.Slot2;
            return WeaponSlotType.None;
        }

        public void Clear()
        {
            weaponSlot1 = null;
            weaponSlot2 = null;
            activeWeaponSlot = WeaponSlotType.None;
            equippedArmor = null;
        }
    }
}

```

#### `EquipmentContracts.cs`

Kontrak penghubung ke `PlayerActionController` dan `InventoryController` (Phase 3) agar `EquipmentController` tetap *decoupled* dan langsung dapat diuji sebelum Inventory selesai dibangun.

```csharp
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public interface IWeaponSwitchGate
    {
        bool CanSwitchWeapon();
        bool CanChangeEquipment();
    }

    public interface IItemOwnershipQuery
    {
        bool HasItem(ItemDefinitionSO item);
    }
}

```

---

### 2. Single Source of Truth (`EquipmentController.cs`)

Mengelola seluruh transaksi *equip*, *unequip*, dan *switching* beserta validasi *action state* dan event notifikasi.

```csharp
using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class EquipmentController : MonoBehaviour
    {
        [Header("Initial Loadout (Optional / Testing)")]
        [SerializeField] private WeaponDefinitionSO startingWeaponSlot1;
        [SerializeField] private WeaponDefinitionSO startingWeaponSlot2;
        [SerializeField] private WeaponSlotType startingActiveSlot = WeaponSlotType.Slot1;
        [SerializeField] private ArmorDefinitionSO startingArmor;

        [Header("Validation Options")]
        [Tooltip("Jika aktif dan Inventory belum terpasang, equip tetap diizinkan untuk pengujian.")]
        [SerializeField] private bool allowEquipWithoutInventoryInEditor = true;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private PlayerEquipmentState state = new PlayerEquipmentState();

        private IWeaponSwitchGate _actionGate;
        private IItemOwnershipQuery _ownershipQuery;

        // Events (Point 4.5)
        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponEquipped;
        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponUnequipped;
        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponSwitched;
        public event Action<ArmorDefinitionSO> OnArmorEquipped;
        public event Action<ArmorDefinitionSO> OnArmorUnequipped;
        public event Action OnEquipmentChanged;

        public PlayerEquipmentState State => state;
        public WeaponSlotType ActiveWeaponSlot => state.ActiveWeaponSlot;
        public WeaponDefinitionSO ActiveWeapon => state.GetActiveWeapon();
        public ArmorDefinitionSO EquippedArmor => state.EquippedArmor;

        private void Awake()
        {
            _actionGate = GetComponent<IWeaponSwitchGate>();
            _ownershipQuery = GetComponent<IItemOwnershipQuery>();
        }

        private void Start()
        {
            ApplyInitialLoadout();
        }

        public void SetActionGate(IWeaponSwitchGate actionGate)
        {
            _actionGate = actionGate;
        }

        public void SetOwnershipQuery(IItemOwnershipQuery ownershipQuery)
        {
            _ownershipQuery = ownershipQuery;
        }

        public void ApplyInitialLoadout()
        {
            state.Clear();

            if (startingWeaponSlot1 != null)
            {
                state.SetWeapon(WeaponSlotType.Slot1, startingWeaponSlot1);
                OnWeaponEquipped?.Invoke(WeaponSlotType.Slot1, startingWeaponSlot1);
            }

            if (startingWeaponSlot2 != null)
            {
                state.SetWeapon(WeaponSlotType.Slot2, startingWeaponSlot2);
                OnWeaponEquipped?.Invoke(WeaponSlotType.Slot2, startingWeaponSlot2);
            }

            if (startingArmor != null)
            {
                state.SetArmor(startingArmor);
                OnArmorEquipped?.Invoke(startingArmor);
            }

            if (state.GetWeapon(startingActiveSlot) != null)
            {
                state.SetActiveWeaponSlot(startingActiveSlot);
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

        public bool TryEquipWeapon(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            if (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2)
            {
                Debug.LogWarning($"[EquipmentController] Target slot tidak valid: {slot}.", this);
                return false;
            }

            if (weapon == null)
            {
                Debug.LogWarning("[EquipmentController] Gagal equip: WeaponDefinitionSO null.", this);
                return false;
            }

            if (!CanModifyEquipment())
            {
                return false;
            }

            if (!VerifyOwnership(weapon))
            {
                Debug.LogWarning($"[EquipmentController] Item '{weapon.ItemId}' tidak dimiliki di Inventory.", this);
                return false;
            }

            // Aturan v1.0: Satu definisi weapon tidak boleh terpasang di kedua slot sekaligus
            WeaponSlotType existingSlot = state.FindSlotOfWeapon(weapon);
            if (existingSlot != WeaponSlotType.None && existingSlot != slot)
            {
                Debug.LogWarning($"[EquipmentController] '{weapon.ItemId}' sudah terpasang di {existingSlot}.", this);
                return false;
            }

            WeaponDefinitionSO previousInSlot = state.GetWeapon(slot);
            if (previousInSlot == weapon)
            {
                return true;
            }

            state.SetWeapon(slot, weapon);
            OnWeaponEquipped?.Invoke(slot, weapon);

            // Otomatis aktifkan slot jika belum ada senjata aktif, atau perbarui jika slot aktif baru saja diganti
            if (state.ActiveWeaponSlot == WeaponSlotType.None || state.ActiveWeaponSlot == slot)
            {
                state.SetActiveWeaponSlot(slot);
                OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
            }

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TryUnequipWeapon(WeaponSlotType slot)
        {
            if (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2)
            {
                return false;
            }

            WeaponDefinitionSO currentWeapon = state.GetWeapon(slot);
            if (currentWeapon == null)
            {
                return false;
            }

            if (!CanModifyEquipment())
            {
                return false;
            }

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
            return true;
        }

        public bool TrySwitchWeaponSlot(WeaponSlotType targetSlot)
        {
            if (targetSlot != WeaponSlotType.Slot1 && targetSlot != WeaponSlotType.Slot2)
            {
                return false;
            }

            if (state.ActiveWeaponSlot == targetSlot)
            {
                return false;
            }

            // Tolak perpindahan ke slot yang kosong
            WeaponDefinitionSO targetWeapon = state.GetWeapon(targetSlot);
            if (targetWeapon == null)
            {
                return false;
            }

            // Point 2.4: Validasi PlayerActionController.CanSwitchWeapon()
            if (_actionGate != null && !_actionGate.CanSwitchWeapon())
            {
                return false;
            }

            state.SetActiveWeaponSlot(targetSlot);
            OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, targetWeapon);
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TryToggleWeaponSlot()
        {
            WeaponSlotType nextSlot = state.ActiveWeaponSlot == WeaponSlotType.Slot1
                ? WeaponSlotType.Slot2
                : WeaponSlotType.Slot1;

            return TrySwitchWeaponSlot(nextSlot);
        }

        public bool TryEquipArmor(ArmorDefinitionSO armor)
        {
            if (armor == null)
            {
                Debug.LogWarning("[EquipmentController] Gagal equip: ArmorDefinitionSO null.", this);
                return false;
            }

            if (!CanModifyEquipment())
            {
                return false;
            }

            if (!VerifyOwnership(armor))
            {
                Debug.LogWarning($"[EquipmentController] Armor '{armor.ItemId}' tidak dimiliki di Inventory.", this);
                return false;
            }

            if (state.EquippedArmor == armor)
            {
                return true;
            }

            state.SetArmor(armor);
            OnArmorEquipped?.Invoke(armor);
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TryUnequipArmor()
        {
            ArmorDefinitionSO previousArmor = state.EquippedArmor;
            if (previousArmor == null)
            {
                return false;
            }

            if (!CanModifyEquipment())
            {
                return false;
            }

            state.SetArmor(null);
            OnArmorUnequipped?.Invoke(previousArmor);
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public WeaponDefinitionSO GetActiveWeapon()
        {
            return state.GetActiveWeapon();
        }

        public PlayerEquipmentState GetEquipmentState()
        {
            return state;
        }

        private bool CanModifyEquipment()
        {
            if (_actionGate == null)
            {
                return true;
            }

            return _actionGate.CanChangeEquipment() && _actionGate.CanSwitchWeapon();
        }

        private bool VerifyOwnership(ItemDefinitionSO item)
        {
            if (_ownershipQuery != null)
            {
                return _ownershipQuery.HasItem(item);
            }

            return allowEquipWithoutInventoryInEditor;
        }
    }
}

```

---

### 3. Input Adapter Murni (`ItemSwitching.cs`)

Menghapus kepemilikan `AttackDefinitionSO[] Attacks`, `Index` terpisah, dan *runtime fallback*. `ItemSwitching` hanya menerjemahkan input pemain menjadi perintah ke `EquipmentController`.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class ItemSwitching : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;

        [Header("Quick Debug Keyboard Input (Optional)")]
        [SerializeField] private bool enableLegacyKeyFallback = false;

        public WeaponSlotType CurrentSlot => equipmentController != null
            ? equipmentController.ActiveWeaponSlot
            : WeaponSlotType.None;

        public WeaponDefinitionSO CurrentWeapon => equipmentController != null
            ? equipmentController.ActiveWeapon
            : null;

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }
        }

        private void Update()
        {
            if (!enableLegacyKeyFallback || equipmentController == null)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SwitchToSlot1();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SwitchToSlot2();
            }
            else if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                ToggleWeaponSlot();
            }
        }

        // Hook untuk Unity Input System (PlayerInput -> Unity Events) atau PlayerInputReader
        public void OnSwitchSlot1Input(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SwitchToSlot1();
            }
        }

        public void OnSwitchSlot2Input(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SwitchToSlot2();
            }
        }

        public void OnToggleWeaponInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ToggleWeaponSlot();
            }
        }

        public void SwitchToSlot1()
        {
            if (equipmentController != null)
            {
                equipmentController.TrySwitchWeaponSlot(WeaponSlotType.Slot1);
            }
        }

        public void SwitchToSlot2()
        {
            if (equipmentController != null)
            {
                equipmentController.TrySwitchWeaponSlot(WeaponSlotType.Slot2);
            }
        }

        public void ToggleWeaponSlot()
        {
            if (equipmentController != null)
            {
                equipmentController.TryToggleWeaponSlot();
            }
        }
    }
}

```

---

### 4. Scene-Authored Visual Controller (`EquipmentVisualController.cs`)

Mengatur visibilitas model senjata dan armor yang sudah dipasang di Hierarchy (`WeaponSocket` dan `ArmorSocket`) tanpa `Instantiate` atau `Destroy`.

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

        [Tooltip("Opsional: daftarkan child visual di dalam WeaponSlot_01 jika satu slot bisa menampung lebih dari satu jenis senjata.")]
        [SerializeField] private List<VisualEntry> slot01Visuals = new List<VisualEntry>();

        [Tooltip("Opsional: daftarkan child visual di dalam WeaponSlot_02 jika satu slot bisa menampung lebih dari satu jenis senjata.")]
        [SerializeField] private List<VisualEntry> slot02Visuals = new List<VisualEntry>();

        [Header("Armor Socket (Scene-Authored)")]
        [SerializeField] private Renderer armorRenderer;
        [SerializeField] private Material defaultArmorMaterial;
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
            if (equipmentController == null) return;

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
            if (equipmentController == null) return;

            ArmorDefinitionSO armor = equipmentController.EquippedArmor;

            if (armorRenderer != null)
            {
                armorRenderer.sharedMaterial = (armor != null && armor.ArmorMaterial != null)
                    ? armor.ArmorMaterial
                    : defaultArmorMaterial;
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
                if (obj == null) continue;

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

### 5. Combat & Animator Adapter (`WeaponCombatAdapter.cs`)

Menjamin `AttackController` dan `Animator` selalu tersinkronisasi dengan `ActiveWeapon` dari `EquipmentController` tanpa membuat `EquipmentController` bergantung langsung pada logika serangan.

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

        // Referensikan AttackController milik proyek Anda di sini
        // [SerializeField] private AttackController attackController;

        private RuntimeAnimatorController _baseAnimatorController;

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

        private void HandleWeaponSwitched(WeaponSlotType activeSlot, WeaponDefinitionSO activeWeapon)
        {
            ApplyAnimatorOverride(activeWeapon);
            ApplyCombatLoadout(activeWeapon);
        }

        private void ApplyAnimatorOverride(WeaponDefinitionSO weapon)
        {
            if (playerAnimator == null) return;

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
            // Hubungkan ke AttackController Anda saat integrasi Phase 4:
            // if (attackController == null) return;
            // if (weapon != null)
            //     attackController.SetWeaponLoadout(weapon);
            // else
            //     attackController.ClearWeaponLoadout();
        }
    }
}

```

---

### 6. Panduan Wiring di Prefab Player (`CombatTest.unity`)

1. **Hierarchy Socket Player:**
```text
Player
├── WeaponSocket
│   ├── WeaponSlot_01
│   │   └── KlewangVisual
│   └── WeaponSlot_02
│       └── PedangCepatVisual
└── ArmorSocket
    └── ArmorVisual

```


2. **Komponen pada Root `Player`:**
* Pasang `EquipmentController`, `ItemSwitching`, `EquipmentVisualController`, dan `WeaponCombatAdapter`.
* Pada `EquipmentController`, isi `Starting Weapon Slot 1` dengan `Weapon_Klewang` dan `Starting Weapon Slot 2` dengan `Weapon_PedangCepat`.
* Pada `EquipmentVisualController`, drag `WeaponSlot_01` ke `Weapon Slot 01 Root` dan `WeaponSlot_02` ke `Weapon Slot 02 Root`.


3. **Integrasi `PlayerActionController`:**
* Implementasikan `IWeaponSwitchGate` pada `PlayerActionController` (mengembalikan `false` saat sedang *attacking*, *staggered*, atau *dodging*) agar pergantian senjata otomatis ditolak saat animasi serangan berlangsung.