using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Rooms/Room Definition")]
public class RoomDefinitionSO : ScriptableObject
{
    public string Id = "forest_combat";
    public RoomType Type = RoomType.Combat;
    public BiomeType Biome = BiomeType.Forest;
}
