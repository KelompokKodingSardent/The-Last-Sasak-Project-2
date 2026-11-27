using System;
using UnityEngine;
using UnityEngine.UI;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.UI.Inventory
{
    public enum InventoryFilterMode
    {
        All = 0,
        Weapon = 1,
        Armor = 2,
        Artifact = 3
    }

    public class ItemCategoryFilter : MonoBehaviour
    {
        [Header("Filter Buttons")]
        [SerializeField] private Button allButton;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button artifactButton;

        [Header("Visual Highlight")]
        [SerializeField] private Color activeTabColor = new Color(0.92f, 0.78f, 0.42f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.25f, 0.25f, 0.28f, 1f);

        private InventoryFilterMode _currentFilter = InventoryFilterMode.All;

        public event Action<InventoryFilterMode> OnFilterChanged;

        public InventoryFilterMode CurrentFilter => _currentFilter;

        private void Awake()
        {
            if (allButton != null) allButton.onClick.AddListener(() => SetFilter(InventoryFilterMode.All));
            if (weaponButton != null) weaponButton.onClick.AddListener(() => SetFilter(InventoryFilterMode.Weapon));
            if (armorButton != null) armorButton.onClick.AddListener(() => SetFilter(InventoryFilterMode.Armor));
            if (artifactButton != null) artifactButton.onClick.AddListener(() => SetFilter(InventoryFilterMode.Artifact));
        }

        private void Start()
        {
            RefreshTabVisuals();
        }

        private void OnDestroy()
        {
            if (allButton != null) allButton.onClick.RemoveAllListeners();
            if (weaponButton != null) weaponButton.onClick.RemoveAllListeners();
            if (armorButton != null) armorButton.onClick.RemoveAllListeners();
            if (artifactButton != null) artifactButton.onClick.RemoveAllListeners();
        }

        public void SetFilter(InventoryFilterMode newFilter)
        {
            if (_currentFilter == newFilter)
            {
                return;
            }

            _currentFilter = newFilter;
            RefreshTabVisuals();
            OnFilterChanged?.Invoke(_currentFilter);
        }

        public bool Matches(ItemDefinitionSO item)
        {
            if (item == null)
            {
                return _currentFilter == InventoryFilterMode.All;
            }

            return _currentFilter switch
            {
                InventoryFilterMode.All => true,
                InventoryFilterMode.Weapon => item.Category == ItemCategory.Weapon,
                InventoryFilterMode.Armor => item.Category == ItemCategory.Armor,
                InventoryFilterMode.Artifact => item.Category == ItemCategory.Artifact,
                _ => true
            };
        }

        private void RefreshTabVisuals()
        {
            UpdateButtonColor(allButton, _currentFilter == InventoryFilterMode.All);
            UpdateButtonColor(weaponButton, _currentFilter == InventoryFilterMode.Weapon);
            UpdateButtonColor(armorButton, _currentFilter == InventoryFilterMode.Armor);
            UpdateButtonColor(artifactButton, _currentFilter == InventoryFilterMode.Artifact);
        }

        private void UpdateButtonColor(Button button, bool isActive)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = isActive ? activeTabColor : inactiveTabColor;
            }
        }
    }
}