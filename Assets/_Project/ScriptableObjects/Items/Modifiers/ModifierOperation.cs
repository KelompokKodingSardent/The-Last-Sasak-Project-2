namespace TheLastSasak.Items.Modifiers
{
    public enum StatType
    {
        MaxHealth,
        AttackDamage,
        AttackSpeed,
        DamageReduction,
        PoiseResistance,
        KnockbackResistance,
        MoveSpeed,
        StaminaRegen
    }

    public enum ModifierOperation
    {
        Additive,       // Base + Value
        Multiplicative, // * (1 + Value) atau faktor pengali sesuai konvensi PlayerStatController
        Override        // Mengganti nilai akhir (gunakan hanya untuk kondisi khusus)
    }
}