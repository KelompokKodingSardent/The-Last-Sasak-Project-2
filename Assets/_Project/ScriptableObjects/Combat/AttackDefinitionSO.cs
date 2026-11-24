using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Combat/Attack Definition")]
public class AttackDefinitionSO : ScriptableObject
{
    [Header("Core Combat")]
    public float Damage = 20f;
    public float Startup = 0.08f;
    public float Active = 0.12f;
    public float Recovery = 0.18f;
    public float Range = 1.4f;
    public float Radius = 0.7f;
    public float Stagger = 10f;

    [Header("Combo & Cancel Rules")]
    public AttackDefinitionSO NextAttack;
    [Range(0f, 1f)] public float ComboWindowStartNormalized = 0.45f;
    public bool CanCancelToDodge = true;
    public bool CanCancelToParry = true;

    [Header("Movement & Game Feel")]
    public float ForwardLungeSpeed = 2.2f;
    public float ForwardLungeDuration = 0.08f;
    public float HitstopDuration = 0.03f;
    public float CameraShakeIntensity = 0.6f;

    public static AttackDefinitionSO CreateRuntime(string name, float damage, float range)
    {
        AttackDefinitionSO so = CreateInstance<AttackDefinitionSO>();
        so.name = name;
        so.Damage = damage;
        so.Range = range;
        return so;
    }
}