using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Equipment
{
    public class EquipmentVisualController : MonoBehaviour
    {
        [Serializable]
        public struct VisualEntry
        {
            public string visualId;
            public GameObject visualObject;
        }

        [Header("Dependencies")]
        [SerializeField] private EquipmentController equipmentController;

        [Header("Weapon Sockets (Scene-Authored)")]
        [SerializeField] private GameObject weaponSlot01Root;
        [SerializeField] private GameObject weaponSlot02Root;

        [Tooltip("Daftarkan semua child visual senjata di bawah WeaponSlot_01 beserta WeaponVisualId-nya.")]
        [SerializeField] private List<VisualEntry> slot01Visuals = new List<VisualEntry>();

        [Tooltip("Daftarkan semua child visual senjata di bawah WeaponSlot_02 beserta WeaponVisualId-nya.")]
        [SerializeField] private List<VisualEntry> slot02Visuals = new List<VisualEntry>();

        [Header("Armor Socket (Scene-Authored)")]
        [Tooltip("Renderer tubuh/armor yang material-nya diganti saat armor dipasang.")]
        [SerializeField] private Renderer[] armorRenderers;
        [SerializeField] private Material defaultArmorMaterial;
        [Tooltip("Opsional: Child mesh khusus di dalam ArmorSocket berdasarkan ArmorVisualId.")]
        [SerializeField] private List<VisualEntry> armorVisuals = new List<VisualEntry>();

        private void Awake()
        {
            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }
        }

        private void OnEnable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched += HandleWeaponSwitched;
                equipmentController.OnWeaponEquipped += HandleWeaponEquipped;
                equipmentController.OnWeaponUnequipped += HandleWeaponUnequipped;
                equipmentController.OnArmorEquipped += HandleArmorChanged;
                equipmentController.OnArmorUnequipped += HandleArmorChanged;
            }
        }

        private void OnDisable()
        {
            if (equipmentController != null)
            {
                equipmentController.OnWeaponSwitched -= HandleWeaponSwitched;
                equipmentController.OnWeaponEquipped -= HandleWeaponEquipped;
                equipmentController.OnWeaponUnequipped -= HandleWeaponUnequipped;
                equipmentController.OnArmorEquipped -= HandleArmorChanged;
                equipmentController.OnArmorUnequipped -= HandleArmorChanged;
            }
        }

        private void Start()
        {
            RefreshAllVisuals();
        }

        public void RefreshAllVisuals()
        {
            RefreshWeaponVisuals();
            RefreshArmorVisuals();
        }

        private void HandleWeaponEquipped(WeaponSlotType slot, WeaponDefinitionSO weapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleWeaponUnequipped(WeaponSlotType slot, WeaponDefinitionSO previousWeapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleWeaponSwitched(WeaponSlotType activeSlot, WeaponDefinitionSO activeWeapon)
        {
            RefreshWeaponVisuals();
        }

        private void HandleArmorChanged(ArmorDefinitionSO _)
        {
            RefreshArmorVisuals();
        }

        public void RefreshWeaponVisuals()
        {
            if (equipmentController == null)
            {
                return;
            }

            PlayerEquipmentState state = equipmentController.State;
            WeaponSlotType activeSlot = state.ActiveWeaponSlot;

            bool isSlot1Active = activeSlot == WeaponSlotType.Slot1 && state.WeaponSlot1 != null;
            bool isSlot2Active = activeSlot == WeaponSlotType.Slot2 && state.WeaponSlot2 != null;

            if (weaponSlot01Root != null)
            {
                weaponSlot01Root.SetActive(isSlot1Active);
                if (isSlot1Active)
                {
                    ActivateMatchingVisual(slot01Visuals, state.WeaponSlot1.WeaponVisualId);
                }
            }

            if (weaponSlot02Root != null)
            {
                weaponSlot02Root.SetActive(isSlot2Active);
                if (isSlot2Active)
                {
                    ActivateMatchingVisual(slot02Visuals, state.WeaponSlot2.WeaponVisualId);
                }
            }
        }

        public void RefreshArmorVisuals()
        {
            if (equipmentController == null)
            {
                return;
            }

            ArmorDefinitionSO armor = equipmentController.EquippedArmor;
            Material targetMaterial = (armor != null && armor.ArmorMaterial != null)
                ? armor.ArmorMaterial
                : defaultArmorMaterial;

            if (armorRenderers != null && targetMaterial != null)
            {
                for (int i = 0; i < armorRenderers.Length; i++)
                {
                    if (armorRenderers[i] != null)
                    {
                        armorRenderers[i].sharedMaterial = targetMaterial;
                    }
                }
            }

            string targetVisualId = armor != null ? armor.ArmorVisualId : string.Empty;
            ActivateMatchingVisual(armorVisuals, targetVisualId);
        }

        private static void ActivateMatchingVisual(List<VisualEntry> entries, string targetVisualId)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject obj = entries[i].visualObject;
                if (obj == null)
                {
                    continue;
                }

                bool shouldBeActive = !string.IsNullOrWhiteSpace(targetVisualId) &&
                                      string.Equals(entries[i].visualId, targetVisualId, StringComparison.OrdinalIgnoreCase);

                if (obj.activeSelf != shouldBeActive)
                {
                    obj.SetActive(shouldBeActive);
                }
            }
        }
    }
}