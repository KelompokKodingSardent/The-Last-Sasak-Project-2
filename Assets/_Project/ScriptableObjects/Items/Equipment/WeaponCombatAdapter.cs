using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class WeaponCombatAdapter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private Animator playerAnimator;
        [Tooltip("Opsional: Komponen AttackController yang mengimplementasikan IWeaponCombatReceiver.")]
        [SerializeField] private MonoBehaviour combatReceiverComponent;

        private RuntimeAnimatorController _baseAnimatorController;
        private IWeaponCombatReceiver _combatReceiver;

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }

            if (playerAnimator != null)
            {
                _baseAnimatorController = playerAnimator.runtimeAnimatorController;
            }

            _combatReceiver = combatReceiverComponent != null
                ? combatReceiverComponent as IWeaponCombatReceiver
                : GetComponent<IWeaponCombatReceiver>();
        }

        private void OnEnable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched += HandleWeaponSwitched;
            }
        }

        private void OnDisable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched -= HandleWeaponSwitched;
            }
        }

        private void Start()
        {
            // Jaminan sinkronisasi awal terlepas dari urutan Start() antar komponen
            if (equipmentController != null)
            {
                SyncCombatWithWeapon(equipmentController.ActiveWeapon);
            }
        }

        private void HandleWeaponSwitched(WeaponSlotType activeSlot, WeaponDefinitionSO activeWeapon)
        {
            SyncCombatWithWeapon(activeWeapon);
        }

        public void SyncCombatWithWeapon(WeaponDefinitionSO weapon)
        {
            ApplyAnimatorOverride(weapon);
            ApplyCombatLoadout(weapon);
        }

        private void ApplyAnimatorOverride(WeaponDefinitionSO weapon)
        {
            if (playerAnimator == null)
            {
                return;
            }

            if (weapon != null && weapon.AnimatorOverride != null)
            {
                playerAnimator.runtimeAnimatorController = weapon.AnimatorOverride;
            }
            else if (_baseAnimatorController != null)
            {
                playerAnimator.runtimeAnimatorController = _baseAnimatorController;
            }
        }

        private void ApplyCombatLoadout(WeaponDefinitionSO weapon)
        {
            if (_combatReceiver == null)
            {
                return;
            }

            if (weapon != null)
            {
                if (weapon.ComboChain == null || weapon.ComboChain.Count == 0)
                {
                    Debug.LogError($"[WeaponCombatAdapter] Senjata '{weapon.ItemId}' tidak memiliki ComboChain yang valid!", weapon);
                    _combatReceiver.ClearWeaponLoadout();
                    return;
                }

                _combatReceiver.SetWeaponLoadout(weapon);
            }
            else
            {
                _combatReceiver.ClearWeaponLoadout();
            }
        }
    }
}