using UnityEngine;

public class ParryController : MonoBehaviour
{
    public float ActiveDuration = 0.2f;
    public float Recovery = 0.15f;

    PlayerActionController actions;
    ParryWindow window;
    PlayerAnimationController anim;

    void Awake()
    {
        actions = GetComponent<PlayerActionController>();
        window = GetComponent<ParryWindow>();
        anim = GetComponent<PlayerAnimationController>();
        if (window != null)
            window.Duration = ActiveDuration;
    }

    public bool TryParry()
    {
        if (actions == null || !actions.CanEnter(PlayerActionState.Parrying))
            return false;

        actions.SetState(PlayerActionState.Parrying);
        window.Open();
        anim?.PlayParry();
        CancelInvoke(nameof(FinishParry));
        Invoke(nameof(FinishParry), ActiveDuration + Recovery);
        return true;
    }

    void FinishParry()
    {
        window.Close();
        if (actions != null && actions.State == PlayerActionState.Parrying)
            actions.SetState(PlayerActionState.Idle);
    }
}
