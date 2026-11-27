using UnityEngine;
using UnityEngine.InputSystem;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class ItemSwitching : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;

        [Header("Quick Debug Keyboard Input (Optional)")]
        [SerializeField] private bool enableLegacyKeyFallback = false;

        public WeaponSlotType CurrentSlot => equipmentController != null
            ? equipmentController.ActiveWeaponSlot
            : WeaponSlotType.None;

        public WeaponDefinitionSO CurrentWeapon => equipmentController != null
            ? equipmentController.ActiveWeapon
            : null;

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }
        }

        private void Update()
        {
            if (!enableLegacyKeyFallback || equipmentController == null)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SwitchToSlot1();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SwitchToSlot2();
            }
            else if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                ToggleWeaponSlot();
            }
        }

        // Hook untuk Unity Input System (PlayerInput -> Unity Events) atau PlayerInputReader
        public void OnSwitchSlot1Input(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SwitchToSlot1();
            }
        }

        public void OnSwitchSlot2Input(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SwitchToSlot2();
            }
        }

        public void OnToggleWeaponInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ToggleWeaponSlot();
            }
        }

        public void SwitchToSlot1()
        {
            if (equipmentController != null)
            {
                equipmentController.TrySwitchWeaponSlot(WeaponSlotType.Slot1);
            }
        }

        public void SwitchToSlot2()
        {
            if (equipmentController != null)
            {
                equipmentController.TrySwitchWeaponSlot(WeaponSlotType.Slot2);
            }
        }

        public void ToggleWeaponSlot()
        {
            if (equipmentController != null)
            {
                equipmentController.TryToggleWeaponSlot();
            }
        }
    }
}