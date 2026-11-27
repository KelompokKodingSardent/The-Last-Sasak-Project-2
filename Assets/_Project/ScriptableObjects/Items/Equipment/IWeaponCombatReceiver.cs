using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    /// <summary>
    /// Implementasikan interface ini pada AttackController milik proyek Anda.
    /// </summary>
    public interface IWeaponCombatReceiver
    {
        void SetWeaponLoadout(WeaponDefinitionSO weapon);
        void ClearWeaponLoadout();
    }
}


// Contoh integrasi di dalam AttackController.cs proyek Anda:
// public class AttackController : MonoBehaviour, IWeaponCombatReceiver
// {
//     private WeaponDefinitionSO _currentWeapon;
//     private IReadOnlyList<AttackDefinitionSO> _activeComboChain;
//     private int _comboIndex;
//
//     public void SetWeaponLoadout(WeaponDefinitionSO weapon)
//     {
//         _currentWeapon = weapon;
//         _activeComboChain = weapon.ComboChain;
//         _comboIndex = 0;
//     }
//
//     public void ClearWeaponLoadout()
//     {
//         _currentWeapon = null;
//         _activeComboChain = null;
//         _comboIndex = 0;
//     }
// }