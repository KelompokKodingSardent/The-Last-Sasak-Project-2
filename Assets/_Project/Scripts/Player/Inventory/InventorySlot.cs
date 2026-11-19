public class InventorySlot
{
    public InventoryItem Item;
    public bool IsEmpty => Item == null;
}
