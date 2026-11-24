using UnityEngine;
using UnityEngine.InputSystem;

public enum BufferedActionType
{
    None,
    Attack,
    Parry,
    Dodge
}

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Buffer Settings")]
    [SerializeField] float bufferDuration = 0.16f;

    public Vector2 Move { get; private set; }
    public Vector2 AimScreenPosition { get; private set; }
    public Vector2 AimStick { get; private set; }
    public bool UsingGamepadAim { get; private set; }

    public bool AttackPressed { get; private set; }
    public bool ParryPressed { get; private set; }
    public bool DodgePressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public int WeaponSlotPressed { get; private set; } = -1;
    public bool Enabled = true;

    public BufferedActionType BufferedAction { get; private set; } = BufferedActionType.None;
    float bufferExpireTime;

    InputActionMap gameplay;
    InputAction moveAction;
    InputAction aimStickAction;
    InputAction mousePosAction;
    InputAction attackAction;
    InputAction parryAction;
    InputAction dodgeAction;
    InputAction interactAction;
    InputAction weapon1Action;
    InputAction weapon2Action;

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

        aimStickAction = gameplay.AddAction("AimStick", InputActionType.Value);
        aimStickAction.AddBinding("<Gamepad>/rightStick");

        mousePosAction = gameplay.AddAction("MousePosition", InputActionType.Value);
        mousePosAction.AddBinding("<Mouse>/position");

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

        weapon1Action = gameplay.AddAction("Weapon1", InputActionType.Button);
        weapon1Action.AddBinding("<Keyboard>/1");
        weapon1Action.AddBinding("<Gamepad>/dpad/left");

        weapon2Action = gameplay.AddAction("Weapon2", InputActionType.Button);
        weapon2Action.AddBinding("<Keyboard>/2");
        weapon2Action.AddBinding("<Gamepad>/dpad/right");
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
        WeaponSlotPressed = -1;

        if (!Enabled || gameplay == null)
        {
            Move = Vector2.zero;
            AimStick = Vector2.zero;
            ClearBuffer();
            return;
        }

        Vector2 value = moveAction.ReadValue<Vector2>();
        Move = value.sqrMagnitude > 1f ? value.normalized : value;

        Vector2 stick = aimStickAction.ReadValue<Vector2>();
        AimStick = stick;
        if (stick.sqrMagnitude > 0.04f)
            UsingGamepadAim = true;
        else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f)
            UsingGamepadAim = false;

        AimScreenPosition = mousePosAction.ReadValue<Vector2>();

        AttackPressed = attackAction.WasPressedThisFrame();
        ParryPressed = parryAction.WasPressedThisFrame();
        DodgePressed = dodgeAction.WasPressedThisFrame();
        InteractPressed = interactAction.WasPressedThisFrame();

        if (weapon1Action.WasPressedThisFrame())
            WeaponSlotPressed = 0;
        else if (weapon2Action.WasPressedThisFrame())
            WeaponSlotPressed = 1;

        if (DodgePressed)
            BufferAction(BufferedActionType.Dodge);
        else if (ParryPressed)
            BufferAction(BufferedActionType.Parry);
        else if (AttackPressed)
            BufferAction(BufferedActionType.Attack);

        if (BufferedAction != BufferedActionType.None && Time.time > bufferExpireTime)
            ClearBuffer();
    }

    public void BufferAction(BufferedActionType action)
    {
        if (action == BufferedActionType.None)
            return;
        BufferedAction = action;
        bufferExpireTime = Time.time + bufferDuration;
    }

    public bool HasBufferedAction(BufferedActionType action)
    {
        return BufferedAction == action && Time.time <= bufferExpireTime;
    }

    public bool TryConsumeBufferedAction(BufferedActionType action)
    {
        if (!HasBufferedAction(action))
            return false;
        ClearBuffer();
        return true;
    }

    public void ClearBuffer()
    {
        BufferedAction = BufferedActionType.None;
        bufferExpireTime = 0f;
    }

    public void ConsumeAttack()
    {
        AttackPressed = false;
        if (BufferedAction == BufferedActionType.Attack)
            ClearBuffer();
    }

    public void ConsumeParry()
    {
        ParryPressed = false;
        if (BufferedAction == BufferedActionType.Parry)
            ClearBuffer();
    }

    public void ConsumeDodge()
    {
        DodgePressed = false;
        if (BufferedAction == BufferedActionType.Dodge)
            ClearBuffer();
    }
}