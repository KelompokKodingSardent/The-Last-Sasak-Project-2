using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

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

    [Header("Optional Item Reward")]
    public ItemDefinitionSO ItemReward;
    [Min(1)] public int ItemQuantity = 1;

    void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        if (ItemReward != null)
        {
            Inventory inventory = other.GetComponentInParent<Inventory>();
            if (inventory == null || !inventory.TryAddItem(ItemReward, ItemQuantity))
            {
                // Inventory penuh: jangan nonaktifkan pickup agar item tidak hilang
                return;
            }
        }

        if (Souls > 0) CurrencyWallet.Instance?.Add(Souls);
        if (Exp > 0) ExperienceSystem.Instance?.Add(Exp);

        gameObject.SetActive(false);
    }
}