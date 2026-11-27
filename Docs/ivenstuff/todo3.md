Implementasi **Phase 3 — Inventory Core** memisahkan penyimpanan data (`InventoryModel` dan `InventorySlot`), logika transaksi (`InventoryController`), serta jembatan komponen Unity (`Inventory.cs`) yang langsung terhubung dengan `IItemOwnershipQuery` milik `EquipmentController`.

### Struktur Target Phase 3

```text
Assets/_Project/Scripts/Items/Inventory/
├── InventoryItem.cs
├── InventorySlot.cs
├── InventoryModel.cs
├── InventoryController.cs
└── Inventory.cs

```

---

### 1. Item Entry & Slot Data (`InventoryItem.cs` & `InventorySlot.cs`)

#### `InventoryItem.cs`

Menggantikan struktur lama (`Id`, `DisplayName`, `Souls`, `Exp`) dengan referensi immutable ke `ItemDefinitionSO` dan jumlah kepemilikan. `Souls` dan `Exp` dikeluarkan sepenuhnya agar tetap dikelola oleh `CurrencyWallet` dan `ExperienceSystem`.

```csharp
using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Inventory
{
    [Serializable]
    public struct InventoryItem
    {
        [SerializeField] private ItemDefinitionSO definition;
        [Min(1)]
        [SerializeField] private int quantity;

        public ItemDefinitionSO Definition => definition;
        public int Quantity => quantity;
        public string ItemId => definition != null ? definition.ItemId : string.Empty;
        public bool IsValid => definition != null && quantity > 0;

        public InventoryItem(ItemDefinitionSO definition, int quantity)
        {
            this.definition = definition;
            this.quantity = Mathf.Max(0, quantity);
        }
    }
}

```

#### `InventorySlot.cs`

Merepresentasikan satu kotak penyimpanan dalam grid 20 slot.

```csharp
using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [SerializeField] private int quantity;

        public int SlotIndex => slotIndex;
        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int Quantity => quantity;

        public bool IsEmpty => itemDefinition == null || quantity <= 0;
        public bool IsFull => !IsEmpty && quantity >= itemDefinition.MaxStackSize;
        public int RemainingCapacity => IsEmpty ? 0 : Mathf.Max(0, itemDefinition.MaxStackSize - quantity);

        public InventorySlot(int slotIndex)
        {
            this.slotIndex = slotIndex;
            this.itemDefinition = null;
            this.quantity = 0;
        }

        public void Set(ItemDefinitionSO definition, int newQuantity)
        {
            if (definition == null || newQuantity <= 0)
            {
                Clear();
                return;
            }

            itemDefinition = definition;
            quantity = Mathf.Clamp(newQuantity, 1, definition.MaxStackSize);
        }

        public int AddQuantity(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return 0;
            }

            int spaceLeft = RemainingCapacity;
            int added = Mathf.Min(amount, spaceLeft);
            quantity += added;
            return added;
        }

        public int RemoveQuantity(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return 0;
            }

            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;

            if (quantity <= 0)
            {
                Clear();
            }

            return removed;
        }

        public void Clear()
        {
            itemDefinition = null;
            quantity = 0;
        }

        public InventoryItem ToItemSnapshot()
        {
            return IsEmpty ? default : new InventoryItem(itemDefinition, quantity);
        }
    }
}

```

---

### 2. Pure C# Storage State (`InventoryModel.cs`)

Menyimpan array `InventorySlot`, menghitung kapasitas kosong, dan menegakkan aturan *stacking* serta kepemilikan tunggal *equipment* v1.0 tanpa ketergantungan pada `MonoBehaviour`.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Inventory
{
    [Serializable]
    public class InventoryModel
    {
        [SerializeField] private InventorySlot[] slots;
        [SerializeField] private bool allowDuplicateEquipment;

        public int Capacity => slots != null ? slots.Length : 0;
        public bool AllowDuplicateEquipment => allowDuplicateEquipment;
        public IReadOnlyList<InventorySlot> Slots => slots;

        public InventoryModel(int capacity = 20, bool allowDuplicateEquipment = false)
        {
            int validCapacity = Mathf.Max(1, capacity);
            this.allowDuplicateEquipment = allowDuplicateEquipment;
            slots = new InventorySlot[validCapacity];

            for (int i = 0; i < validCapacity; i++)
            {
                slots[i] = new InventorySlot(i);
            }
        }

        public int GetUsedSlotCount()
        {
            int used = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    used++;
                }
            }
            return used;
        }

        public int GetEmptySlotCount()
        {
            return Capacity - GetUsedSlotCount();
        }

        public int GetItemCount(ItemDefinitionSO item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemDefinition == item)
                {
                    total += slots[i].Quantity;
                }
            }
            return total;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return 0;

            string normalizedId = itemId.Trim().ToLowerInvariant();
            int total = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemDefinition.ItemId == normalizedId)
                {
                    total += slots[i].Quantity;
                }
            }
            return total;
        }

        public bool HasItem(ItemDefinitionSO item, int minQuantity = 1)
        {
            if (item == null || minQuantity <= 0) return false;
            return GetItemCount(item) >= minQuantity;
        }

        public bool HasItem(string itemId, int minQuantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || minQuantity <= 0) return false;
            return GetItemCount(itemId) >= minQuantity;
        }

        public InventorySlot FindSlot(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;

            string normalizedId = itemId.Trim().ToLowerInvariant();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemDefinition.ItemId == normalizedId)
                {
                    return slots[i];
                }
            }
            return null;
        }

        public InventorySlot FindSlot(ItemDefinitionSO item)
        {
            if (item == null) return null;

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemDefinition == item)
                {
                    return slots[i];
                }
            }
            return null;
        }

        public int GetMaxAddableAmount(ItemDefinitionSO item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                return 0;
            }

            // Aturan v1.0 (Bagian III Point 6.1): Satu kepemilikan per definisi equipment
            if (!allowDuplicateEquipment && item is EquipmentDefinitionSO)
            {
                return HasItem(item) ? 0 : 1;
            }

            int maxStack = Mathf.Max(1, item.MaxStackSize);
            int addable = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    addable += maxStack;
                }
                else if (slot.ItemDefinition == item && !slot.IsFull)
                {
                    addable += slot.RemainingCapacity;
                }
            }

            return addable;
        }

        public void ClearAll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }
        }
    }
}

```

---

### 3. Transaction & Event Controller (`InventoryController.cs`)

Menangani seluruh kontrak metode transaksi (`CanAddItem`, `TryAddItem`, `TryRemoveItem`, `FindItem`, `HasItem`, `GetItemCount`, `GetSlots`) beserta 3 skenario penambahan item (penuh seluruhnya, masuk sebagian, atau ditolak karena penuh).

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Inventory
{
    public class InventoryController
    {
        private readonly InventoryModel _model;

        // Events (Point 3.4)
        public event Action<ItemDefinitionSO, int> OnItemAdded;
        public event Action<ItemDefinitionSO, int> OnItemRemoved;
        public event Action OnInventoryChanged;

        public InventoryModel Model => _model;
        public int Capacity => _model.Capacity;

        public InventoryController(int capacity = 20, bool allowDuplicateEquipment = false)
        {
            _model = new InventoryModel(capacity, allowDuplicateEquipment);
        }

        public InventoryController(InventoryModel existingModel)
        {
            _model = existingModel ?? new InventoryModel(20, false);
        }

        public bool CanAddItem(ItemDefinitionSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            return _model.GetMaxAddableAmount(item) >= amount;
        }

        /// <summary>
        /// Menambahkan item ke inventory. Mengembalikan true jika minimal 1 item berhasil masuk.
        /// Gunakan parameter out amountAdded untuk mengetahui sisa yang tidak muat (partial add).
        /// </summary>
        public bool TryAddItem(ItemDefinitionSO item, int amount, out int amountAdded)
        {
            amountAdded = 0;

            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || amount <= 0)
            {
                return false;
            }

            int maxAddable = _model.GetMaxAddableAmount(item);
            if (maxAddable <= 0)
            {
                return false;
            }

            int remainingToAdd = Mathf.Min(amount, maxAddable);
            IReadOnlyList<InventorySlot> slots = _model.Slots;

            // Langkah 1: Isi slot yang sudah memiliki item sejenis dan belum penuh
            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Count && remainingToAdd > 0; i++)
                {
                    InventorySlot slot = slots[i];
                    if (!slot.IsEmpty && slot.ItemDefinition == item && !slot.IsFull)
                    {
                        int added = slot.AddQuantity(remainingToAdd);
                        amountAdded += added;
                        remainingToAdd -= added;
                    }
                }
            }

            // Langkah 2: Alokasikan sisa jumlah ke slot kosong
            for (int i = 0; i < slots.Count && remainingToAdd > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int stackAmount = Mathf.Min(remainingToAdd, item.MaxStackSize);
                    slot.Set(item, stackAmount);
                    amountAdded += stackAmount;
                    remainingToAdd -= stackAmount;
                }
            }

            if (amountAdded > 0)
            {
                OnItemAdded?.Invoke(item, amountAdded);
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAddItem(ItemDefinitionSO item, int amount = 1)
        {
            return TryAddItem(item, amount, out _);
        }

        public bool TryRemoveItem(ItemDefinitionSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            // Transaksi atomik: tolak jika stok tidak mencukupi jumlah yang diminta
            if (_model.GetItemCount(item) < amount)
            {
                return false;
            }

            int remainingToRemove = amount;
            IReadOnlyList<InventorySlot> slots = _model.Slots;

            // Kurangi dari slot paling akhir agar tumpukan parsial habis lebih dulu
            for (int i = slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.ItemDefinition == item)
                {
                    int removed = slot.RemoveQuantity(remainingToRemove);
                    remainingToRemove -= removed;
                }
            }

            OnItemRemoved?.Invoke(item, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveItemById(string itemId, int amount = 1)
        {
            InventorySlot slot = _model.FindSlot(itemId);
            if (slot == null || slot.IsEmpty)
            {
                return false;
            }

            return TryRemoveItem(slot.ItemDefinition, amount);
        }

        public InventorySlot FindItem(string itemId)
        {
            return _model.FindSlot(itemId);
        }

        public InventorySlot FindItem(ItemDefinitionSO item)
        {
            return _model.FindSlot(item);
        }

        public bool HasItem(ItemDefinitionSO item, int minQuantity = 1)
        {
            return _model.HasItem(item, minQuantity);
        }

        public bool HasItem(string itemId, int minQuantity = 1)
        {
            return _model.HasItem(itemId, minQuantity);
        }

        public int GetItemCount(ItemDefinitionSO item)
        {
            return _model.GetItemCount(item);
        }

        public int GetItemCount(string itemId)
        {
            return _model.GetItemCount(itemId);
        }

        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _model.Slots;
        }

        public void ClearInventory()
        {
            _model.ClearAll();
            OnInventoryChanged?.Invoke();
        }
    }
}

```

---

### 4. MonoBehaviour Component Facade (`Inventory.cs`)

Mempertahankan kompatibilitas komponen `Inventory` di prefab `Player`, menyediakan konfigurasi kapasitas 20 slot di Inspector, dan mengimplementasikan `IItemOwnershipQuery` agar `EquipmentController` otomatis memvalidasi kepemilikan item sebelum *equip*.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;

namespace TheLastSasak.Items.Inventory
{
    public class Inventory : MonoBehaviour, IItemOwnershipQuery
    {
        [Header("Configuration")]
        [Min(1)]
        [SerializeField] private int capacity = 20;
        [Tooltip("v1.0 default false: satu kepemilikan per definisi equipment.")]
        [SerializeField] private bool allowDuplicateEquipment = false;

        [Header("Initial Test Items (Optional)")]
        [SerializeField] private List<InventoryItem> startingItems = new List<InventoryItem>();

        private InventoryController _controller;

        public event Action<ItemDefinitionSO, int> OnItemAdded;
        public event Action<ItemDefinitionSO, int> OnItemRemoved;
        public event Action OnInventoryChanged;

        public InventoryController Controller
        {
            get
            {
                EnsureInitialized();
                return _controller;
            }
        }

        public int Capacity => Controller.Capacity;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            PopulateStartingItems();
        }

        private void EnsureInitialized()
        {
            if (_controller != null) return;

            _controller = new InventoryController(capacity, allowDuplicateEquipment);
            _controller.OnItemAdded += HandleItemAdded;
            _controller.OnItemRemoved += HandleItemRemoved;
            _controller.OnInventoryChanged += HandleInventoryChanged;
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnItemAdded -= HandleItemAdded;
                _controller.OnItemRemoved -= HandleItemRemoved;
                _controller.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void PopulateStartingItems()
        {
            if (startingItems == null || startingItems.Count == 0) return;

            for (int i = 0; i < startingItems.Count; i++)
            {
                InventoryItem entry = startingItems[i];
                if (entry.IsValid)
                {
                    TryAddItem(entry.Definition, entry.Quantity);
                }
            }
        }

        // Implementasi IItemOwnershipQuery untuk EquipmentController (Phase 2)
        public bool HasItem(ItemDefinitionSO item)
        {
            return Controller.HasItem(item, 1);
        }

        public bool HasItem(ItemDefinitionSO item, int minQuantity)
        {
            return Controller.HasItem(item, minQuantity);
        }

        public bool HasItem(string itemId, int minQuantity = 1)
        {
            return Controller.HasItem(itemId, minQuantity);
        }

        public bool CanAddItem(ItemDefinitionSO item, int amount = 1)
        {
            return Controller.CanAddItem(item, amount);
        }

        public bool TryAddItem(ItemDefinitionSO item, int amount = 1)
        {
            return Controller.TryAddItem(item, amount);
        }

        public bool TryAddItem(ItemDefinitionSO item, int amount, out int amountAdded)
        {
            return Controller.TryAddItem(item, amount, out amountAdded);
        }

        public bool TryRemoveItem(ItemDefinitionSO item, int amount = 1)
        {
            return Controller.TryRemoveItem(item, amount);
        }

        public bool TryRemoveItemById(string itemId, int amount = 1)
        {
            return Controller.TryRemoveItemById(itemId, amount);
        }

        public InventorySlot FindItem(string itemId)
        {
            return Controller.FindItem(itemId);
        }

        public int GetItemCount(ItemDefinitionSO item)
        {
            return Controller.GetItemCount(item);
        }

        public int GetItemCount(string itemId)
        {
            return Controller.GetItemCount(itemId);
        }

        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return Controller.GetSlots();
        }

        private void HandleItemAdded(ItemDefinitionSO item, int amount)
        {
            OnItemAdded?.Invoke(item, amount);
        }

        private void HandleItemRemoved(ItemDefinitionSO item, int amount)
        {
            OnItemRemoved?.Invoke(item, amount);
        }

        private void HandleInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}

```

---

### 5. Checklist Wiring & Validasi Phase 3

1. **Integrasi Otomatis di Prefab `Player`:**
* Pastikan komponen `Inventory` dan `EquipmentController` berada pada GameObject root `Player` yang sama.
* Pada `EquipmentController.Awake()`, `GetComponent<IItemOwnershipQuery>()` otomatis terhubung ke `Inventory`.
* Matikan `Allow Equip Without Inventory In Editor` pada `EquipmentController` untuk menguji kasus **E08 (Equip Unowned Item ditolak)** secara langsung.


2. **Skenario Pengujian Point 3.5 (I01–I06):**
* **Null / Invalid (`I06`):** `TryAddItem(null, 1)` dan `TryAddItem(item, 0)` mengembalikan `false` tanpa memicu `OnInventoryChanged`.
* **Stacking & Overflow (`I02`, `I03`):** Menambahkan item dengan `MaxStackSize = 5` sebanyak `7` mengisi Slot 0 = `5` dan Slot 1 = `2`.
* **Partial Add & Full (`I04`):** Saat tersisa 1 slot kosong untuk item dengan `MaxStackSize = 5`, `TryAddItem(item, 8, out int added)` mengembalikan `true` dengan `added == 5`, lalu pemanggilan berikutnya mengembalikan `false` dengan `added == 0`.
* **Remove Atomik (`I05`):** Meminta `TryRemoveItem(item, 10)` saat stok hanya `7` mengembalikan `false` dan tidak mengurangi stok sama sekali.