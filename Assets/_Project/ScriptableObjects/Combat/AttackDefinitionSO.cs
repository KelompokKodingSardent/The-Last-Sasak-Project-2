using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Combat/Attack Definition")]
public class AttackDefinitionSO : ScriptableObject
{
    public float Damage = 20f;
    public float Startup = 0.08f;
    public float Active = 0.12f;
    public float Recovery = 0.18f;
    public float Range = 1.4f;
    public float Radius = 0.7f;
    public bool CanCancelToDodge = true;
    public bool CanCancelToParry = true;
    public float Stagger = 10f;

    public static AttackDefinitionSO CreateRuntime(string name, float damage, float range)
    {
        AttackDefinitionSO so = CreateInstance<AttackDefinitionSO>();
        so.name = name;
        so.Damage = damage;
        so.Range = range;
        return so;
    }
}
