// PlayerActionController.cs
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

        if ((input.DodgePressed || input.HasBufferedAction(BufferedActionType.Dodge)) && CanEnter(PlayerActionState.Dodging))
        {
            if (dodge != null && dodge.TryDodge())
            {
                input.ConsumeDodge();
                return;
            }
        }

        if ((input.ParryPressed || input.HasBufferedAction(BufferedActionType.Parry)) && CanEnter(PlayerActionState.Parrying))
        {
            if (parry != null && parry.TryParry())
            {
                input.ConsumeParry();
                return;
            }
        }

        if ((input.AttackPressed || input.HasBufferedAction(BufferedActionType.Attack)) && CanEnter(PlayerActionState.Attacking))
        {
            attack?.TryAttack();
        }
    }

    public bool CanEnter(PlayerActionState next)
    {
        if (State == PlayerActionState.Dead)
            return false;

        if (State == PlayerActionState.Idle)
            return true;

        if (State == PlayerActionState.Attacking)
        {
            AttackDefinitionSO def = attack != null ? attack.CurrentActiveDefinition : null;

            if (next == PlayerActionState.Attacking)
                return attack != null && attack.CanChainNextAttack;

            if (next == PlayerActionState.Dodging)
                return def == null || def.CanCancelToDodge;

            if (next == PlayerActionState.Parrying)
                return def == null || def.CanCancelToParry;

            return false;
        }

        return false;
    }

    public bool CanSwitchWeapon()
    {
        if (State == PlayerActionState.Dead || State == PlayerActionState.Dodging || State == PlayerActionState.Parrying)
            return false;

        if (State == PlayerActionState.Attacking)
            return attack != null && attack.IsInComboWindow;

        return true;
    }

    public void SetState(PlayerActionState state)
    {
        if (State == PlayerActionState.Attacking && state != PlayerActionState.Attacking)
        {
            attack?.CancelAttack();
        }

        State = state;
        if (motor != null)
            motor.CanMove = state == PlayerActionState.Idle || state == PlayerActionState.Attacking || state == PlayerActionState.Parrying;
        if (input != null)
            input.Enabled = state != PlayerActionState.Dead;
    }

    void HandleDeath()
    {
        input?.ClearBuffer();
        SetState(PlayerActionState.Dead);
        GetComponent<PlayerAnimationController>()?.PlayDead();
    }
}