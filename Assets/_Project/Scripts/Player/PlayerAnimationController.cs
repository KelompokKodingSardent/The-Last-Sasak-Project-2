using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    Animator animator;
    PlayerMotor motor;
    AttackController attack;
    PlayerStatController stats;
    Health health;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int DodgeHash = Animator.StringToHash("Dodge");
    static readonly int ParryHash = Animator.StringToHash("Parry");
    static readonly int ParrySuccessHash = Animator.StringToHash("ParrySuccess");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DeadHash = Animator.StringToHash("Dead");

    readonly HashSet<int> validParameters = new HashSet<int>();
    float lastKnownHealth = -1f;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        motor = GetComponent<PlayerMotor>();
        attack = GetComponent<AttackController>();
        stats = GetComponent<PlayerStatController>();
        health = GetComponent<Health>();

        CacheAnimatorParameters();
    }

    void OnEnable()
    {
        if (health != null)
        {
            lastKnownHealth = health.CurrentHealth;
            health.OnHealthChanged += HandleHealthChanged;
        }
    }

    void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    public void RefreshAnimatorReference()
    {
        animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();
    }

    void CacheAnimatorParameters()
    {
        validParameters.Clear();
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            validParameters.Add(param.nameHash);
        }
    }

    void Update()
    {
        if (animator == null || motor == null)
            return;

        Vector3 vel = motor.PlanarVelocity;
        SetFloat(SpeedHash, vel.magnitude, 0.06f);

        Vector3 localVel = transform.InverseTransformDirection(vel);
        float maxSpeed = stats != null && stats.FinalMoveSpeed > 0.1f ? stats.FinalMoveSpeed : 8f;
        SetFloat(MoveXHash, Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f), 0.08f);
        SetFloat(MoveZHash, Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f), 0.08f);
    }

    public void PlayAttack()
    {
        int step = attack != null ? Mathf.Max(1, attack.CurrentComboStep) : 1;
        float atkSpeed = stats != null ? Mathf.Max(0.1f, stats.FinalAttackSpeed) : 1f;

        SetInt(AttackIndexHash, step);
        SetFloat(AttackSpeedHash, atkSpeed);
        SetTrigger(AttackHash);
    }

    public void PlayDodge() => SetTrigger(DodgeHash);
    public void PlayParry() => SetTrigger(ParryHash);
    public void PlayParrySuccess() => SetTrigger(ParrySuccessHash);
    public void PlayHit() => SetTrigger(HitHash);
    public void PlayDead() => SetBool(DeadHash, true);

    void HandleHealthChanged(float current, float max)
    {
        if (lastKnownHealth > 0f && current < lastKnownHealth && current > 0f)
        {
            PlayHit();
        }
        lastKnownHealth = current;
    }

    void SetTrigger(int hash)
    {
        if (animator != null && validParameters.Contains(hash))
            animator.SetTrigger(hash);
    }

    void SetBool(int hash, bool value)
    {
        if (animator != null && validParameters.Contains(hash))
            animator.SetBool(hash, value);
    }

    void SetInt(int hash, int value)
    {
        if (animator != null && validParameters.Contains(hash))
            animator.SetInteger(hash, value);
    }

    void SetFloat(int hash, float value)
    {
        if (animator != null && validParameters.Contains(hash))
            animator.SetFloat(hash, value);
    }

    void SetFloat(int hash, float value, float dampTime)
    {
        if (animator != null && validParameters.Contains(hash))
            animator.SetFloat(hash, value, dampTime, Time.deltaTime);
    }
}