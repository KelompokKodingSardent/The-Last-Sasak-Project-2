using System;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Player.Stats;
namespace TheLastSasak.Items.Equipment
{
    public class EquipmentController : MonoBehaviour
    {
        [Header("Initial Loadout (Optional)")]
        [SerializeField] private WeaponDefinitionSO initialWeaponSlot1;
        [SerializeField] private WeaponDefinitionSO initialWeaponSlot2;
        [SerializeField] private ArmorDefinitionSO initialArmor;

        [Header("Scene-Authored Weapon Visuals")]
        [SerializeField] private GameObject weaponSlot1Visual;
        [SerializeField] private GameObject weaponSlot2Visual;

        [Header("Armor Visual")]
        [SerializeField] private Renderer armorRenderer;

        [Header("Legacy Fallback")]
        [SerializeField] private AttackDefinitionSO[] loadout;

        private readonly PlayerEquipmentState state = new PlayerEquipmentState();

        private PlayerStatController statController;
        private PlayerActionController actionController;
        private Animator animator;
        private RuntimeAnimatorController defaultAnimatorController;
        private Material defaultArmorMaterial;

        public PlayerEquipmentState State => state;
        public WeaponDefinitionSO WeaponSlot1 => state.WeaponSlot1;
        public WeaponDefinitionSO WeaponSlot2 => state.WeaponSlot2;
        public WeaponDefinitionSO ActiveWeapon => state.GetActiveWeapon();
        public WeaponSlotType ActiveWeaponSlot => state.ActiveWeaponSlot;
        public ArmorDefinitionSO EquippedArmor => state.EquippedArmor;
        public AttackDefinitionSO[] Loadout => loadout;

        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponEquipped;
        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponUnequipped;
        public event Action<WeaponSlotType, WeaponDefinitionSO> OnWeaponSwitched;
        public event Action<ArmorDefinitionSO> OnArmorEquipped;
        public event Action<ArmorDefinitionSO> OnArmorUnequipped;
        public event Action OnEquipmentChanged;

        private void Awake()
        {
            statController = GetComponent<PlayerStatController>();
            actionController = GetComponent<PlayerActionController>();
            animator = GetComponentInChildren<Animator>();

            if (animator != null)
                defaultAnimatorController = animator.runtimeAnimatorController;

            if (armorRenderer != null)
                defaultArmorMaterial = armorRenderer.sharedMaterial;
        }

        private void Start()
        {
            if (state.WeaponSlot1 == null && initialWeaponSlot1 != null)
                state.SetWeapon(WeaponSlotType.Slot1, initialWeaponSlot1);

            if (state.WeaponSlot2 == null && initialWeaponSlot2 != null)
                state.SetWeapon(WeaponSlotType.Slot2, initialWeaponSlot2);

            if (state.ActiveWeaponSlot == WeaponSlotType.None)
            {
                if (state.WeaponSlot1 != null)
                    state.SetActiveWeaponSlot(WeaponSlotType.Slot1);
                else if (state.WeaponSlot2 != null)
                    state.SetActiveWeaponSlot(WeaponSlotType.Slot2);
            }

            if (state.EquippedArmor == null && initialArmor != null)
                state.SetArmor(initialArmor);

            RefreshPresentationAndStats();
        }

        public bool TryEquipWeapon(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            if (weapon == null || (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2))
                return false;

            if (!CanModifyEquipment())
                return false;

            state.SetWeapon(slot, weapon);
            OnWeaponEquipped?.Invoke(slot, weapon);

            if (state.ActiveWeaponSlot == WeaponSlotType.None)
            {
                state.SetActiveWeaponSlot(slot);
                OnWeaponSwitched?.Invoke(slot, weapon);
            }

            RefreshPresentationAndStats();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TryUnequipWeapon(WeaponSlotType slot)
        {
            if (!CanModifyEquipment())
                return false;

            ForceUnequipWeapon(slot);
            return true;
        }

        public void ForceUnequipWeapon(WeaponSlotType slot)
        {
            if (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2)
                return;

            WeaponDefinitionSO currentWeapon = state.GetWeapon(slot);
            if (currentWeapon == null)
                return;

            state.SetWeapon(slot, null);
            OnWeaponUnequipped?.Invoke(slot, currentWeapon);

            if (state.ActiveWeaponSlot == slot)
            {
                WeaponSlotType fallbackSlot = slot == WeaponSlotType.Slot1
                    ? WeaponSlotType.Slot2
                    : WeaponSlotType.Slot1;

                WeaponSlotType nextActive = state.GetWeapon(fallbackSlot) != null
                    ? fallbackSlot
                    : WeaponSlotType.None;

                state.SetActiveWeaponSlot(nextActive);
                OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
            }

            RefreshPresentationAndStats();
            OnEquipmentChanged?.Invoke();
        }

        public bool TrySwitchWeaponSlot(WeaponSlotType targetSlot)
        {
            if (targetSlot != WeaponSlotType.Slot1 && targetSlot != WeaponSlotType.Slot2)
                return false;

            if (state.ActiveWeaponSlot == targetSlot)
                return false;

            if (state.GetWeapon(targetSlot) == null)
                return false;

            if (!CanModifyEquipment())
                return false;

            state.SetActiveWeaponSlot(targetSlot);
            RefreshPresentationAndStats();
            OnWeaponSwitched?.Invoke(targetSlot, state.GetActiveWeapon());
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TrySwitchWeaponSlot(int zeroBasedSlotIndex)
        {
            WeaponSlotType targetSlot = zeroBasedSlotIndex switch
            {
                0 => WeaponSlotType.Slot1,
                1 => WeaponSlotType.Slot2,
                _ => WeaponSlotType.None
            };
            return TrySwitchWeaponSlot(targetSlot);
        }

        public bool TryToggleWeaponSlot()
        {
            WeaponSlotType nextSlot = state.ActiveWeaponSlot == WeaponSlotType.Slot1
                ? WeaponSlotType.Slot2
                : WeaponSlotType.Slot1;

            return TrySwitchWeaponSlot(nextSlot);
        }

        public bool TryEquipArmor(ArmorDefinitionSO armor)
        {
            if (armor == null || !CanModifyEquipment())
                return false;

            state.SetArmor(armor);
            RefreshPresentationAndStats();
            OnArmorEquipped?.Invoke(armor);
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool TryUnequipArmor()
        {
            if (!CanModifyEquipment())
                return false;

            ForceUnequipArmor();
            return true;
        }

        public void ForceUnequipArmor()
        {
            ArmorDefinitionSO previousArmor = state.EquippedArmor;
            if (previousArmor == null)
                return;

            state.SetArmor(null);
            RefreshPresentationAndStats();
            OnArmorUnequipped?.Invoke(previousArmor);
            OnEquipmentChanged?.Invoke();
        }

        public void RestoreEquipmentState(
            WeaponDefinitionSO slot1,
            WeaponDefinitionSO slot2,
            WeaponSlotType activeSlot,
            ArmorDefinitionSO armor)
        {
            state.Clear();

            if (slot1 != null)
            {
                state.SetWeapon(WeaponSlotType.Slot1, slot1);
                OnWeaponEquipped?.Invoke(WeaponSlotType.Slot1, slot1);
            }

            if (slot2 != null && slot2 != slot1)
            {
                state.SetWeapon(WeaponSlotType.Slot2, slot2);
                OnWeaponEquipped?.Invoke(WeaponSlotType.Slot2, slot2);
            }

            if (armor != null)
            {
                state.SetArmor(armor);
                OnArmorEquipped?.Invoke(armor);
            }

            if (state.GetWeapon(activeSlot) != null)
                state.SetActiveWeaponSlot(activeSlot);
            else if (state.WeaponSlot1 != null)
                state.SetActiveWeaponSlot(WeaponSlotType.Slot1);
            else if (state.WeaponSlot2 != null)
                state.SetActiveWeaponSlot(WeaponSlotType.Slot2);
            else
                state.SetActiveWeaponSlot(WeaponSlotType.None);

            RefreshPresentationAndStats();
            OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
            OnEquipmentChanged?.Invoke();
        }

        private bool CanModifyEquipment()
        {
            return actionController == null || actionController.CanSwitchWeapon();
        }

        private void RefreshPresentationAndStats()
        {
            ApplyWeaponPresentation();
            ApplyArmorPresentation();
            statController?.RecalculateStats();
        }

        private void ApplyWeaponPresentation()
        {
            if (weaponSlot1Visual != null)
                weaponSlot1Visual.SetActive(state.ActiveWeaponSlot == WeaponSlotType.Slot1 && state.WeaponSlot1 != null);

            if (weaponSlot2Visual != null)
                weaponSlot2Visual.SetActive(state.ActiveWeaponSlot == WeaponSlotType.Slot2 && state.WeaponSlot2 != null);

            WeaponDefinitionSO active = state.GetActiveWeapon();

            if (animator != null)
            {
                animator.runtimeAnimatorController = (active != null && active.AnimatorOverride != null)
                    ? active.AnimatorOverride
                    : defaultAnimatorController;
            }

            if (active != null && active.ComboChain != null && active.ComboChain.Count > 0)
            {
                loadout = System.Linq.Enumerable.ToArray(active.ComboChain);
            }
        }

        private void ApplyArmorPresentation()
        {
            if (armorRenderer == null)
                return;

            if (state.EquippedArmor != null && state.EquippedArmor.ArmorMaterial != null)
                armorRenderer.sharedMaterial = state.EquippedArmor.ArmorMaterial;
            else if (defaultArmorMaterial != null)
                armorRenderer.sharedMaterial = defaultArmorMaterial;
        }
        private IItemOwnershipQuery ownershipQuery;

public void SetOwnershipQuery(IItemOwnershipQuery query)
{
    ownershipQuery = query;
}
    }
}