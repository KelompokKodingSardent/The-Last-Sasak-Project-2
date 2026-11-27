using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Inventory;
using TheLastSasak.UI.Equipment;
using PlayerInventory = TheLastSasak.Items.Inventory.Inventory;

namespace TheLastSasak.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Domain Dependencies")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;

        [Header("UI Sub-Views")]
        [SerializeField] private Transform slotGridContainer;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private ItemCategoryFilter categoryFilter;
        [SerializeField] private ItemDetailUI itemDetailUI;
        [SerializeField] private StatComparisonUI statComparisonUI;
        [SerializeField] private TMP_Text capacityCounterText;

        private readonly List<InventorySlotUI> _spawnedSlots = new List<InventorySlotUI>();
        private ItemDefinitionSO _selectedItem;

        public ItemDefinitionSO SelectedItem => _selectedItem;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (equipmentController == null) equipmentController = FindFirstObjectByType<EquipmentController>();
            if (artifactController == null) artifactController = FindFirstObjectByType<ArtifactController>();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnInventoryChanged += RefreshGrid;
            if (equipmentController != null) equipmentController.OnEquipmentChanged += RefreshGrid;
            if (artifactController != null) artifactController.OnArtifactsChanged += RefreshGrid;
            if (categoryFilter != null) categoryFilter.OnFilterChanged += HandleFilterChanged;

            RefreshGrid();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= RefreshGrid;
            if (equipmentController != null) equipmentController.OnEquipmentChanged -= RefreshGrid;
            if (artifactController != null) artifactController.OnArtifactsChanged -= RefreshGrid;
            if (categoryFilter != null) categoryFilter.OnFilterChanged -= HandleFilterChanged;
        }

        private void HandleFilterChanged(InventoryFilterMode _)
        {
            RefreshGrid();
        }

        public void SelectItem(ItemDefinitionSO item)
        {
            _selectedItem = item;
            UpdateSelectionHighlights();

            if (itemDetailUI != null)
            {
                itemDetailUI.ShowItem(item);
            }

            if (statComparisonUI != null)
            {
                statComparisonUI.ShowComparison(item as EquipmentDefinitionSO);
            }
        }

        public void RefreshGrid()
        {
            if (inventory == null || slotGridContainer == null || slotPrefab == null)
            {
                return;
            }

            IReadOnlyList<InventorySlot> allSlots = inventory.GetSlots();
            InventoryFilterMode filterMode = categoryFilter != null
                ? categoryFilter.CurrentFilter
                : InventoryFilterMode.All;

            // Kumpulkan slot yang ditampilkan berdasarkan filter
            List<InventorySlot> visibleSlots = new List<InventorySlot>(allSlots.Count);
            int usedCount = 0;

            for (int i = 0; i < allSlots.Count; i++)
            {
                InventorySlot slot = allSlots[i];
                if (!slot.IsEmpty)
                {
                    usedCount++;
                }

                if (filterMode == InventoryFilterMode.All)
                {
                    visibleSlots.Add(slot);
                }
                else if (!slot.IsEmpty && categoryFilter != null && categoryFilter.Matches(slot.ItemDefinition))
                {
                    visibleSlots.Add(slot);
                }
            }

            EnsureSlotPoolSize(visibleSlots.Count);

            // Reset seleksi jika item terpilih tidak lagi ada di inventory
            if (_selectedItem != null && !inventory.HasItem(_selectedItem))
            {
                _selectedItem = null;
            }

            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                InventorySlotUI slotUI = _spawnedSlots[i];
                if (i < visibleSlots.Count)
                {
                    slotUI.gameObject.SetActive(true);
                    InventorySlot data = visibleSlots[i];
                    bool equipped = !data.IsEmpty && IsItemCurrentlyEquipped(data.ItemDefinition);
                    bool selected = !data.IsEmpty && data.ItemDefinition == _selectedItem;
                    slotUI.Bind(data, equipped, selected, HandleSlotClicked);
                }
                else
                {
                    slotUI.gameObject.SetActive(false);
                }
            }

            if (capacityCounterText != null)
            {
                capacityCounterText.text = $"{usedCount} / {inventory.Capacity}";
            }

            if (itemDetailUI != null)
            {
                itemDetailUI.ShowItem(_selectedItem);
            }

            if (statComparisonUI != null)
            {
                statComparisonUI.ShowComparison(_selectedItem as EquipmentDefinitionSO);
            }
        }

        private void EnsureSlotPoolSize(int requiredCount)
        {
            while (_spawnedSlots.Count < requiredCount)
            {
                InventorySlotUI instance = Instantiate(slotPrefab, slotGridContainer);
                _spawnedSlots.Add(instance);
            }
        }

        private void HandleSlotClicked(InventorySlotUI clickedSlot)
        {
            if (clickedSlot == null || clickedSlot.ItemDefinition == null)
            {
                SelectItem(null);
                return;
            }

            SelectItem(clickedSlot.ItemDefinition);
        }

        private void UpdateSelectionHighlights()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                InventorySlotUI slotUI = _spawnedSlots[i];
                if (slotUI.gameObject.activeSelf)
                {
                    slotUI.SetSelected(slotUI.ItemDefinition != null && slotUI.ItemDefinition == _selectedItem);
                }
            }
        }

        private bool IsItemCurrentlyEquipped(ItemDefinitionSO item)
        {
            if (item == null) return false;

            if (equipmentController != null)
            {
                if (item is WeaponDefinitionSO weapon && equipmentController.State.IsWeaponEquipped(weapon))
                {
                    return true;
                }

                if (item is ArmorDefinitionSO armor && equipmentController.EquippedArmor == armor)
                {
                    return true;
                }
            }

            if (artifactController != null && item is ArtifactDefinitionSO artifact)
            {
                return artifactController.IsArtifactEquipped(artifact);
            }

            return false;
        }
public void Bind(PlayerInventory targetInventory)
{
    if (inventory == targetInventory) return;
    if (isActiveAndEnabled && inventory != null) inventory.OnInventoryChanged -= RefreshGrid;
    inventory = targetInventory;
    if (isActiveAndEnabled && inventory != null) inventory.OnInventoryChanged += RefreshGrid;
    RefreshGrid();
}

    }
}