using UnityEngine;

public struct DamageInfo
{
    public float Amount;
    public GameObject Instigator;
    public Transform HitPoint;
    public bool CanBeParried;
    public float Stagger;
    public int AttackId;

    public static DamageInfo Create(float amount, GameObject instigator, bool canBeParried = true)
    {
        return new DamageInfo
        {
            Amount = amount,
            Instigator = instigator,
            CanBeParried = canBeParried,
            AttackId = 0
        };
    }
}
