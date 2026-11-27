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