using UnityEngine;

namespace TheLastSasak.Items.Definitions
{
    public abstract class ItemDefinitionSO : ScriptableObject
    {
        [Header("Persistent Identity")]
        [Tooltip("ID permanen unik (contoh: weapon.klewang). Jangan bergantung pada nama file.")]
        [SerializeField] private string itemId;

        [Header("Presentation")]
        [SerializeField] private string displayName;
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Classification & Stacking")]
        [SerializeField] private ItemCategory category;
        [Min(1)]
        [SerializeField] private int maxStackSize = 1;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public int MaxStackSize => maxStackSize;

        public bool IsStackable => maxStackSize > 1;

        protected void SetCategory(ItemCategory enforcedCategory)
        {
            category = enforcedCategory;
        }

        protected void SetMaxStackSize(int enforcedStackSize)
        {
            maxStackSize = Mathf.Max(1, enforcedStackSize);
        }

        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                itemId = itemId.Trim().ToLowerInvariant();
            }

            if (maxStackSize < 1)
            {
                maxStackSize = 1;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' belum memiliki ItemId.", this);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' ({itemId}) belum memiliki DisplayName.", this);
            }

            if (icon == null)
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' ({itemId}) belum memiliki Icon untuk UI.", this);
            }
        }
    }
}