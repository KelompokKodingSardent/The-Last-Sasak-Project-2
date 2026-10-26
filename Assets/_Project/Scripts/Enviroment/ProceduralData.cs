using System.Collections.Generic;
using UnityEngine;

// Enum untuk tipe ruangan (Bab 1)
public enum RoomType { Start, Normal, Combat, Treasure, Boss, Transition }

// Enum untuk Bioma berdasarkan Ketinggian Y (Bab 3)
public enum BiomeType { Beach, Forest, Castle }

// Arah Socket (Bab 2)
public enum Direction { North, South, East, West }

// Class Node untuk Conceptual Graph (Layer 1)
public class MapNode
{
    public Vector2Int GridPosition;
    public RoomType Type;
    public BiomeType Biome;
    
    // Menyimpan koneksi ke Node lain (Socket Terbuka)
    public Dictionary<Direction, MapNode> ConnectedNodes = new Dictionary<Direction, MapNode>();

    public MapNode(Vector2Int pos, RoomType type)
    {
        GridPosition = pos;
        Type = type;
    }
}

