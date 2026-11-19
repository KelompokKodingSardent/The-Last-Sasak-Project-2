using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    Animator animator;
    PlayerMotor motor;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int DodgeHash = Animator.StringToHash("Dodge");
    static readonly int ParryHash = Animator.StringToHash("Parry");
    static readonly int DeadHash = Animator.StringToHash("Dead");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        if (animator == null || motor == null)
            return;

        animator.SetFloat(SpeedHash, motor.PlanarVelocity.magnitude);
    }

    public void PlayAttack() => SetTrigger(AttackHash);
    public void PlayDodge() => SetTrigger(DodgeHash);
    public void PlayParry() => SetTrigger(ParryHash);
    public void PlayDead() => SetBool(DeadHash, true);

    void SetTrigger(int hash)
    {
        if (animator != null)
            animator.SetTrigger(hash);
    }

    void SetBool(int hash, bool value)
    {
        if (animator != null)
            animator.SetBool(hash, value);
    }
}
