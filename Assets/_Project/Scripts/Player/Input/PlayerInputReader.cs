using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool ParryPressed { get; private set; }
    public bool DodgePressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool Enabled = true;

    InputActionMap gameplay;
    InputAction moveAction;
    InputAction attackAction;
    InputAction parryAction;
    InputAction dodgeAction;
    InputAction interactAction;
    InputAction lookAction;

    void Awake()
    {
        gameplay = new InputActionMap("Gameplay");
        moveAction = gameplay.AddAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = gameplay.AddAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Gamepad>/rightStick");
        lookAction.AddBinding("<Mouse>/delta");

        attackAction = gameplay.AddAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Keyboard>/j");
        attackAction.AddBinding("<Gamepad>/buttonWest");

        parryAction = gameplay.AddAction("Parry", InputActionType.Button);
        parryAction.AddBinding("<Mouse>/rightButton");
        parryAction.AddBinding("<Keyboard>/k");
        parryAction.AddBinding("<Gamepad>/buttonNorth");

        dodgeAction = gameplay.AddAction("Dodge", InputActionType.Button);
        dodgeAction.AddBinding("<Keyboard>/space");
        dodgeAction.AddBinding("<Keyboard>/leftShift");
        dodgeAction.AddBinding("<Gamepad>/buttonEast");
        dodgeAction.AddBinding("<Gamepad>/rightShoulder");

        interactAction = gameplay.AddAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable() => gameplay?.Enable();
    void OnDisable() => gameplay?.Disable();

    void OnDestroy() => gameplay?.Dispose();

    void Update()
    {
        AttackPressed = false;
        ParryPressed = false;
        DodgePressed = false;
        InteractPressed = false;

        if (!Enabled || gameplay == null)
        {
            Move = Vector2.zero;
            return;
        }

        Vector2 value = moveAction.ReadValue<Vector2>();
        Move = value.sqrMagnitude > 1f ? value.normalized : value;
        AttackPressed = attackAction.WasPressedThisFrame();
        ParryPressed = parryAction.WasPressedThisFrame();
        DodgePressed = dodgeAction.WasPressedThisFrame();
        InteractPressed = interactAction.WasPressedThisFrame();
    }

    public void ConsumeAttack() => AttackPressed = false;
    public void ConsumeParry() => ParryPressed = false;
    public void ConsumeDodge() => DodgePressed = false;
}
