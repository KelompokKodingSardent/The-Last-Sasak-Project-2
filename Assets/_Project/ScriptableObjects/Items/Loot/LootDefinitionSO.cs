using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Loot
{
    [CreateAssetMenu(fileName = "Loot_", menuName = "The Last Sasak/Loot/Loot Definition")]
    public class LootDefinitionSO : ScriptableObject
    {
        [Header("Persistent Identity")]
        [Tooltip("ID unik loot (contoh: loot.klewang, loot.souls_small).")]
        [SerializeField] private string lootId;

        [Header("Reward Configuration")]
        [SerializeField] private LootRewardType rewardType = LootRewardType.Item;
        [Tooltip("Wajib diisi jika RewardType bernilai Item.")]
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [Min(1)]
        [SerializeField] private int amount = 1;

        [Header("World Presentation & Pickup Rule")]
        [SerializeField] private LootPickup pickupPrefab;
        [SerializeField] private LootPickupCondition pickupCondition = LootPickupCondition.AutoOnTrigger;

        public string LootId => lootId;
        public LootRewardType RewardType => rewardType;
        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int Amount => amount;
        public LootPickup PickupPrefab => pickupPrefab;
        public LootPickupCondition PickupCondition => pickupCondition;

        public string GetDisplayLabel()
        {
            return rewardType switch
            {
                LootRewardType.Item => itemDefinition != null ? itemDefinition.DisplayName : "Unknown Item",
                LootRewardType.Currency => $"{amount} Souls",
                LootRewardType.Experience => $"{amount} EXP",
                _ => name
            };
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(lootId))
            {
                lootId = lootId.Trim().ToLowerInvariant();
            }

            if (amount < 1)
            {
                amount = 1;
            }

            if (string.IsNullOrWhiteSpace(lootId))
            {
                Debug.LogWarning($"[LootDefinitionSO] '{name}' belum memiliki LootId.", this);
            }

            if (rewardType == LootRewardType.Item && itemDefinition == null)
            {
                Debug.LogError($"[LootDefinitionSO] '{name}' bertipe Item tetapi ItemDefinition masih kosong.", this);
            }
            else if (rewardType != LootRewardType.Item && itemDefinition != null)
            {
                Debug.LogWarning($"[LootDefinitionSO] '{name}' bertipe {rewardType}; ItemDefinition tidak akan digunakan.", this);
            }
        }
    }
}