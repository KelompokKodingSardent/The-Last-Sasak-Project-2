using UnityEngine;

public class LootDropController : MonoBehaviour
{
    public static LootDropController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Drop(Vector3 position, int souls, int exp)
    {
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public class LootPickup : MonoBehaviour
{
    public int Souls = 2;
    public int Exp = 10;

    void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        Inventory inventory = other.GetComponentInParent<Inventory>();
        inventory?.Add(new InventoryItem { Id = "loot", DisplayName = "Loot", Souls = Souls, Exp = Exp });
        CurrencyWallet.Instance?.Add(Souls);
        ExperienceSystem.Instance?.Add(Exp);
        gameObject.SetActive(false);
    }
}
