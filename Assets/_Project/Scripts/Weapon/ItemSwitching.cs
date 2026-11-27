using UnityEngine;
using TheLastSasak.Items.Equipment;

public class ItemSwitching : MonoBehaviour
{
    private EquipmentController equipment;
    private PlayerInputReader input;

    private void Awake()
    {
        equipment = GetComponent<EquipmentController>();
        input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (input == null || equipment == null || input.WeaponSlotPressed < 0)
            return;

        equipment.TrySwitchWeaponSlot(input.WeaponSlotPressed);
    }
}