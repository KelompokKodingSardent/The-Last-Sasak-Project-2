// ParryController.cs
using System.Collections;
using UnityEngine;
using TheLastSasak.Player.Stats;
public class ParryController : MonoBehaviour
{
    public float ActiveDuration = 0.2f;
    public float Recovery = 0.15f;
    public float BaseParryStagger = 15f;

    PlayerActionController actions;
    PlayerFacingController facing;
    PlayerStatController stats;
    ParryWindow window;
    PlayerAnimationController anim;
    Coroutine parryRoutine;

    void Awake()
    {
        actions = GetComponent<PlayerActionController>();
        facing = GetComponent<PlayerFacingController>();
        stats = GetComponent<PlayerStatController>();
        window = GetComponent<ParryWindow>();
        anim = GetComponent<PlayerAnimationController>();

        if (window != null)
            window.Duration = ActiveDuration;
    }

    public bool TryParry()
    {
        if (actions == null || !actions.CanEnter(PlayerActionState.Parrying))
            return false;

        if (parryRoutine != null)
            StopCoroutine(parryRoutine);

        float windowBonus = stats != null ? stats.FinalParryWindowBonus : 0f;
        float effectiveWindow = Mathf.Max(0.06f, ActiveDuration + windowBonus);

        if (window != null)
        {
            window.Duration = effectiveWindow;
            window.Open();
        }

        facing?.SnapToAimDirection();
        actions.SetState(PlayerActionState.Parrying);
        anim?.PlayParry();

        parryRoutine = StartCoroutine(ParryRoutine(effectiveWindow, Recovery));
        return true;
    }

    /// <summary>
    /// Dipanggil saat Perfect Parry berhasil: memotong recovery agar Player bisa langsung Counter-Attack.
    /// </summary>
    public void OnParrySuccess(GameObject instigator)
    {
        if (parryRoutine != null)
        {
            StopCoroutine(parryRoutine);
            parryRoutine = null;
        }

        window?.Close();
        anim?.PlayParrySuccess();

        if (instigator != null && BaseParryStagger > 0f)
        {
            StaggerComponent stagger = instigator.GetComponentInParent<StaggerComponent>();
            stagger?.Add(BaseParryStagger, true);
        }

        if (actions != null && actions.State == PlayerActionState.Parrying)
            actions.SetState(PlayerActionState.Idle);
    }

    IEnumerator ParryRoutine(float activeTime, float recoveryTime)
    {
        yield return new WaitForSeconds(activeTime);
        window?.Close();

        // Whiff recovery: berlaku penuh jika tidak ada serangan yang berhasil di-parry
        yield return new WaitForSeconds(recoveryTime);

        if (actions != null && actions.State == PlayerActionState.Parrying)
            actions.SetState(PlayerActionState.Idle);

        parryRoutine = null;
    }
}