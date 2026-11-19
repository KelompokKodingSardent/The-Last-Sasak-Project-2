using UnityEngine;

public class DodgeController : MonoBehaviour
{
    PlayerMovementConfig config;
    PlayerMotor motor;
    PlayerInputReader input;
    PlayerFacingController facing;
    PlayerActionController actions;
    InvulnerabilityHandler invuln;
    PlayerAnimationController anim;
    float cooldown;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        facing = GetComponent<PlayerFacingController>();
        actions = GetComponent<PlayerActionController>();
        invuln = GetComponent<InvulnerabilityHandler>();
        anim = GetComponent<PlayerAnimationController>();
        if (motor != null)
            config = motor.Config;
    }

    public void BindConfig(PlayerMovementConfig movementConfig)
    {
        config = movementConfig;
    }

    public bool TryDodge()
    {
        if (cooldown > 0f || motor == null)
            return false;

        Vector3 dir = facing != null && input != null && input.Move.sqrMagnitude > 0.01f
            ? facing.GetCameraRelativeMove(input.Move)
            : transform.forward;

        float duration = config != null ? config.DodgeDuration : 0.22f;
        float distance = config != null ? config.DodgeDistance : 5f;
        float iframe = config != null ? config.DodgeIFrame : 0.18f;

        actions?.SetState(PlayerActionState.Dodging);
        motor.AddDash(dir.normalized * distance, duration);
        invuln?.Activate(iframe);
        anim?.PlayDodge();
        cooldown = config != null ? config.DodgeCooldown : 0.45f;
        CancelInvoke(nameof(FinishDodge));
        Invoke(nameof(FinishDodge), duration);
        return true;
    }

    void FinishDodge()
    {
        if (actions != null && actions.State == PlayerActionState.Dodging)
            actions.SetState(PlayerActionState.Idle);
    }

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }
}
