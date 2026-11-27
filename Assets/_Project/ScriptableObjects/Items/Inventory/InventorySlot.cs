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