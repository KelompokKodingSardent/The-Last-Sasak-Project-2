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