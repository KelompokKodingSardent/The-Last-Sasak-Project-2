using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Text Label;
    Inventory inventory;

    public void Bind(Inventory source)
    {
        inventory = source;
        Refresh();
    }

    void Update() => Refresh();

    void Refresh()
    {
        if (Label == null)
            return;
        int count = inventory != null ? inventory.Items.Count : 0;
        Label.text = $"Bag {count}";
    }
}
