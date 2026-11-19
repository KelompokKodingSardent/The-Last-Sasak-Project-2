using UnityEngine;

public class ItemSwitching : MonoBehaviour
{
    public AttackDefinitionSO[] Attacks;
    public int Index;
    AttackController attack;

    void Awake()
    {
        attack = GetComponent<AttackController>();
        if (Attacks == null || Attacks.Length == 0)
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
        KeyboardSwap();
    }

    void KeyboardSwap()
    {
        if (Attacks == null || Attacks.Length == 0)
            return;

        UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null)
            return;

        if (kb.digit1Key.wasPressedThisFrame)
            Equip(0);
        if (kb.digit2Key.wasPressedThisFrame)
            Equip(1);
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
