using System;
using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    [Header("Weapon Slots (Slot 1 & 2)")]
    public WeaponDefinitionSO WeaponSlot1;
    public WeaponDefinitionSO WeaponSlot2;
    [SerializeField] int activeWeaponSlot = 0;

    [Header("Armor Slot")]
    public ArmorDefinitionSO EquippedArmor;

    [Header("Visual Sockets")]
    [SerializeField] Transform weaponSocket;
    [SerializeField] Renderer armorRenderer;

    [Header("Legacy Fallback")]
    public AttackDefinitionSO[] Loadout;

    public WeaponDefinitionSO ActiveWeapon => activeWeaponSlot == 0 ? WeaponSlot1 : WeaponSlot2;
    public int ActiveWeaponSlot => activeWeaponSlot;

    public event Action<WeaponDefinitionSO> OnWeaponEquipped;
    public event Action<ArmorDefinitionSO> OnArmorEquipped;

    ItemSwitching switching;
    PlayerStatController statController;
    Animator animator;
    RuntimeAnimatorController defaultAnimatorController;
    GameObject spawnedWeaponVisual;

    void Awake()
    {
        switching = GetComponent<ItemSwitching>();
        statController = GetComponent<PlayerStatController>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            defaultAnimatorController = animator.runtimeAnimatorController;
    }

    void Start()
    {
        ApplyActiveWeaponVisualsAndCombo();
        ApplyArmorVisuals();
    }

    public bool SwitchWeaponSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 1 || slotIndex == activeWeaponSlot)
            return false;

        WeaponDefinitionSO target = slotIndex == 0 ? WeaponSlot1 : WeaponSlot2;
        if (target == null)
            return false;

        activeWeaponSlot = slotIndex;
        ApplyActiveWeaponVisualsAndCombo();
        statController?.RecalculateStats(resetHealthToFull: false);
        OnWeaponEquipped?.Invoke(target);
        return true;
    }

    public void EquipWeapon(WeaponDefinitionSO weapon, int slotIndex = -1)
    {
        int targetSlot = slotIndex < 0 ? activeWeaponSlot : Mathf.Clamp(slotIndex, 0, 1);
        if (targetSlot == 0) WeaponSlot1 = weapon;
        else WeaponSlot2 = weapon;

        if (targetSlot == activeWeaponSlot)
        {
            ApplyActiveWeaponVisualsAndCombo();
            statController?.RecalculateStats(resetHealthToFull: false);
            OnWeaponEquipped?.Invoke(weapon);
        }
    }

    public void EquipArmor(ArmorDefinitionSO armor)
    {
        EquippedArmor = armor;
        ApplyArmorVisuals();
        statController?.RecalculateStats(resetHealthToFull: false);
        OnArmorEquipped?.Invoke(armor);
    }

    public void SetLoadout(AttackDefinitionSO[] attacks)
    {
        Loadout = attacks;
        if (switching != null)
            switching.Attacks = attacks;
    }

    void ApplyActiveWeaponVisualsAndCombo()
    {
        WeaponDefinitionSO weapon = ActiveWeapon;

        if (spawnedWeaponVisual != null)
            Destroy(spawnedWeaponVisual);

        if (weapon != null)
        {
            if (weapon.WeaponPrefab != null && weaponSocket != null)
            {
                spawnedWeaponVisual = Instantiate(weapon.WeaponPrefab, weaponSocket);
                spawnedWeaponVisual.transform.localPosition = Vector3.zero;
                spawnedWeaponVisual.transform.localRotation = Quaternion.identity;
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = weapon.AnimatorOverride != null
                    ? weapon.AnimatorOverride
                    : defaultAnimatorController;
            }

            if (weapon.ComboChain != null && weapon.ComboChain.Length > 0)
            {
                SetLoadout(weapon.ComboChain);
            }
        }
    }

    void ApplyArmorVisuals()
    {
        if (EquippedArmor != null && armorRenderer != null && EquippedArmor.ArmorMaterial != null)
            armorRenderer.sharedMaterial = EquippedArmor.ArmorMaterial;
    }
}