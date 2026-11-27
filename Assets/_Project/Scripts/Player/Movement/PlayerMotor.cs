using UnityEngine;
using TheLastSasak.Player.Stats;
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    public PlayerMovementConfig Config;
    public bool CanMove = true;

    CharacterController controller;
    PlayerInputReader input;
    PlayerFacingController facing;
    PlayerActionController actions;
    PlayerStatController stats;

    float velocityY;
    Vector3 dashVelocity;
    float dashTime;
    Vector3 impulseVelocity;
    float impulseTime;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public Vector3 PlanarVelocity { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        facing = GetComponent<PlayerFacingController>();
        actions = GetComponent<PlayerActionController>();
        stats = GetComponent<PlayerStatController>();

        if (Config == null)
            Config = PlayerMovementConfig.CreateRuntime();
        ApplyControllerSettings();
    }

    public void ApplyControllerSettings()
    {
        if (controller == null || Config == null)
            return;

        controller.stepOffset = Config.StepOffset;
        controller.slopeLimit = Config.SlopeLimit;
        controller.minMoveDistance = 0f;
    }

    public void AddDash(Vector3 worldDelta, float duration)
    {
        duration = Mathf.Max(0.05f, duration);
        dashVelocity = worldDelta / duration;
        dashTime = duration;
    }

    public void ApplyImpulse(Vector3 worldVelocity, float duration)
    {
        duration = Mathf.Max(0.02f, duration);
        impulseVelocity = worldVelocity;
        impulseTime = duration;
    }

    void Update()
    {
        if (controller == null)
            return;

        Vector3 motion = Vector3.zero;

        if (dashTime > 0f)
        {
            motion += dashVelocity * Time.deltaTime;
            dashTime -= Time.deltaTime;
            if (dashTime <= 0f)
                dashVelocity = Vector3.zero;
        }
        else
        {
            if (impulseTime > 0f)
            {
                motion += impulseVelocity * Time.deltaTime;
                impulseTime -= Time.deltaTime;
                if (impulseTime <= 0f)
                    impulseVelocity = Vector3.zero;
            }

            if (CanMove && input != null && input.Move.sqrMagnitude > 0.01f)
            {
                Vector3 move = facing != null
                    ? facing.GetCameraRelativeMove(input.Move)
                    : new Vector3(input.Move.x, 0f, input.Move.y);

                float baseSpeed = stats != null ? stats.FinalMoveSpeed : (Config != null ? Config.MoveSpeed : 8f);
                float stateMultiplier = GetActionSpeedMultiplier();

                PlanarVelocity = move * (baseSpeed * stateMultiplier);
                motion += PlanarVelocity * Time.deltaTime;
                TryAutoJump(move);
            }
            else
            {
                PlanarVelocity = Vector3.zero;
            }
        }

        float gravity = Config != null ? Config.Gravity : -20f;
        if (controller.isGrounded && velocityY < 0f)
            velocityY = -2f;
        velocityY += gravity * Time.deltaTime;
        motion += Vector3.up * velocityY * Time.deltaTime;

        controller.Move(motion);
    }

    float GetActionSpeedMultiplier()
    {
        if (actions == null || Config == null)
            return 1f;

        switch (actions.State)
        {
            case PlayerActionState.Attacking:
                return Config.AttackMoveSpeedMultiplier;
            case PlayerActionState.Parrying:
                return Config.ParryMoveSpeedMultiplier;
            default:
                return 1f;
        }
    }

    void TryAutoJump(Vector3 moveDir)
    {
        if (Config == null || !controller.isGrounded)
            return;

        Vector3 origin = transform.position + Vector3.up * 0.2f;
        int mask = GameplayLayers.Mask(GameplayLayers.Environment);
        if (mask == 0)
            mask = Physics.DefaultRaycastLayers;
        if (!Physics.Raycast(origin, moveDir, out RaycastHit hit, 0.7f, mask))
            return;

        float height = hit.collider.bounds.max.y - transform.position.y;
        if (height > controller.stepOffset && height <= Config.AutoJumpHeight)
            velocityY = Mathf.Sqrt(height * -2f * Config.Gravity);
    }
}