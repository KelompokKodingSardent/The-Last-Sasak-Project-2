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