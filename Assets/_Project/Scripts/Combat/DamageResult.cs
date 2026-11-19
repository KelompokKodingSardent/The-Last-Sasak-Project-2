public enum DamageOutcome
{
    Applied,
    Blocked,
    Parried,
    Ignored,
    Killed
}

public struct DamageResult
{
    public DamageOutcome Outcome;
    public float AppliedAmount;
    public float RemainingHealth;

    public static DamageResult Ignored => new DamageResult { Outcome = DamageOutcome.Ignored };
    public static DamageResult Blocked => new DamageResult { Outcome = DamageOutcome.Blocked };

    public static DamageResult Applied(float amount, float remaining)
    {
        return new DamageResult
        {
            Outcome = DamageOutcome.Applied,
            AppliedAmount = amount,
            RemainingHealth = remaining
        };
    }

    public static DamageResult Killed(float amount)
    {
        return new DamageResult
        {
            Outcome = DamageOutcome.Killed,
            AppliedAmount = amount,
            RemainingHealth = 0f
        };
    }

    public static DamageResult Parried()
    {
        return new DamageResult { Outcome = DamageOutcome.Parried };
    }
}
