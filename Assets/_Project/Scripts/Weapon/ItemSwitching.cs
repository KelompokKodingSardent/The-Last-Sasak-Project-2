// ItemSwitching.cs
using UnityEngine;

public class ItemSwitching : MonoBehaviour
{
    public AttackDefinitionSO[] Attacks;
    public int Index;

    AttackController attack;
    EquipmentController equipment;
    PlayerInputReader input;
    PlayerActionController actions;

    void Awake()
    {
        attack = GetComponent<AttackController>();
        equipment = GetComponent<EquipmentController>();
        input = GetComponent<PlayerInputReader>();
        actions = GetComponent<PlayerActionController>();

        if ((equipment == null || equipment.ActiveWeapon == null) && (Attacks == null || Attacks.Length == 0))
        {
            Attacks = new[]
            {
                AttackDefinitionSO.CreateRuntime("Slash", 20f, 1.4f),
                AttackDefinitionSO.CreateRuntime("Heavy", 32f, 1.6f)
            };
            Equip(0);
        }
    }

    void Update()
    {
        if (input == null || input.WeaponSlotPressed < 0)
            return;

        if (actions != null && !actions.CanSwitchWeapon())
            return;

        int slot = input.WeaponSlotPressed;

        if (equipment != null && (equipment.WeaponSlot1 != null || equipment.WeaponSlot2 != null))
        {
            if (equipment.SwitchWeaponSlot(slot))
                Index = slot;
            return;
        }

        Equip(slot);
    }

    public void Equip(int index)
    {
        if (Attacks == null || index < 0 || index >= Attacks.Length)
            return;
        Index = index;
        if (attack != null)
            attack.Definition = Attacks[index];
    }
}