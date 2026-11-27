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