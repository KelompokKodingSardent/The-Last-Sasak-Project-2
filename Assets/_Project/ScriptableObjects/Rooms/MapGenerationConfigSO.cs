using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Rooms/Map Generation Config")]
public class MapGenerationConfigSO : ScriptableObject
{
    public int RoomCount = 5;
    public int Seed = 1;
    public float RoomSize = 16f;
    public BiomeType Biome = BiomeType.Forest;

    public static MapGenerationConfigSO CreateRuntime(int seed, int rooms)
    {
        MapGenerationConfigSO so = CreateInstance<MapGenerationConfigSO>();
        so.Seed = seed;
        so.RoomCount = rooms;
        return so;
    }
}
