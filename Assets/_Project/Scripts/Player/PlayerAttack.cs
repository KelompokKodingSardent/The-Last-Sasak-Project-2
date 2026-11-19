using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    AttackController attack;

    void Awake()
    {
        attack = GetComponent<AttackController>();
    }

    void Update()
    {
        if (attack != null)
            return;

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("PlayerAttack legacy: tambahkan AttackController untuk combat baru.");
        }
    }
}
