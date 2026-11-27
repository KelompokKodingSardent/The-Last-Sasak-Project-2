Implementasi **Phase 8 — Inventory & Equipment UI** memisahkan lapisan presentasi sepenuhnya dari penyimpanan data dan kalkulasi statistik. Seluruh komponen UI hanya mengirim perintah ke `EquipmentTransactionHandler`, `EquipmentController`, dan `ArtifactController`, lalu memperbarui tampilan saat menerima event perubahan state.

### Struktur Target Phase 8

```text
Assets/_Project/Scripts/UI/
├── Inventory/
│   ├── ItemCategoryFilter.cs
│   ├── InventorySlotUI.cs
│   ├── InventoryUI.cs
│   ├── ItemDetailUI.cs
│   └── InventoryFeedbackUI.cs
└── Equipment/
    ├── EquipmentSlotUI.cs
    ├── EquipmentUI.cs
    └── StatComparisonUI.cs

```

---

### 1. Category Filter & Feedback Toast (`UI/Inventory/`)

#### `ItemCategoryFilter.cs`

Mengatur filter tab kategori (`All | Weapon | Armor | Artifact`) sesuai **Point 11.1**.

```csharp
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

```

#### `InventoryFeedbackUI.cs`

Menampilkan notifikasi transaksi (berhasil dipasang, dilepas, ditolak karena *combat state*, atau *inventory* penuh).

```csharp
using System.Collections;
using TMPro;
using UnityEngine;

namespace TheLastSasak.UI.Inventory
{
    public class InventoryFeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        [Header("Colors & Timing")]
        [SerializeField] private Color infoColor = new Color(0.92f, 0.90f, 0.84f, 1f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.42f, 0.35f, 1f);
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 0.35f;

        private Coroutine _activeRoutine;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void ShowInfo(string message)
        {
            ShowMessage(message, infoColor);
        }

        public void ShowWarning(string message)
        {
            ShowMessage(message, warningColor);
        }

        private void ShowMessage(string message, Color color)
        {
            if (messageText == null || canvasGroup == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }

            messageText.text = message;
            messageText.color = color;
            _activeRoutine = StartCoroutine(AnimateToast());
        }

        private IEnumerator AnimateToast()
        {
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(displayDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            _activeRoutine = null;
        }
    }
}

```

---

### 2. Inventory Slot & Grid Controller (`UI/Inventory/`)

#### `InventorySlotUI.cs`

Komponen tiap kotak pada grid 20 slot: menampilkan icon, jumlah tumpukan, indikator *Equipped*, dan sorotan seleksi.

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

namespace TheLastSasak.UI.Inventory
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button slotButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private GameObject selectionHighlight;

        private InventorySlot _boundSlot;
        private Action<InventorySlotUI> _onClicked;

        public InventorySlot BoundSlot => _boundSlot;
        public ItemDefinitionSO ItemDefinition => _boundSlot != null && !_boundSlot.IsEmpty ? _boundSlot.ItemDefinition : null;

        private void Awake()
        {
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(HandleClick);
            }
        }

        public void Bind(InventorySlot slot, bool isEquipped, bool isSelected, Action<InventorySlotUI> onClicked)
        {
            _boundSlot = slot;
            _onClicked = onClicked;

            bool hasItem = slot != null && !slot.IsEmpty && slot.ItemDefinition != null;

            if (iconImage != null)
            {
                iconImage.enabled = hasItem && slot.ItemDefinition.Icon != null;
                iconImage.sprite = hasItem ? slot.ItemDefinition.Icon : null;
            }

            if (quantityText != null)
            {
                bool showQuantity = hasItem && slot.Quantity > 1;
                quantityText.gameObject.SetActive(showQuantity);
                if (showQuantity)
                {
                    quantityText.text = slot.Quantity.ToString();
                }
            }

            if (equippedBadge != null)
            {
                equippedBadge.SetActive(hasItem && isEquipped);
            }

            SetSelected(hasItem && isSelected);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(selected);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(this);
        }
    }
}

```

#### `InventoryUI.cs`

Membangun grid 20 slot dari `Inventory.GetSlots()`, mendengarkan perubahan `Inventory`, `EquipmentController`, dan `ArtifactController`, serta meneruskan item terpilih ke `ItemDetailUI` dan `StatComparisonUI`.

```csharp
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
    }
}

```

---

### 3. Item Detail & Contextual Actions (`UI/Inventory/ItemDetailUI.cs`)

Menampilkan informasi lengkap item yang dipilih beserta tombol aksi kontekstual sesuai kategori (**Point 11.2**):

* **Weapon:** `[Equip Slot 1]`, `[Equip Slot 2]`, dan `[Unequip]` apabila sedang terpasang.
* **Armor:** `[Equip]` atau `[Unequip]`.
* **Artifact:** `[Equip Slot 1]`, `[Equip Slot 2]`, atau `[Unequip]`.

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;

namespace TheLastSasak.UI.Inventory
{
    public class ItemDetailUI : MonoBehaviour
    {
        [Header("Domain Dependencies")]
        [SerializeField] private EquipmentTransactionHandler transactionHandler;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;
        [SerializeField] private InventoryFeedbackUI feedbackUI;

        [Header("Detail Views")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text equippedStatusText;

        [Header("Action Buttons")]
        [SerializeField] private Button primaryEquipButton;
        [SerializeField] private TMP_Text primaryEquipLabel;
        [SerializeField] private Button secondaryEquipButton;
        [SerializeField] private TMP_Text secondaryEquipLabel;
        [SerializeField] private Button unequipButton;

        private ItemDefinitionSO _currentItem;

        private void Awake()
        {
            if (transactionHandler == null) transactionHandler = FindFirstObjectByType<EquipmentTransactionHandler>();
            if (equipmentController == null) equipmentController = FindFirstObjectByType<EquipmentController>();
            if (artifactController == null) artifactController = FindFirstObjectByType<ArtifactController>();

            if (primaryEquipButton != null) primaryEquipButton.onClick.AddListener(HandlePrimaryEquipClick);
            if (secondaryEquipButton != null) secondaryEquipButton.onClick.AddListener(HandleSecondaryEquipClick);
            if (unequipButton != null) unequipButton.onClick.AddListener(HandleUnequipClick);
        }

        private void OnDestroy()
        {
            if (primaryEquipButton != null) primaryEquipButton.onClick.RemoveAllListeners();
            if (secondaryEquipButton != null) secondaryEquipButton.onClick.RemoveAllListeners();
            if (unequipButton != null) unequipButton.onClick.RemoveAllListeners();
        }

        public void ShowItem(ItemDefinitionSO item)
        {
            _currentItem = item;

            if (item == null)
            {
                if (contentRoot != null) contentRoot.SetActive(false);
                return;
            }

            if (contentRoot != null) contentRoot.SetActive(true);

            if (itemIconImage != null)
            {
                Sprite displaySprite = item is ArtifactDefinitionSO art ? art.ArtifactVisual : item.Icon;
                itemIconImage.enabled = displaySprite != null;
                itemIconImage.sprite = displaySprite;
            }

            if (itemNameText != null)
            {
                itemNameText.text = item.DisplayName;
            }

            if (categoryText != null)
            {
                categoryText.text = item.Category.ToString().ToUpperInvariant();
            }

            if (descriptionText != null)
            {
                descriptionText.text = item is ArtifactDefinitionSO artifact
                    ? artifact.PassiveDescription
                    : item.Description;
            }

            ConfigureActionButtons(item);
        }

        private void ConfigureActionButtons(ItemDefinitionSO item)
        {
            SetButtonVisible(primaryEquipButton, false);
            SetButtonVisible(secondaryEquipButton, false);
            SetButtonVisible(unequipButton, false);

            if (equippedStatusText != null)
            {
                equippedStatusText.text = string.Empty;
            }

            if (item is WeaponDefinitionSO weapon && equipmentController != null)
            {
                WeaponSlotType equippedSlot = equipmentController.State.FindSlotOfWeapon(weapon);
                bool isEquipped = equippedSlot != WeaponSlotType.None;

                if (equippedStatusText != null)
                {
                    equippedStatusText.text = isEquipped ? $"Terpasang di Weapon {equippedSlot}" : string.Empty;
                }

                if (equippedSlot != WeaponSlotType.Slot1)
                {
                    SetButtonVisible(primaryEquipButton, true);
                    if (primaryEquipLabel != null) primaryEquipLabel.text = "Equip Slot 1";
                }

                if (equippedSlot != WeaponSlotType.Slot2)
                {
                    SetButtonVisible(secondaryEquipButton, true);
                    if (secondaryEquipLabel != null) secondaryEquipLabel.text = "Equip Slot 2";
                }

                SetButtonVisible(unequipButton, isEquipped);
            }
            else if (item is ArmorDefinitionSO armor && equipmentController != null)
            {
                bool isEquipped = equipmentController.EquippedArmor == armor;

                if (equippedStatusText != null)
                {
                    equippedStatusText.text = isEquipped ? "Terpasang di Armor Slot" : string.Empty;
                }

                SetButtonVisible(primaryEquipButton, !isEquipped);
                if (primaryEquipLabel != null) primaryEquipLabel.text = "Equip Armor";
                SetButtonVisible(unequipButton, isEquipped);
            }
            else if (item is ArtifactDefinitionSO artifact && artifactController != null)
            {
                int slotIndex = artifactController.FindSlotOfArtifact(artifact);
                bool isEquipped = slotIndex != -1;

                if (equippedStatusText != null)
                {
                    equippedStatusText.text = isEquipped ? $"Terpasang di Artifact Slot {slotIndex + 1}" : string.Empty;
                }

                if (slotIndex != 0)
                {
                    SetButtonVisible(primaryEquipButton, true);
                    if (primaryEquipLabel != null) primaryEquipLabel.text = "Equip Slot 1";
                }

                if (slotIndex != 1 && artifactController.SlotCount >= 2)
                {
                    SetButtonVisible(secondaryEquipButton, true);
                    if (secondaryEquipLabel != null) secondaryEquipLabel.text = "Equip Slot 2";
                }

                SetButtonVisible(unequipButton, isEquipped);
            }
        }

        private void HandlePrimaryEquipClick()
        {
            if (_currentItem == null) return;

            bool success = false;
            if (_currentItem is WeaponDefinitionSO && transactionHandler != null)
            {
                success = transactionHandler.TryEquipFromInventory(_currentItem, WeaponSlotType.Slot1);
            }
            else if (_currentItem is ArmorDefinitionSO && transactionHandler != null)
            {
                success = transactionHandler.TryEquipFromInventory(_currentItem);
            }
            else if (_currentItem is ArtifactDefinitionSO artifact && artifactController != null)
            {
                success = artifactController.TryEquipArtifact(0, artifact);
            }

            NotifyTransactionResult(success, $"{_currentItem.DisplayName} terpasang.", "Gagal memasang perlengkapan.");
        }

        private void HandleSecondaryEquipClick()
        {
            if (_currentItem == null) return;

            bool success = false;
            if (_currentItem is WeaponDefinitionSO && transactionHandler != null)
            {
                success = transactionHandler.TryEquipFromInventory(_currentItem, WeaponSlotType.Slot2);
            }
            else if (_currentItem is ArtifactDefinitionSO artifact && artifactController != null)
            {
                success = artifactController.TryEquipArtifact(1, artifact);
            }

            NotifyTransactionResult(success, $"{_currentItem.DisplayName} terpasang ke Slot 2.", "Gagal memasang perlengkapan.");
        }

        private void HandleUnequipClick()
        {
            if (_currentItem == null) return;

            bool success = false;
            if (_currentItem is WeaponDefinitionSO weapon && equipmentController != null)
            {
                WeaponSlotType slot = equipmentController.State.FindSlotOfWeapon(weapon);
                if (slot != WeaponSlotType.None)
                {
                    success = equipmentController.TryUnequipWeapon(slot);
                }
            }
            else if (_currentItem is ArmorDefinitionSO && equipmentController != null)
            {
                success = equipmentController.TryUnequipArmor();
            }
            else if (_currentItem is ArtifactDefinitionSO artifact && artifactController != null)
            {
                int slotIndex = artifactController.FindSlotOfArtifact(artifact);
                if (slotIndex != -1)
                {
                    success = artifactController.TryUnequipArtifact(slotIndex);
                }
            }

            NotifyTransactionResult(success, $"{_currentItem.DisplayName} dilepas.", "Tidak dapat melepas perlengkapan sekarang.");
        }

        private void NotifyTransactionResult(bool success, string successMsg, string failMsg)
        {
            if (feedbackUI == null) return;

            if (success)
            {
                feedbackUI.ShowInfo(successMsg);
            }
            else
            {
                feedbackUI.ShowWarning(failMsg);
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}

```

---

### 4. Equipment Slots & Stat Comparison (`UI/Equipment/`)

#### `EquipmentSlotUI.cs`

Komponen tiap slot perlengkapan (`Weapon Slot 1`, `Weapon Slot 2`, `Armor Slot`, `Artifact Slot 1`, `Artifact Slot 2`).

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.UI.Equipment
{
    public class EquipmentSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text slotHeaderText;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject activeWeaponBadge;
        [SerializeField] private Button selectSlotButton;
        [SerializeField] private Button unequipSlotButton;

        private EquipmentDefinitionSO _equippedItem;
        private Action<EquipmentDefinitionSO> _onSelectCallback;
        private Action _onUnequipCallback;

        private void Awake()
        {
            if (selectSlotButton != null) selectSlotButton.onClick.AddListener(HandleSelect);
            if (unequipSlotButton != null) unequipSlotButton.onClick.AddListener(HandleUnequip);
        }

        private void OnDestroy()
        {
            if (selectSlotButton != null) selectSlotButton.onClick.RemoveAllListeners();
            if (unequipSlotButton != null) unequipSlotButton.onClick.RemoveAllListeners();
        }

        public void Bind(
            string slotTitle,
            EquipmentDefinitionSO item,
            bool isActiveWeapon,
            Action<EquipmentDefinitionSO> onSelect,
            Action onUnequip)
        {
            _equippedItem = item;
            _onSelectCallback = onSelect;
            _onUnequipCallback = onUnequip;

            bool hasItem = item != null;

            if (slotHeaderText != null)
            {
                slotHeaderText.text = slotTitle;
            }

            if (itemNameText != null)
            {
                itemNameText.text = hasItem ? item.DisplayName : "Kosong";
            }

            if (iconImage != null)
            {
                Sprite sprite = item is ArtifactDefinitionSO art ? art.ArtifactVisual : (hasItem ? item.Icon : null);
                iconImage.enabled = sprite != null;
                iconImage.sprite = sprite;
            }

            if (activeWeaponBadge != null)
            {
                activeWeaponBadge.SetActive(hasItem && isActiveWeapon);
            }

            if (unequipSlotButton != null)
            {
                unequipSlotButton.gameObject.SetActive(hasItem);
            }
        }

        private void HandleSelect()
        {
            _onSelectCallback?.Invoke(_equippedItem);
        }

        private void HandleUnequip()
        {
            _onUnequipCallback?.Invoke();
        }
    }
}

```

#### `EquipmentUI.cs`

Mengatur kelima slot perlengkapan dan tombol pergantian senjata aktif (**Point 11.2**).

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.UI.Inventory;

namespace TheLastSasak.UI.Equipment
{
    public class EquipmentUI : MonoBehaviour
    {
        [Header("Domain Dependencies")]
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private InventoryFeedbackUI feedbackUI;

        [Header("Equipment Slot Views")]
        [SerializeField] private EquipmentSlotUI weaponSlot1UI;
        [SerializeField] private EquipmentSlotUI weaponSlot2UI;
        [SerializeField] private EquipmentSlotUI armorSlotUI;
        [SerializeField] private EquipmentSlotUI artifactSlot1UI;
        [SerializeField] private EquipmentSlotUI artifactSlot2UI;

        [Header("Active Weapon Controls")]
        [SerializeField] private Button switchActiveWeaponButton;
        [SerializeField] private TMP_Text activeWeaponLabel;

        private void Awake()
        {
            if (equipmentController == null) equipmentController = FindFirstObjectByType<EquipmentController>();
            if (artifactController == null) artifactController = FindFirstObjectByType<ArtifactController>();
            if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUI>();

            if (switchActiveWeaponButton != null)
            {
                switchActiveWeaponButton.onClick.AddListener(HandleSwitchWeaponClick);
            }
        }

        private void OnEnable()
        {
            if (equipmentController != null) equipmentController.OnEquipmentChanged += RefreshEquipmentSlots;
            if (artifactController != null) artifactController.OnArtifactsChanged += RefreshEquipmentSlots;

            RefreshEquipmentSlots();
        }

        private void OnDisable()
        {
            if (equipmentController != null) equipmentController.OnEquipmentChanged -= RefreshEquipmentSlots;
            if (artifactController != null) artifactController.OnArtifactsChanged -= RefreshEquipmentSlots;
        }

        private void OnDestroy()
        {
            if (switchActiveWeaponButton != null)
            {
                switchActiveWeaponButton.onClick.RemoveAllListeners();
            }
        }

        public void RefreshEquipmentSlots()
        {
            if (equipmentController != null)
            {
                PlayerEquipmentState state = equipmentController.State;

                if (weaponSlot1UI != null)
                {
                    weaponSlot1UI.Bind(
                        "Weapon Slot 1",
                        state.WeaponSlot1,
                        state.ActiveWeaponSlot == WeaponSlotType.Slot1,
                        HandleSlotSelected,
                        () => equipmentController.TryUnequipWeapon(WeaponSlotType.Slot1));
                }

                if (weaponSlot2UI != null)
                {
                    weaponSlot2UI.Bind(
                        "Weapon Slot 2",
                        state.WeaponSlot2,
                        state.ActiveWeaponSlot == WeaponSlotType.Slot2,
                        HandleSlotSelected,
                        () => equipmentController.TryUnequipWeapon(WeaponSlotType.Slot2));
                }

                if (armorSlotUI != null)
                {
                    armorSlotUI.Bind(
                        "Armor",
                        state.EquippedArmor,
                        false,
                        HandleSlotSelected,
                        () => equipmentController.TryUnequipArmor());
                }

                if (activeWeaponLabel != null)
                {
                    WeaponDefinitionSO active = state.GetActiveWeapon();
                    activeWeaponLabel.text = active != null
                        ? $"Active: {active.DisplayName} ({state.ActiveWeaponSlot})"
                        : "Active: Tidak Ada";
                }
            }

            if (artifactController != null)
            {
                if (artifactSlot1UI != null)
                {
                    artifactSlot1UI.Bind(
                        "Artifact 1",
                        artifactController.GetArtifactInSlot(0),
                        false,
                        HandleSlotSelected,
                        () => artifactController.TryUnequipArtifact(0));
                }

                if (artifactSlot2UI != null)
                {
                    artifactSlot2UI.Bind(
                        "Artifact 2",
                        artifactController.GetArtifactInSlot(1),
                        false,
                        HandleSlotSelected,
                        () => artifactController.TryUnequipArtifact(1));
                }
            }
        }

        private void HandleSlotSelected(EquipmentDefinitionSO item)
        {
            if (inventoryUI != null && item != null)
            {
                inventoryUI.SelectItem(item);
            }
        }

        private void HandleSwitchWeaponClick()
        {
            if (equipmentController == null) return;

            bool switched = equipmentController.TryToggleWeaponSlot();
            if (!switched && feedbackUI != null)
            {
                feedbackUI.ShowWarning("Tidak dapat mengganti senjata saat ini.");
            }
        }
    }
}

```

#### `StatComparisonUI.cs` & Hook Pratinjau Artefak

Menampilkan tabel perbandingan `Current` vs `New` beserta selisih (`Changes`) langsung dari hasil kalkulasi `PlayerStatController` tanpa menulis angka secara manual di UI (**Point 11.3**).

Tambahkan dulu dukungan `ArtifactDefinitionSO` di dalam `PlayerStatController.PreviewStatsWithCandidate` (Phase 5):

```csharp
// Tambahkan di PlayerStatController.PreviewStatsWithCandidate() sebelum mengumpulkan artifact modifiers:
List<ArtifactDefinitionSO> previewArtifacts = new List<ArtifactDefinitionSO>();
if (artifactController != null)
{
    previewArtifacts.AddRange(artifactController.GetActiveArtifacts());
    if (candidate is ArtifactDefinitionSO candidateArtifact && !previewArtifacts.Contains(candidateArtifact))
    {
        if (previewArtifacts.Count < artifactController.SlotCount)
            previewArtifacts.Add(candidateArtifact);
        else if (previewArtifacts.Count > 0)
            previewArtifacts[0] = candidateArtifact;
    }
}

for (int i = 0; i < previewArtifacts.Count; i++)
{
    foreach (StatModifier mod in previewArtifacts[i].GetScopedModifiers())
    {
        previewBuffer.Add(mod);
    }
}

```

Kemudian gunakan `StatComparisonUI.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Player.Stats;

namespace TheLastSasak.UI.Equipment
{
    public class StatComparisonUI : MonoBehaviour
    {
        [Serializable]
        public struct StatRowView
        {
            public StatType statType;
            public string displayLabel;
            public bool isPercentage;
            public GameObject rowRoot;
            public TMP_Text labelText;
            public TMP_Text currentValueText;
            public TMP_Text newValueText;
            public TMP_Text deltaText;
        }

        [Header("Domain Dependency")]
        [SerializeField] private PlayerStatController playerStatController;

        [Header("Stat Rows")]
        [SerializeField] private List<StatRowView> statRows = new List<StatRowView>();

        [Header("Delta Colors")]
        [SerializeField] private Color positiveColor = new Color(0.35f, 0.88f, 0.45f, 1f);
        [SerializeField] private Color negativeColor = new Color(0.92f, 0.36f, 0.32f, 1f);
        [SerializeField] private Color neutralColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        private EquipmentDefinitionSO _candidateItem;

        private void Awake()
        {
            if (playerStatController == null)
            {
                playerStatController = FindFirstObjectByType<PlayerStatController>();
            }
        }

        private void OnEnable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsRecalculated += RefreshDisplay;
            }

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsRecalculated -= RefreshDisplay;
            }
        }

        public void ShowComparison(EquipmentDefinitionSO candidate)
        {
            _candidateItem = candidate;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (playerStatController == null)
            {
                return;
            }

            Dictionary<StatType, float> previewStats = _candidateItem != null
                ? playerStatController.PreviewStatsWithCandidate(_candidateItem)
                : null;

            for (int i = 0; i < statRows.Count; i++)
            {
                StatRowView row = statRows[i];
                float currentVal = playerStatController.GetStat(row.statType);
                float nextVal = previewStats != null && previewStats.TryGetValue(row.statType, out float pVal)
                    ? pVal
                    : currentVal;

                float delta = nextVal - currentVal;
                bool hasChange = Mathf.Abs(delta) > 0.001f;

                if (row.labelText != null && !string.IsNullOrEmpty(row.displayLabel))
                {
                    row.labelText.text = row.displayLabel;
                }

                if (row.currentValueText != null)
                {
                    row.currentValueText.text = FormatStatValue(currentVal, row.isPercentage);
                }

                if (row.newValueText != null)
                {
                    row.newValueText.text = previewStats != null
                        ? FormatStatValue(nextVal, row.isPercentage)
                        : "-";
                }

                if (row.deltaText != null)
                {
                    if (!hasChange || previewStats == null)
                    {
                        row.deltaText.text = "-";
                        row.deltaText.color = neutralColor;
                    }
                    else
                    {
                        string sign = delta > 0f ? "+" : "";
                        row.deltaText.text = $"{sign}{FormatStatValue(delta, row.isPercentage)}";
                        row.deltaText.color = delta > 0f ? positiveColor : negativeColor;
                    }
                }
            }
        }

        private static string FormatStatValue(float value, bool isPercentage)
        {
            if (isPercentage)
            {
                return $"{value * 100f:0.#}%";
            }

            return Mathf.Approximately(value % 1f, 0f)
                ? $"{value:0}"
                : $"{value:0.##}";
        }
    }
}

```

---

### 5. Panduan Wiring Prefab & Canvas UI (`Assets/_Project/Prefabs/UI/`)

1. **Prefab `InventorySlotUI.prefab`:**
* Buat UI `Button` dengan child `IconImage` (`Image`), `QuantityText` (`TextMeshProUGUI`), `EquippedBadge` (`GameObject` kecil bertuliskan `"E"`), dan `SelectionHighlight` (`Image` border).
* Pasang script `InventorySlotUI` dan referensikan keempat elemen tersebut.


2. **Hierarki Canvas `InventoryEquipmentCanvas`:**
```text
InventoryEquipmentCanvas
├── LeftPanel_Equipment
│   ├── EquipmentUI (pasang EquipmentUI.cs)
│   │   ├── WeaponSlot1_UI  (EquipmentSlotUI)
│   │   ├── WeaponSlot2_UI  (EquipmentSlotUI)
│   │   ├── ArmorSlot_UI    (EquipmentSlotUI)
│   │   ├── ArtifactSlot1_UI (EquipmentSlotUI)
│   │   ├── ArtifactSlot2_UI (EquipmentSlotUI)
│   │   └── SwitchWeaponButton
│   └── StatComparisonPanel (StatComparisonUI)
│       └── Rows: AttackDamage, AttackSpeed, MaxHealth, DamageReduction (isPercentage = true), PoiseResistance
├── CenterPanel_Inventory
│   ├── CategoryFilterBar (ItemCategoryFilter: All, Weapon, Armor, Artifact)
│   ├── CapacityText ("4 / 20")
│   └── SlotGrid (GridLayoutGroup + InventoryUI.cs, hubungkan InventorySlotUI.prefab)
├── RightPanel_Detail
│   └── ItemDetailPanel (ItemDetailUI: Icon, Name, Category, Description, Primary/Secondary/Unequip Buttons)
└── FeedbackToast (InventoryFeedbackUI + CanvasGroup)

```


3. **Verifikasi Alur Interaksi:**
* Klik item di `SlotGrid` → `ItemDetailUI` menampilkan tombol aksi sesuai kategori dan `StatComparisonUI` menampilkan selisih atribut (`+` hijau / `-` merah).
* Klik `Equip Slot 1` / `Equip Slot 2` → `InventorySlotUI` langsung memunculkan badge `Equipped`, `EquipmentUI` memperbarui slot terpasang, dan model senjata di tangan `Player` langsung berganti.