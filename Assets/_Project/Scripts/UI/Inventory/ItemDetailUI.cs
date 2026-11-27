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