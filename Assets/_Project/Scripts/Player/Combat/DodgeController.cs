using System.Collections;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Player.Stats;
public class DodgeController : MonoBehaviour
{
    PlayerMovementConfig config;
    PlayerMotor motor;
    PlayerInputReader input;
    PlayerFacingController facing;
    PlayerActionController actions;
    PlayerStatController stats;
    InvulnerabilityHandler invuln;
    ArtifactController artifact;
    PlayerAnimationController anim;

    float cooldown;
    Coroutine dodgeRoutine;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        facing = GetComponent<PlayerFacingController>();
        actions = GetComponent<PlayerActionController>();
        stats = GetComponent<PlayerStatController>();
        invuln = GetComponent<InvulnerabilityHandler>();
        artifact = GetComponent<ArtifactController>();
        anim = GetComponent<PlayerAnimationController>();

        if (motor != null)
            config = motor.Config;
    }

    void Start()
    {
        if (config == null && motor != null)
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

        if (actions != null && !actions.CanEnter(PlayerActionState.Dodging))
            return false;

        Vector3 dir = facing != null && input != null && input.Move.sqrMagnitude > 0.01f
            ? facing.GetCameraRelativeMove(input.Move)
            : transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;
        dir.Normalize();

        float duration = config != null ? config.DodgeDuration : 0.22f;
        float distance = config != null ? config.DodgeDistance : 5f;
        float iframe = config != null ? config.DodgeIFrame : 0.18f;
        float baseCooldown = config != null ? config.DodgeCooldown : 0.45f;
        float recoveryMult = stats != null ? Mathf.Max(0.25f, stats.FinalDodgeRecoveryMultiplier) : 1f;

        if (dodgeRoutine != null)
            StopCoroutine(dodgeRoutine);

        actions?.SetState(PlayerActionState.Dodging);
        facing?.FaceDirection(dir, instant: true);
        motor.AddDash(dir * distance, duration);
        invuln?.Activate(iframe);
        artifact?.NotifyDodgeSuccess();
        anim?.PlayDodge();

        cooldown = baseCooldown / recoveryMult;
        dodgeRoutine = StartCoroutine(DodgeRoutine(duration));
        return true;
    }

    IEnumerator DodgeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (actions != null && actions.State == PlayerActionState.Dodging)
            actions.SetState(PlayerActionState.Idle);

        dodgeRoutine = null;
    }

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }
}