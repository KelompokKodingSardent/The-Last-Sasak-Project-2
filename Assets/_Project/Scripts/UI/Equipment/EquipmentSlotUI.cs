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