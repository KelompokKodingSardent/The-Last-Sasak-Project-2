using UnityEngine;

public enum PlayerActionState
{
    Idle,
    Attacking,
    Parrying,
    Dodging,
    Dead
}

public class PlayerActionController : MonoBehaviour
{
    public PlayerActionState State { get; private set; } = PlayerActionState.Idle;

    PlayerInputReader input;
    DodgeController dodge;
    AttackController attack;
    ParryController parry;
    PlayerMotor motor;
    Health health;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        dodge = GetComponent<DodgeController>();
        attack = GetComponent<AttackController>();
        parry = GetComponent<ParryController>();
        motor = GetComponent<PlayerMotor>();
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    void Update()
    {
        if (State == PlayerActionState.Dead || input == null)
            return;

        if (input.DodgePressed && CanEnter(PlayerActionState.Dodging))
        {
            dodge?.TryDodge();
            return;
        }

        if (input.ParryPressed && CanEnter(PlayerActionState.Parrying))
        {
            parry?.TryParry();
            return;
        }

        if (input.AttackPressed && CanEnter(PlayerActionState.Attacking))
            attack?.TryAttack();
    }

    public bool CanEnter(PlayerActionState next)
    {
        if (State == PlayerActionState.Dead)
            return false;

        if (State == PlayerActionState.Idle)
            return true;

        if (State == PlayerActionState.Attacking)
        {
            AttackDefinitionSO def = attack != null ? attack.Definition : null;
            if (next == PlayerActionState.Dodging)
                return def == null || def.CanCancelToDodge;
            if (next == PlayerActionState.Parrying)
                return def == null || def.CanCancelToParry;
            return false;
        }

        return false;
    }

    public void SetState(PlayerActionState state)
    {
        State = state;
        if (motor != null)
            motor.CanMove = state == PlayerActionState.Idle || state == PlayerActionState.Attacking;
        if (input != null)
            input.Enabled = state != PlayerActionState.Dead;
    }

    void HandleDeath()
    {
        SetState(PlayerActionState.Dead);
        GetComponent<PlayerAnimationController>()?.PlayDead();
    }
}
