using UnityEngine;

public class RoomModule : MonoBehaviour
{
    [Header("Room Settings (10x10 Unit)")]
    public BiomeType RoomBiome;
    public RoomType RoomCategory;

    [Header("Sockets / Doors (Assign in Inspector)")]
    public GameObject DoorNorth;
    public GameObject DoorSouth;
    public GameObject DoorEast;
    public GameObject DoorWest;

    [Header("Walls (To block closed sockets)")]
    public GameObject WallNorth;
    public GameObject WallSouth;
    public GameObject WallEast;
    public GameObject WallWest;

    // Fungsi untuk menyesuaikan fisik ruangan dengan Data Graph (Layer 2)
    public void SetupSockets(MapNode nodeData)
    {
        // Jika Node terhubung ke Utara, buka pintu Utara, matikan tembok Utara
        bool hasNorth = nodeData.ConnectedNodes.ContainsKey(Direction.North);
        DoorNorth.SetActive(hasNorth);
        WallNorth.SetActive(!hasNorth);

        bool hasSouth = nodeData.ConnectedNodes.ContainsKey(Direction.South);
        DoorSouth.SetActive(hasSouth);
        WallSouth.SetActive(!hasSouth);

        bool hasEast = nodeData.ConnectedNodes.ContainsKey(Direction.East);
        DoorEast.SetActive(hasEast);
        WallEast.SetActive(!hasEast);

        bool hasWest = nodeData.ConnectedNodes.ContainsKey(Direction.West);
        DoorWest.SetActive(hasWest);
        WallWest.SetActive(!hasWest);
    }
}