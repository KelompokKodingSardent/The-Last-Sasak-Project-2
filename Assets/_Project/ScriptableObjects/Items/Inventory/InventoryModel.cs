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