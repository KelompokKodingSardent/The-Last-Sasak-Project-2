using UnityEngine;

public class StaggerComponent : MonoBehaviour
{
    public float MaxStagger = 100f;
    public float Current;

    [Header("Phase 3 — Stagger Resistance & Bulwark Guard")]
    [Range(0f, 0.85f)] public float StaggerResistance = 0f;
    [Range(0.05f, 1f)] public float GuardStaggerMultiplier = 0.25f;
    public bool IsGuardActive { get; set; }

    public System.Action OnStaggered;

    public void Add(float amount)
    {
        Add(amount, false);
    }

    public void Add(float amount, bool bypassResistance)
    {
        if (Current >= MaxStagger || amount <= 0f)
            return;

        float finalAmount = amount;
        if (!bypassResistance)
        {
            finalAmount *= (1f - Mathf.Clamp01(StaggerResistance));
            if (IsGuardActive)
                finalAmount *= Mathf.Clamp(GuardStaggerMultiplier, 0.05f, 1f);
        }

        Current += finalAmount;
        if (Current >= MaxStagger)
        {
            Current = MaxStagger;
            IsGuardActive = false;
            OnStaggered?.Invoke();
            EnemyBrain brain = GetComponent<EnemyBrain>();
            brain?.ResetBrain();
        }
    }

    public void ResetStagger()
    {
        Current = 0f;
    }
}