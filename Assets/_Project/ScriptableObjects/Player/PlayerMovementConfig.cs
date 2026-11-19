using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Player/Movement Config")]
public class PlayerMovementConfig : ScriptableObject
{
    public float MoveSpeed = 8f;
    public float TurnSmoothTime = 0.1f;
    public float Gravity = -20f;
    public float StepOffset = 0.4f;
    public float SlopeLimit = 45f;
    public float AutoJumpHeight = 1.1f;
    public float DodgeDistance = 5f;
    public float DodgeDuration = 0.22f;
    public float DodgeIFrame = 0.18f;
    public float DodgeCooldown = 0.45f;

    public static PlayerMovementConfig CreateRuntime()
    {
        return CreateInstance<PlayerMovementConfig>();
    }
}
