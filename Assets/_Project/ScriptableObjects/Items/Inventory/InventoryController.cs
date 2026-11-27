using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Inventory
{
    public class InventoryController
    {
        private readonly InventoryModel _model;

        // Events (Point 3.4)
        public event Action<ItemDefinitionSO, int> OnItemAdded;
        public event Action<ItemDefinitionSO, int> OnItemRemoved;
        public event Action OnInventoryChanged;

        public InventoryModel Model => _model;
        public int Capacity => _model.Capacity;

        public InventoryController(int capacity = 20, bool allowDuplicateEquipment = false)
        {
            _model = new InventoryModel(capacity, allowDuplicateEquipment);
        }

        public InventoryController(InventoryModel existingModel)
        {
            _model = existingModel ?? new InventoryModel(20, false);
        }

        public bool CanAddItem(ItemDefinitionSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            return _model.GetMaxAddableAmount(item) >= amount;
        }

        /// <summary>
        /// Menambahkan item ke inventory. Mengembalikan true jika minimal 1 item berhasil masuk.
        /// Gunakan parameter out amountAdded untuk mengetahui sisa yang tidak muat (partial add).
        /// </summary>
        public bool TryAddItem(ItemDefinitionSO item, int amount, out int amountAdded)
        {
            amountAdded = 0;

            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || amount <= 0)
            {
                return false;
            }

            int maxAddable = _model.GetMaxAddableAmount(item);
            if (maxAddable <= 0)
            {
                return false;
            }

            int remainingToAdd = Mathf.Min(amount, maxAddable);
            IReadOnlyList<InventorySlot> slots = _model.Slots;

            // Langkah 1: Isi slot yang sudah memiliki item sejenis dan belum penuh
            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Count && remainingToAdd > 0; i++)
                {
                    InventorySlot slot = slots[i];
                    if (!slot.IsEmpty && slot.ItemDefinition == item && !slot.IsFull)
                    {
                        int added = slot.AddQuantity(remainingToAdd);
                        amountAdded += added;
                        remainingToAdd -= added;
                    }
                }
            }

            // Langkah 2: Alokasikan sisa jumlah ke slot kosong
            for (int i = 0; i < slots.Count && remainingToAdd > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int stackAmount = Mathf.Min(remainingToAdd, item.MaxStackSize);
                    slot.Set(item, stackAmount);
                    amountAdded += stackAmount;
                    remainingToAdd -= stackAmount;
                }
            }

            if (amountAdded > 0)
            {
                OnItemAdded?.Invoke(item, amountAdded);
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAddItem(ItemDefinitionSO item, int amount = 1)
        {
            return TryAddItem(item, amount, out _);
        }

        public bool TryRemoveItem(ItemDefinitionSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            // Transaksi atomik: tolak jika stok tidak mencukupi jumlah yang diminta
            if (_model.GetItemCount(item) < amount)
            {
                return false;
            }

            int remainingToRemove = amount;
            IReadOnlyList<InventorySlot> slots = _model.Slots;

            // Kurangi dari slot paling akhir agar tumpukan parsial habis lebih dulu
            for (int i = slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.ItemDefinition == item)
                {
                    int removed = slot.RemoveQuantity(remainingToRemove);
                    remainingToRemove -= removed;
                }
            }

            OnItemRemoved?.Invoke(item, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveItemById(string itemId, int amount = 1)
        {
            InventorySlot slot = _model.FindSlot(itemId);
            if (slot == null || slot.IsEmpty)
            {
                return false;
            }

            return TryRemoveItem(slot.ItemDefinition, amount);
        }

        public InventorySlot FindItem(string itemId)
        {
            return _model.FindSlot(itemId);
        }

        public InventorySlot FindItem(ItemDefinitionSO item)
        {
            return _model.FindSlot(item);
        }

        public bool HasItem(ItemDefinitionSO item, int minQuantity = 1)
        {
            return _model.HasItem(item, minQuantity);
        }

        public bool HasItem(string itemId, int minQuantity = 1)
        {
            return _model.HasItem(itemId, minQuantity);
        }

        public int GetItemCount(ItemDefinitionSO item)
        {
            return _model.GetItemCount(item);
        }

        public int GetItemCount(string itemId)
        {
            return _model.GetItemCount(itemId);
        }

        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _model.Slots;
        }

        public void ClearInventory()
        {
            _model.ClearAll();
            OnInventoryChanged?.Invoke();
        }
                // Tambahkan di dalam class InventoryController:
        public void RestoreFromSaveEntries(
            IReadOnlyList<(int slotIndex, ItemDefinitionSO item, int quantity)> entries,
            bool allowDuplicateEquipmentOverride)
        {
            _model.ClearAll();

            if (entries != null)
            {
                IReadOnlyList<InventorySlot> slots = _model.Slots;
                for (int i = 0; i < entries.Count; i++)
                {
                    var (slotIndex, item, quantity) = entries[i];
                    if (item == null || quantity <= 0) continue;

                    if (!allowDuplicateEquipmentOverride && item is EquipmentDefinitionSO && _model.HasItem(item))
                    {
                        continue;
                    }

                    if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].IsEmpty)
                    {
                        slots[slotIndex].Set(item, quantity);
                    }
                    else
                    {
                        TryAddItem(item, quantity);
                    }
                }
            }

            OnInventoryChanged?.Invoke();
        }
    }
}