using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Items/Equipment Definition")]
public class EquipmentDefinitionSO : ScriptableObject
{
    public string Id = "weapon";
    public AttackDefinitionSO Attack;
}
