using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

namespace TheLastSasak.Items.Loot
{
    public class LootCollector : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Inventory.Inventory inventory;
        [Tooltip("Komponen CurrencyWallet yang mengimplementasikan ICurrencyReceiver.")]
        [SerializeField] private MonoBehaviour currencyWalletComponent;
        [Tooltip("Komponen ExperienceSystem yang mengimplementasikan IExperienceReceiver.")]
        [SerializeField] private MonoBehaviour experienceSystemComponent;

        private ICurrencyReceiver _currencyReceiver;
        private IExperienceReceiver _experienceReceiver;

        public event Action<LootDefinitionSO, int> OnLootCollected;
        public event Action<LootDefinitionSO> OnLootCollectionFailed;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory.Inventory>();
            }

            _currencyReceiver = currencyWalletComponent != null
                ? currencyWalletComponent as ICurrencyReceiver
                : GetComponent<ICurrencyReceiver>();

            _experienceReceiver = experienceSystemComponent != null
                ? experienceSystemComponent as IExperienceReceiver
                : GetComponent<IExperienceReceiver>();
        }

        public void SetCurrencyReceiver(ICurrencyReceiver receiver)
        {
            _currencyReceiver = receiver;
        }

        public void SetExperienceReceiver(IExperienceReceiver receiver)
        {
            _experienceReceiver = receiver;
        }

        public bool TryCollectLoot(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (definition == null || requestedAmount <= 0)
            {
                return false;
            }

            switch (definition.RewardType)
            {
                case LootRewardType.Item:
                    return CollectItemReward(definition, requestedAmount, out consumedAmount);

                case LootRewardType.Currency:
                    return CollectCurrencyReward(definition, requestedAmount, out consumedAmount);

                case LootRewardType.Experience:
                    return CollectExperienceReward(definition, requestedAmount, out consumedAmount);

                default:
                    return false;
            }
        }

        private bool CollectItemReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;
            ItemDefinitionSO itemDef = definition.ItemDefinition;

            if (inventory == null || itemDef == null)
            {
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            bool addedAny = inventory.TryAddItem(itemDef, requestedAmount, out consumedAmount);
            if (!addedAny || consumedAmount <= 0)
            {
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }

        private bool CollectCurrencyReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (_currencyReceiver == null)
            {
                Debug.LogWarning("[LootCollector] ICurrencyReceiver (CurrencyWallet) belum terhubung.", this);
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            _currencyReceiver.AddCurrency(requestedAmount);
            consumedAmount = requestedAmount;
            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }

        private bool CollectExperienceReward(LootDefinitionSO definition, int requestedAmount, out int consumedAmount)
        {
            consumedAmount = 0;

            if (_experienceReceiver == null)
            {
                Debug.LogWarning("[LootCollector] IExperienceReceiver (ExperienceSystem) belum terhubung.", this);
                OnLootCollectionFailed?.Invoke(definition);
                return false;
            }

            _experienceReceiver.AddExperience(requestedAmount);
            consumedAmount = requestedAmount;
            OnLootCollected?.Invoke(definition, consumedAmount);
            return true;
        }
    }
}