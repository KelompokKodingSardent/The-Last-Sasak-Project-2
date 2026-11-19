public struct StatModifier
{
    public float Additive;
    public float Multiplier;

    public static StatModifier Identity => new StatModifier { Multiplier = 1f };

    public float Apply(float baseValue)
    {
        return (baseValue + Additive) * (Multiplier <= 0f ? 1f : Multiplier);
    }
}
