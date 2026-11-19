using System.Collections.Generic;
using UnityEngine;

public class InventoryItem
{
    public string Id;
    public string DisplayName;
    public int Souls;
    public int Exp;
}

public class Inventory : MonoBehaviour
{
    public readonly List<InventoryItem> Items = new List<InventoryItem>();
    public int Capacity = 20;

    public bool Add(InventoryItem item)
    {
        if (item == null || Items.Count >= Capacity)
            return false;
        Items.Add(item);
        return true;
    }
}
