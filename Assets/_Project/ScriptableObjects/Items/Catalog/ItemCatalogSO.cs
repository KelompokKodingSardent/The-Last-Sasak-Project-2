using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Catalog
{
    [CreateAssetMenu(fileName = "ItemCatalog_Main", menuName = "The Last Sasak/Items/Item Catalog")]
    public class ItemCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ItemDefinitionSO> items = new List<ItemDefinitionSO>();

        private Dictionary<string, ItemDefinitionSO> _lookupCache;

        public IReadOnlyList<ItemDefinitionSO> AllItems => items;

        private void OnEnable()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            _lookupCache = new Dictionary<string, ItemDefinitionSO>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinitionSO item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (!_lookupCache.ContainsKey(item.ItemId))
                {
                    _lookupCache.Add(item.ItemId, item);
                }
            }
        }

        public bool TryGetItem(string itemId, out ItemDefinitionSO item)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            if (_lookupCache == null)
            {
                RebuildCache();
            }

            return _lookupCache.TryGetValue(itemId.Trim().ToLowerInvariant(), out item);
        }

        public bool TryGetItem<T>(string itemId, out T typedItem) where T : ItemDefinitionSO
        {
            if (TryGetItem(itemId, out ItemDefinitionSO item) && item is T casted)
            {
                typedItem = casted;
                return true;
            }

            typedItem = null;
            return false;
        }

        public bool Contains(string itemId)
        {
            return TryGetItem(itemId, out _);
        }

        [ContextMenu("Validate Catalog")]
        public void ValidateCatalog()
        {
            HashSet<string> seenIds = new HashSet<string>();
            int errorCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinitionSO item = items[i];
                if (item == null)
                {
                    Debug.LogError($"[ItemCatalogSO] '{name}' memiliki slot null pada index {i}.", this);
                    errorCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[ItemCatalogSO] Item '{item.name}' pada index {i} memiliki ItemId kosong.", item);
                    errorCount++;
                    continue;
                }

                if (!seenIds.Add(item.ItemId))
                {
                    Debug.LogError($"[ItemCatalogSO] DUPLIKAT ItemId ditemukan: '{item.ItemId}' pada asset '{item.name}'.", item);
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                Debug.Log($"[ItemCatalogSO] Validasi '{name}' sukses: {seenIds.Count} item unik terverifikasi.", this);
            }
        }

        private void OnValidate()
        {
            ValidateCatalog();
            RebuildCache();
        }
    }
}