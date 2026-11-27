using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Inventory;

namespace TheLastSasak.Items.Equipment
{
    public class EquipmentTransactionHandler : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Inventory.Inventory inventory;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private TheLastSasak.Items.Artifact.ArtifactController artifactController;

        private void Awake()
        {
            if (artifactController == null) artifactController = GetComponent<TheLastSasak.Items.Artifact.ArtifactController>();
            if (artifactController != null && inventory != null) artifactController.SetOwnershipQuery(inventory);
            if (inventory == null)
            {
                inventory = GetComponent<Inventory.Inventory>();
            }

            if (equipmentController == null)
            {
                equipmentController = GetComponent<EquipmentController>();
            }

            if (equipmentController != null && inventory != null)
            {
                equipmentController.SetOwnershipQuery(inventory);
            }

        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnItemRemoved += HandleInventoryItemRemoved;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnItemRemoved -= HandleInventoryItemRemoved;
            }
        }

        /// <summary>
        /// Titik masuk utama dari UI atau shortcut untuk memasang item dari Inventory.
        /// Jika preferredWeaponSlot adalah None, sistem memilih slot kosong atau slot aktif.
        /// </summary>
        public bool TryEquipFromInventory(ItemDefinitionSO item, WeaponSlotType preferredWeaponSlot = WeaponSlotType.None)
        {
            // 1. Validasi referensi item
            if (item == null || equipmentController == null)
            {
                return false;
            }

            // 2. Check Ownership
            if (inventory != null && !inventory.HasItem(item))
            {
                Debug.LogWarning($"[EquipmentTransaction] Gagal equip: '{item.ItemId}' tidak ada di Inventory.", this);
                return false;
            }

            // 3. Check Item Category & 4. Validate Equipment Slot
            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    if (item is not WeaponDefinitionSO weapon)
                    {
                        return false;
                    }

                    WeaponSlotType targetSlot = ResolveTargetWeaponSlot(weapon, preferredWeaponSlot);
                    return equipmentController.TryEquipWeapon(targetSlot, weapon);

                case ItemCategory.Armor:
                    if (item is not ArmorDefinitionSO armor)
                    {
                        return false;
                    }

                    return equipmentController.TryEquipArmor(armor);

                case ItemCategory.Artifact:
                        if (item is not ArtifactDefinitionSO artifact || artifactController == null)
                        {
                            return false;
                        }
                        return artifactController.TryEquipArtifactAuto(artifact);

                    Debug.Log($"[EquipmentTransaction] '{item.ItemId}' adalah Artifact; gunakan ArtifactController.", this);
                    return false;

                default:
                    Debug.LogWarning($"[EquipmentTransaction] Kategori '{item.Category}' tidak dapat dipasang sebagai equipment.", this);
                    return false;
            }
        }

        public bool TryUnequipWeaponSlot(WeaponSlotType slot)
        {
            if (equipmentController == null) return false;
            return equipmentController.TryUnequipWeapon(slot);
        }

        public bool TryUnequipArmor()
        {
            if (equipmentController == null) return false;
            return equipmentController.TryUnequipArmor();
        }

        public bool IsItemEquipped(ItemDefinitionSO item)
        {
            if (item == null || equipmentController == null) return false;

            if (item is WeaponDefinitionSO weapon)
            {
                return equipmentController.State.IsWeaponEquipped(weapon);
            }

            if (item is ArmorDefinitionSO armor)
            {
                return equipmentController.EquippedArmor == armor;
            }

            return false;
        }

        private WeaponSlotType ResolveTargetWeaponSlot(WeaponDefinitionSO weapon, WeaponSlotType preferredSlot)
        {
            PlayerEquipmentState state = equipmentController.State;

            // Jika sudah terpasang di salah satu slot, pertahankan slot tersebut
            WeaponSlotType existingSlot = state.FindSlotOfWeapon(weapon);
            if (existingSlot != WeaponSlotType.None)
            {
                return existingSlot;
            }

            if (preferredSlot == WeaponSlotType.Slot1 || preferredSlot == WeaponSlotType.Slot2)
            {
                return preferredSlot;
            }

            // Otomatis pilih slot kosong terlebih dahulu
            if (state.WeaponSlot1 == null) return WeaponSlotType.Slot1;
            if (state.WeaponSlot2 == null) return WeaponSlotType.Slot2;

            // Jika kedua slot terisi, timpa slot yang sedang aktif
            return state.ActiveWeaponSlot != WeaponSlotType.None
                ? state.ActiveWeaponSlot
                : WeaponSlotType.Slot1;
        }

        /// <summary>
        /// Menjamin equipment otomatis dilepas apabila item dibuang/hilang dari Inventory.
        /// </summary>
        private void HandleInventoryItemRemoved(ItemDefinitionSO removedItem, int _)
        {
            if (removedItem == null || equipmentController == null || inventory == null)
            {
                return;
            }

            if (inventory.HasItem(removedItem))
            {
                return;
            }

            if (removedItem is WeaponDefinitionSO weapon)
            {
                WeaponSlotType equippedSlot = equipmentController.State.FindSlotOfWeapon(weapon);
                if (equippedSlot != WeaponSlotType.None)
                {
                    equipmentController.ForceUnequipWeapon(equippedSlot);
                }
            }
            else if (removedItem is ArmorDefinitionSO armor && equipmentController.EquippedArmor == armor)
            {
                equipmentController.ForceUnequipArmor();
            }
        }
    }
}