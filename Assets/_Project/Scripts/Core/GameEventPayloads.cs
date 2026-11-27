using System;

[Serializable]
public readonly struct RunId : IEquatable<RunId>
{
    public readonly int Value;

    public RunId(int value)
    {
        Value = value;
    }

    public bool IsValid => Value > 0;
    public bool Equals(RunId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is RunId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value > 0 ? $"Run#{Value}" : "Run#None";

    public static bool operator ==(RunId left, RunId right) => left.Equals(right);
    public static bool operator !=(RunId left, RunId right) => !left.Equals(right);
}

[Serializable]
public readonly struct PoolLeaseId : IEquatable<PoolLeaseId>
{
    public readonly int Value;

    public PoolLeaseId(int value)
    {
        Value = value;
    }

    public bool IsValid => Value > 0;
    public bool Equals(PoolLeaseId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is PoolLeaseId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value > 0 ? $"Lease#{Value}" : "Lease#None";

    public static bool operator ==(PoolLeaseId left, PoolLeaseId right) => left.Equals(right);
    public static bool operator !=(PoolLeaseId left, PoolLeaseId right) => !left.Equals(right);
}

public enum RunEndReason
{
    None = 0,
    PlayerDied = 1,
    Completed = 2,
    AbandonedToHub = 3,
    AbandonedToMenu = 4,
    Restarted = 5
}