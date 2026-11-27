// Tambahkan di dalam class EquipmentController (Assets/_Project/Scripts/Items/Equipment/EquipmentController.cs)
public void ForceUnequipWeapon(WeaponSlotType slot)

{
    if (slot != WeaponSlotType.Slot1 && slot != WeaponSlotType.Slot2) return;

    WeaponDefinitionSO currentWeapon = state.GetWeapon(slot);
    if (currentWeapon == null) return;

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

    OnEquipmentChanged?.Invoke();
}

public void ForceUnequipArmor()
{
    ArmorDefinitionSO previousArmor = state.EquippedArmor;
    if (previousArmor == null) return;

    state.SetArmor(null);
    OnArmorUnequipped?.Invoke(previousArmor);
    OnEquipmentChanged?.Invoke();
}

// Tambahkan di dalam class EquipmentController:
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
    {
        state.SetActiveWeaponSlot(activeSlot);
    }
    else if (state.WeaponSlot1 != null)
    {
        state.SetActiveWeaponSlot(WeaponSlotType.Slot1);
    }
    else if (state.WeaponSlot2 != null)
    {
        state.SetActiveWeaponSlot(WeaponSlotType.Slot2);
    }
    else
    {
        state.SetActiveWeaponSlot(WeaponSlotType.None);
    }

    OnWeaponSwitched?.Invoke(state.ActiveWeaponSlot, state.GetActiveWeapon());
    OnEquipmentChanged?.Invoke();
}