using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Items.Artifact
{
    public class ArtifactController : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [Range(1, 4)]
        [SerializeField] private int maxArtifactSlots = 2;

        [Header("Initial Loadout (Optional / Testing)")]
        [SerializeField] private List<ArtifactDefinitionSO> startingArtifacts = new List<ArtifactDefinitionSO>();

        [Header("Validation Options")]
        [SerializeField] private bool allowEquipWithoutInventoryInEditor = true;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private ArtifactDefinitionSO[] equippedArtifacts;

        private IWeaponSwitchGate _actionGate;
        private IItemOwnershipQuery _ownershipQuery;

        public event Action<int, ArtifactDefinitionSO> OnArtifactEquipped;
        public event Action<int, ArtifactDefinitionSO> OnArtifactUnequipped;
        public event Action OnArtifactsChanged;

        public int SlotCount => maxArtifactSlots;
        public IReadOnlyList<ArtifactDefinitionSO> EquippedArtifacts => equippedArtifacts;

        private void Awake()
        {
            EnsureArrayInitialized();
            _actionGate = GetComponent<IWeaponSwitchGate>();
            _ownershipQuery = GetComponent<IItemOwnershipQuery>();
        }

        private void Start()
        {
            ApplyInitialArtifacts();
        }

        private void EnsureArrayInitialized()
        {
            if (equippedArtifacts == null || equippedArtifacts.Length != maxArtifactSlots)
            {
                equippedArtifacts = new ArtifactDefinitionSO[maxArtifactSlots];
            }
        }

        public void SetActionGate(IWeaponSwitchGate actionGate)
        {
            _actionGate = actionGate;
        }

        public void SetOwnershipQuery(IItemOwnershipQuery ownershipQuery)
        {
            _ownershipQuery = ownershipQuery;
        }

        public void ApplyInitialArtifacts()
        {
            EnsureArrayInitialized();
            Array.Clear(equippedArtifacts, 0, equippedArtifacts.Length);

            if (startingArtifacts != null)
            {
                int count = Mathf.Min(startingArtifacts.Count, maxArtifactSlots);
                for (int i = 0; i < count; i++)
                {
                    ArtifactDefinitionSO artifact = startingArtifacts[i];
                    if (artifact != null && !IsArtifactEquipped(artifact))
                    {
                        equippedArtifacts[i] = artifact;
                        OnArtifactEquipped?.Invoke(i, artifact);
                    }
                }
            }

            OnArtifactsChanged?.Invoke();
        }

        public bool TryEquipArtifact(int slotIndex, ArtifactDefinitionSO artifact)
        {
            EnsureArrayInitialized();

            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning($"[ArtifactController] Index slot artefak tidak valid: {slotIndex}.", this);
                return false;
            }

            if (artifact == null)
            {
                Debug.LogWarning("[ArtifactController] Gagal equip: ArtifactDefinitionSO null.", this);
                return false;
            }

            if (!CanModifyArtifacts())
            {
                return false;
            }

            if (!VerifyOwnership(artifact))
            {
                Debug.LogWarning($"[ArtifactController] Artefak '{artifact.ItemId}' tidak dimiliki di Inventory.", this);
                return false;
            }

            // S08: Cegah artefak yang sama dipasang di dua slot berbeda
            int existingSlot = FindSlotOfArtifact(artifact);
            if (existingSlot != -1 && existingSlot != slotIndex)
            {
                Debug.LogWarning($"[ArtifactController] Artefak '{artifact.ItemId}' sudah terpasang di slot {existingSlot}.", this);
                return false;
            }

            if (equippedArtifacts[slotIndex] == artifact)
            {
                return true;
            }

            ArtifactDefinitionSO previous = equippedArtifacts[slotIndex];
            if (previous != null)
            {
                equippedArtifacts[slotIndex] = null;
                OnArtifactUnequipped?.Invoke(slotIndex, previous);
            }

            equippedArtifacts[slotIndex] = artifact;
            OnArtifactEquipped?.Invoke(slotIndex, artifact);
            OnArtifactsChanged?.Invoke();
            return true;
        }

        public bool TryEquipArtifactAuto(ArtifactDefinitionSO artifact)
        {
            if (artifact == null) return false;

            int existingSlot = FindSlotOfArtifact(artifact);
            if (existingSlot != -1)
            {
                return true;
            }

            int emptySlot = FindFirstEmptySlot();
            int targetSlot = emptySlot != -1 ? emptySlot : 0;
            return TryEquipArtifact(targetSlot, artifact);
        }

        public bool TryUnequipArtifact(int slotIndex)
        {
            EnsureArrayInitialized();

            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            ArtifactDefinitionSO current = equippedArtifacts[slotIndex];
            if (current == null)
            {
                return false;
            }

            if (!CanModifyArtifacts())
            {
                return false;
            }

            equippedArtifacts[slotIndex] = null;
            OnArtifactUnequipped?.Invoke(slotIndex, current);
            OnArtifactsChanged?.Invoke();
            return true;
        }

        public void ForceUnequipArtifact(ArtifactDefinitionSO artifact)
        {
            int slotIndex = FindSlotOfArtifact(artifact);
            if (slotIndex == -1) return;

            ArtifactDefinitionSO previous = equippedArtifacts[slotIndex];
            equippedArtifacts[slotIndex] = null;
            OnArtifactUnequipped?.Invoke(slotIndex, previous);
            OnArtifactsChanged?.Invoke();
        }

        public ArtifactDefinitionSO GetArtifactInSlot(int slotIndex)
        {
            EnsureArrayInitialized();
            return IsValidSlotIndex(slotIndex) ? equippedArtifacts[slotIndex] : null;
        }

        public IReadOnlyList<ArtifactDefinitionSO> GetActiveArtifacts()
        {
            EnsureArrayInitialized();
            List<ArtifactDefinitionSO> active = new List<ArtifactDefinitionSO>(maxArtifactSlots);
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] != null)
                {
                    active.Add(equippedArtifacts[i]);
                }
            }
            return active;
        }

        public IEnumerable<StatModifier> GetArtifactModifiers()
        {
            EnsureArrayInitialized();
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                ArtifactDefinitionSO artifact = equippedArtifacts[i];
                if (artifact == null) continue;

                foreach (StatModifier mod in artifact.GetScopedModifiers())
                {
                    yield return mod;
                }
            }
        }

        public bool IsArtifactEquipped(ArtifactDefinitionSO artifact)
        {
            return FindSlotOfArtifact(artifact) != -1;
        }

        public int FindSlotOfArtifact(ArtifactDefinitionSO artifact)
        {
            EnsureArrayInitialized();
            if (artifact == null) return -1;

            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] == artifact)
                {
                    return i;
                }
            }
            return -1;
        }

        public int FindFirstEmptySlot()
        {
            EnsureArrayInitialized();
            for (int i = 0; i < equippedArtifacts.Length; i++)
            {
                if (equippedArtifacts[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < maxArtifactSlots;
        }

        private bool CanModifyArtifacts()
        {
            if (_actionGate == null) return true;
            return _actionGate.CanChangeEquipment() && _actionGate.CanSwitchWeapon();
        }

        private bool VerifyOwnership(ItemDefinitionSO item)
        {
            if (_ownershipQuery != null)
            {
                return _ownershipQuery.HasItem(item);
            }

            return allowEquipWithoutInventoryInEditor;
        }
        // Tambahkan di dalam class ArtifactController:
        public void RestoreArtifactState(IReadOnlyList<ArtifactDefinitionSO> artifacts)
        {
            EnsureArrayInitialized();
            Array.Clear(equippedArtifacts, 0, equippedArtifacts.Length);

            if (artifacts != null)
            {
                int count = Mathf.Min(artifacts.Count, maxArtifactSlots);
                for (int i = 0; i < count; i++)
                {
                    ArtifactDefinitionSO artifact = artifacts[i];
                    if (artifact != null && !IsArtifactEquipped(artifact))
                    {
                        equippedArtifacts[i] = artifact;
                        OnArtifactEquipped?.Invoke(i, artifact);
                    }
                }
            }

            OnArtifactsChanged?.Invoke();
        }
    }
}