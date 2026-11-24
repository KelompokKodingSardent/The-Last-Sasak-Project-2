using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Player/Movement Config")]
public class PlayerMovementConfig : ScriptableObject
{
    [Header("Base Locomotion")]
    public float MoveSpeed = 8f;
    public float TurnSmoothTime = 0.08f;
    public float Gravity = -20f;
    public float StepOffset = 0.4f;
    public float SlopeLimit = 45f;
    public float AutoJumpHeight = 1.1f;

    [Header("Combat Locomotion")]
    [Range(0f, 1f)] public float AttackMoveSpeedMultiplier = 0.35f;
    [Range(0f, 1f)] public float ParryMoveSpeedMultiplier = 0.2f;
    public float AimSnapTurnTime = 0.03f;

    [Header("Dodge Settings")]
    public float DodgeDistance = 5f;
    public float DodgeDuration = 0.22f;
    public float DodgeIFrame = 0.18f;
    public float DodgeCooldown = 0.45f;

    public static PlayerMovementConfig CreateRuntime()
    {
        return CreateInstance<PlayerMovementConfig>();
    }
}