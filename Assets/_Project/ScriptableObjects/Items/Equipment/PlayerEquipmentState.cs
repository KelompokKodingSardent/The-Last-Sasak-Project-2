using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public enum WeaponSlotType
    {
        None = 0,
        Slot1 = 1,
        Slot2 = 2
    }

    [Serializable]
    public class PlayerEquipmentState
    {
        [SerializeField] private WeaponDefinitionSO weaponSlot1;
        [SerializeField] private WeaponDefinitionSO weaponSlot2;
        [SerializeField] private WeaponSlotType activeWeaponSlot = WeaponSlotType.None;
        [SerializeField] private ArmorDefinitionSO equippedArmor;

        public WeaponDefinitionSO WeaponSlot1 => weaponSlot1;
        public WeaponDefinitionSO WeaponSlot2 => weaponSlot2;
        public WeaponSlotType ActiveWeaponSlot => activeWeaponSlot;
        public ArmorDefinitionSO EquippedArmor => equippedArmor;

        public WeaponDefinitionSO GetWeapon(WeaponSlotType slot)
        {
            return slot switch
            {
                WeaponSlotType.Slot1 => weaponSlot1,
                WeaponSlotType.Slot2 => weaponSlot2,
                _ => null
            };
        }

        public WeaponDefinitionSO GetActiveWeapon()
        {
            return GetWeapon(activeWeaponSlot);
        }

        public void SetWeapon(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            switch (slot)
            {
                case WeaponSlotType.Slot1:
                    weaponSlot1 = weapon;
                    break;
                case WeaponSlotType.Slot2:
                    weaponSlot2 = weapon;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot senjata tidak valid.");
            }
        }

        public void SetActiveWeaponSlot(WeaponSlotType slot)
        {
            activeWeaponSlot = slot;
        }

        public void SetArmor(ArmorDefinitionSO armor)
        {
            equippedArmor = armor;
        }

        public bool IsWeaponEquipped(WeaponDefinitionSO weapon)
        {
            if (weapon == null) return false;
            return weaponSlot1 == weapon || weaponSlot2 == weapon;
        }

        public WeaponSlotType FindSlotOfWeapon(WeaponDefinitionSO weapon)
        {
            if (weapon == null) return WeaponSlotType.None;
            if (weaponSlot1 == weapon) return WeaponSlotType.Slot1;
            if (weaponSlot2 == weapon) return WeaponSlotType.Slot2;
            return WeaponSlotType.None;
        }

        public void Clear()
        {
            weaponSlot1 = null;
            weaponSlot2 = null;
            activeWeaponSlot = WeaponSlotType.None;
            equippedArmor = null;
        }
    }
}