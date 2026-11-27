using UnityEngine;

namespace TheLastSasak.Items.Equipment
{
    public class PlayerActionGateAdapter : MonoBehaviour, IWeaponSwitchGate
    {
        // Hubungkan ke PlayerActionController Anda:
        // [SerializeField] private PlayerActionController actionController;

        [Header("Debug Override (Testing)")]
        [SerializeField] private bool lockEquipmentActionsInEditor = false;

        public bool CanSwitchWeapon()
        {
            if (lockEquipmentActionsInEditor)
            {
                return false;
            }

            // Ganti dengan pemeriksaan state PlayerActionController Anda, misalnya:
            // return actionController == null || actionController.CanSwitchWeapon();
            return true;
        }

        public bool CanChangeEquipment()
        {
            if (lockEquipmentActionsInEditor)
            {
                return false;
            }

            // Misalnya: hanya izinkan equip/unequip saat pemain tidak dalam state combat terkunci
            // return actionController == null || (!actionController.IsAttacking && !actionController.IsStaggered && !actionController.IsDead);
            return CanSwitchWeapon();
        }
    }
}