namespace TheLastSasak.Items.Modifiers
{
    public interface IStatModifier
    {
        StatType StatType { get; }
        ModifierOperation Operation { get; }
        float Value { get; }
        string SourceId { get; }
    }
}