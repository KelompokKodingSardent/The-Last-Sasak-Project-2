using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public interface IWeaponSwitchGate
    {
        bool CanSwitchWeapon();
        bool CanChangeEquipment();
    }

    public interface IItemOwnershipQuery
    {
        bool HasItem(ItemDefinitionSO item);
    }
}