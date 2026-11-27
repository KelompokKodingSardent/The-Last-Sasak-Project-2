using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Catalog;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Inventory;
using TheLastSasak.Player.Stats;

namespace TheLastSasak.Systems.Save
{
    public class ItemPersistenceCoordinator : MonoBehaviour
    {
        [Header("Catalog Reference (Required)")]
        [SerializeField] private ItemCatalogSO itemCatalog;

        [Header("Domain Controllers")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private EquipmentController equipmentController;
        [SerializeField] private ArtifactController artifactController;
        [SerializeField] private PlayerStatController playerStatController;

        [Header("Persistence Policy (Point 10.4)")]
        [Tooltip("Aktifkan jika koleksi Inventory & Equipment bertahan antar sesi/run.")]
        [SerializeField] private bool persistInventoryAndEquipment = true;

        public bool PersistInventoryAndEquipment
        {
            get => persistInventoryAndEquipment;
            set => persistInventoryAndEquipment = value;
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<Inventory>();
            if (equipmentController == null) equipmentController = GetComponent<EquipmentController>();
            if (artifactController == null) artifactController = GetComponent<ArtifactController>();
            if (playerStatController == null) playerStatController = GetComponent<PlayerStatController>();
        }

        public void CaptureToSaveData(TLS_SaveData saveData)
        {
            if (saveData == null) return;
            saveData.EnsureValidStructure();

            if (!persistInventoryAndEquipment)
            {
                saveData.InventorySaveData.Items.Clear();
                saveData.EquipmentSaveData = new EquipmentSaveData();
                return;
            }

            CaptureInventory(saveData.InventorySaveData);
            CaptureEquipmentAndArtifacts(saveData.EquipmentSaveData);
        }

        public void ApplyFromSaveData(TLS_SaveData saveData)
        {
            if (saveData == null) return;
            saveData.EnsureValidStructure();

            if (itemCatalog == null)
            {
                Debug.LogError("[ItemPersistenceCoordinator] ItemCatalogSO belum dipasang! Pemulihan item dibatalkan.", this);
                return;
            }

            if (!persistInventoryAndEquipment)
            {
                return;
            }

            // Langkah 4, 5 & 6: Resolve ItemId via ItemCatalogSO & pulihkan Inventory
            RestoreInventory(saveData.InventorySaveData);

            // Langkah 7 & 8: Pulihkan Equipment & Artifacts dengan validasi kepemilikan Inventory
            RestoreEquipmentAndArtifacts(saveData.EquipmentSaveData);

            // Langkah 9: Hitung ulang FinalStats dari konfigurasi & modifier aktif (tidak pernah dari JSON)
            if (playerStatController != null)
            {
                playerStatController.RecalculateStats();
            }
        }

        private void CaptureInventory(InventorySaveData invSave)
        {
            invSave.Items.Clear();
            if (inventory == null) return;

            invSave.Capacity = inventory.Capacity;
            IReadOnlyList<InventorySlot> slots = inventory.GetSlots();

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && !slot.IsEmpty && slot.ItemDefinition != null)
                {
                    invSave.Items.Add(new InventorySlotSaveEntry(
                        slot.SlotIndex,
                        slot.ItemDefinition.ItemId,
                        slot.Quantity));
                }
            }
        }

        private void CaptureEquipmentAndArtifacts(EquipmentSaveData eqSave)
        {
            eqSave.WeaponSlot1Id = string.Empty;
            eqSave.WeaponSlot2Id = string.Empty;
            eqSave.ActiveWeaponSlot = 0;
            eqSave.ArmorId = string.Empty;
            eqSave.Artifacts.ArtifactIds.Clear();

            if (equipmentController != null)
            {
                PlayerEquipmentState state = equipmentController.State;
                eqSave.WeaponSlot1Id = state.WeaponSlot1 != null ? state.WeaponSlot1.ItemId : string.Empty;
                eqSave.WeaponSlot2Id = state.WeaponSlot2 != null ? state.WeaponSlot2.ItemId : string.Empty;
                eqSave.ActiveWeaponSlot = (int)state.ActiveWeaponSlot;
                eqSave.ArmorId = state.EquippedArmor != null ? state.EquippedArmor.ItemId : string.Empty;
            }

            if (artifactController != null)
            {
                eqSave.Artifacts.MaxSlots = artifactController.SlotCount;
                for (int i = 0; i < artifactController.SlotCount; i++)
                {
                    ArtifactDefinitionSO art = artifactController.GetArtifactInSlot(i);
                    eqSave.Artifacts.ArtifactIds.Add(art != null ? art.ItemId : string.Empty);
                }
            }
        }

        private void RestoreInventory(InventorySaveData invSave)
        {
            if (inventory == null) return;

            var resolvedEntries = new List<(int slotIndex, ItemDefinitionSO item, int quantity)>(invSave.Items.Count);

            for (int i = 0; i < invSave.Items.Count; i++)
            {
                InventorySlotSaveEntry entry = invSave.Items[i];
                if (string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0)
                {
                    continue;
                }

                // Langkah 4 & 5: Validasi ID di ItemCatalogSO; laporkan & lewati jika tidak ditemukan (P05)
                if (!itemCatalog.TryGetItem(entry.ItemId, out ItemDefinitionSO definition))
                {
                    Debug.LogWarning($"[ItemPersistence] ItemId tidak ditemukan di catalog dan dilewati: '{entry.ItemId}'.", this);
                    continue;
                }

                int clampedQuantity = Mathf.Clamp(entry.Quantity, 1, definition.MaxStackSize);
                resolvedEntries.Add((entry.SlotIndex, definition, clampedQuantity));
            }

            inventory.Controller.RestoreFromSaveEntries(
                resolvedEntries,
                inventory.Controller.Model.AllowDuplicateEquipment);
        }

        private void RestoreEquipmentAndArtifacts(EquipmentSaveData eqSave)
        {
            if (equipmentController != null)
            {
                WeaponDefinitionSO slot1 = ResolveOwnedEquipment<WeaponDefinitionSO>(eqSave.WeaponSlot1Id, "WeaponSlot1");
                WeaponDefinitionSO slot2 = ResolveOwnedEquipment<WeaponDefinitionSO>(eqSave.WeaponSlot2Id, "WeaponSlot2");
                ArmorDefinitionSO armor = ResolveOwnedEquipment<ArmorDefinitionSO>(eqSave.ArmorId, "ArmorSlot");

                WeaponSlotType activeSlot = eqSave.ActiveWeaponSlot switch
                {
                    1 => WeaponSlotType.Slot1,
                    2 => WeaponSlotType.Slot2,
                    _ => WeaponSlotType.None
                };

                equipmentController.RestoreEquipmentState(slot1, slot2, activeSlot, armor);
            }

            if (artifactController != null && eqSave.Artifacts != null)
            {
                List<string> savedIds = eqSave.Artifacts.ArtifactIds;
                ArtifactDefinitionSO[] resolvedArtifacts = new ArtifactDefinitionSO[savedIds.Count];

                for (int i = 0; i < savedIds.Count; i++)
                {
                    resolvedArtifacts[i] = ResolveOwnedEquipment<ArtifactDefinitionSO>(savedIds[i], $"ArtifactSlot[{i}]");
                }

                artifactController.RestoreArtifactState(resolvedArtifacts);
            }
        }

        private T ResolveOwnedEquipment<T>(string itemId, string slotLabel) where T : EquipmentDefinitionSO
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            if (!itemCatalog.TryGetItem<T>(itemId, out T definition))
            {
                Debug.LogWarning($"[ItemPersistence] Referensi {slotLabel} ('{itemId}') tidak valid atau tidak ada di catalog.", this);
                return null;
            }

            // Langkah 8: Validasi item yang terpasang benar-benar dimiliki di Inventory
            if (inventory != null && !inventory.HasItem(definition))
            {
                Debug.LogWarning($"[ItemPersistence] Item '{itemId}' pada {slotLabel} tidak dimiliki di Inventory; dilepas otomatis.", this);
                return null;
            }

            return definition;
        }
    }
}