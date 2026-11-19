using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Items/Loot Definition")]
public class LootDefinitionSO : ScriptableObject
{
    public string Id = "loot";
    public int Souls;
    public int Exp;
}
