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