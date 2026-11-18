using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.AI; // Aktifkan jika menggunakan NavMesh runtime

public class MapGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int GridSize = 10; // 1 Grid = 10x10 Unit (Bab 5)
    public int MaxY_Length = 100; // Panjang map ke atas (Menuju Boss)

[Header("Set Dressing System")]
public SetDressingGenerator SetDressingManager;

    [Header("Prefabs Library")]
    public RoomModule[] BeachRooms;
    public RoomModule[] ForestRooms;
    public RoomModule[] CastleRooms;
    public RoomModule BossRoomPrefab;

[Header("NavMesh System")]
public RuntimeNavMeshBuilder NavMeshManager;

[Header("Side Path Settings")]
[Range(0f, 1f)] public float SidePathChance = 0.4f; // 40% peluang cabang muncul di node Golden Path
public int MaxBranchLength = 2; // Maksimal panjang jalan buntu (1-2 room)
    private Dictionary<Vector2Int, MapNode> mapGraph = new Dictionary<Vector2Int, MapNode>();

    void Start()
    {
        GenerateMap();
    }

public void GenerateMap()
{
    // 1. LAYER 1: CONCEPTUAL GRAPH
    GenerateGoldenPath();
    GenerateSidePaths();

    // 2. LAYER 2: PHYSICAL GRID & SET DRESSING
    InstantiatePhysicalGrid();
    
    if (SetDressingManager != null)
    {
        SetDressingManager.GenerateSetDressing(mapGraph, GridSize, this.transform);
    }

    // 3. BAKE NAVMESH (Bab 5: Run-time Baking)
    // Dipanggil paling akhir agar AI mengenali seluruh lantai dan dinding yang baru saja di-spawn
    if (NavMeshManager != null)
    {
        NavMeshManager.BakeNavMeshRuntime();
    }
}
    private void GenerateGoldenPath()
    {
        Vector2Int currentPos = Vector2Int.zero; // Start di Y=0
        MapNode previousNode = null;

        for (int y = 0; y <= MaxY_Length; y++)
        {
            // Tentukan Bioma Berdasarkan Y (Bab 3: Macro Pattern)
            BiomeType currentBiome = BiomeType.Beach;
            if (y > 30 && y <= 60) currentBiome = BiomeType.Forest;
            else if (y > 60) currentBiome = BiomeType.Castle;

            RoomType currentType = (y == 0) ? RoomType.Start : (y == MaxY_Length) ? RoomType.Boss : RoomType.Normal;

            MapNode newNode = new MapNode(currentPos, currentType);
            newNode.Biome = currentBiome;

            // Sambungkan socket logika dengan node sebelumnya
            if (previousNode != null)
            {
                newNode.ConnectedNodes.Add(Direction.South, previousNode);
                previousNode.ConnectedNodes.Add(Direction.North, newNode);
            }

            mapGraph.Add(currentPos, newNode);
            previousNode = newNode;

            // Randomisasi X untuk Golden Path yang berbelok (Sedikit zigzag ke atas)
            if (y < MaxY_Length - 1)
            {
                int moveX = Random.Range(-1, 2); // -1 (Kiri), 0 (Lurus), 1 (Kanan)
                if (moveX != 0)
                {
                    currentPos.x += moveX;
                    MapNode sideStepNode = new MapNode(currentPos, RoomType.Combat);
                    sideStepNode.Biome = currentBiome;
                    
                    Direction dirFrom = moveX == 1 ? Direction.West : Direction.East;
                    Direction dirTo = moveX == 1 ? Direction.East : Direction.West;

                    sideStepNode.ConnectedNodes.Add(dirFrom, newNode);
                    newNode.ConnectedNodes.Add(dirTo, sideStepNode);
                    
                    mapGraph.Add(currentPos, sideStepNode);
                    previousNode = sideStepNode; // Lanjutkan ke atas dari titik X yang baru
                }
            }
            
            currentPos.y++;
        }
    }

private void GenerateSidePaths()
{
    // Simpan daftar node awal (Golden Path) agar tidak memodifikasi Collection saat iterasi
    List<MapNode> goldenPathNodes = new List<MapNode>(mapGraph.Values);

    foreach (MapNode sourceNode in goldenPathNodes)
    {
        // Abaikan titik Start dan Boss agar area tersebut tetap fokus
        if (sourceNode.Type == RoomType.Start || sourceNode.Type == RoomType.Boss) 
            continue;

        // Roll probabilitas apakah node ini akan memiliki cabang side path
        if (Random.value > SidePathChance) 
            continue;

        // Cari arah samping yang masih kosong (Utamanya East & West)
        List<Direction> availableDirections = new List<Direction>();
        
        Direction[] sideDirs = new Direction[] { Direction.East, Direction.West, Direction.North, Direction.South };
        foreach (Direction dir in sideDirs)
        {
            Vector2Int checkPos = sourceNode.GridPosition + GetDirectionOffset(dir);
            // Jika posisi tersebut belum ada ruangan di mapGraph
            if (!mapGraph.ContainsKey(checkPos))
            {
                availableDirections.Add(dir);
            }
        }

        // Jika tidak ada ruang kosong di sekitarnya, lewati
        if (availableDirections.Count == 0) 
            continue;

        // Pilih satu arah acak dari ruang kosong yang tersedia
        Direction branchDir = availableDirections[Random.Range(0, availableDirections.Count)];
        
        // Tentukan panjang cabang (1 sampai MaxBranchLength)
        int branchLength = Random.Range(1, MaxBranchLength + 1);
        
        MapNode currentParent = sourceNode;
        Vector2Int currentBranchPos = sourceNode.GridPosition;

        for (int step = 1; step <= branchLength; step++)
        {
            Vector2Int nextPos = currentBranchPos + GetDirectionOffset(branchDir);

            // Validasi ulang: pastikan slot benar-benar kosong di grid
            if (mapGraph.ContainsKey(nextPos)) 
                break; // Hentikan cabang jika menabrak ruangan lain

            // Tentukan tipe ruangan: Ujung cabang = Treasure, perantara = Combat/Normal
            bool isDeadEnd = (step == branchLength);
            RoomType branchRoomType = isDeadEnd ? RoomType.Treasure : RoomType.Combat;

            // Buat Node Baru
            MapNode sideNode = new MapNode(nextPos, branchRoomType);
            sideNode.Biome = GetBiomeByY(nextPos.y);

            // LOGIKA SOCKETING (Sambungkan dua arah)
            Direction dirFromParentToChild = branchDir;
            Direction dirFromChildToParent = GetOppositeDirection(branchDir);

            currentParent.ConnectedNodes.Add(dirFromParentToChild, sideNode);
            sideNode.ConnectedNodes.Add(dirFromChildToParent, currentParent);

            // Masukkan ke Graph
            mapGraph.Add(nextPos, sideNode);

            // Update pointer untuk iterasi cabang berikutnya (jika panjang > 1)
            currentParent = sideNode;
            currentBranchPos = nextPos;
        }
    }
}

    private void InstantiatePhysicalGrid()
    {
        foreach (KeyValuePair<Vector2Int, MapNode> item in mapGraph)
        {
            MapNode node = item.Value;
            
            // Konversi dari Grid Koordinat ke Koordinat Dunia (Unity Vector3)
            Vector3 worldPosition = new Vector3(node.GridPosition.x * GridSize, 0, node.GridPosition.y * GridSize);

            // Pilih Prefab berdasarkan Bioma dan Tipe
            RoomModule prefabToSpawn = GetRoomPrefab(node);

            // Spawn model 3D (Pastikan Pivot Center-Bottom sesuai Bab 5)
            RoomModule spawnedRoom = Instantiate(prefabToSpawn, worldPosition, Quaternion.identity, this.transform);

            // Sesuaikan Pintu dan Tembok berdasarkan Graph
            spawnedRoom.SetupSockets(node);
            spawnedRoom.gameObject.name = $"Room [{node.GridPosition.x}, {node.GridPosition.y}] - {node.Biome}";
        }
    }

    private RoomModule GetRoomPrefab(MapNode node)
    {
        if (node.Type == RoomType.Boss) return BossRoomPrefab;

        RoomModule[] biomeLibrary;
        switch (node.Biome)
        {
            case BiomeType.Forest: biomeLibrary = ForestRooms; break;
            case BiomeType.Castle: biomeLibrary = CastleRooms; break;
            default: biomeLibrary = BeachRooms; break;
        }

        // Ambil prefab acak dari array bioma yang sesuai
        return biomeLibrary[Random.Range(0, biomeLibrary.Length)];
    }

// Helper untuk mendapatkan offset koordinat berdasarkan Direction
private Vector2Int GetDirectionOffset(Direction dir)
{
    switch (dir)
    {
        case Direction.North: return new Vector2Int(0, 1);
        case Direction.South: return new Vector2Int(0, -1);
        case Direction.East:  return new Vector2Int(1, 0);
        case Direction.West:  return new Vector2Int(-1, 0);
        default: return Vector2Int.zero;
    }
}

// Helper untuk mendapatkan arah kebalikannya (contoh: East -> West)
private Direction GetOppositeDirection(Direction dir)
{
    switch (dir)
    {
        case Direction.North: return Direction.South;
        case Direction.South: return Direction.North;
        case Direction.East:  return Direction.West;
        case Direction.West:  return Direction.East;
        default: return Direction.North;
    }
}

// Helper untuk menentukan bioma berdasarkan koordinat Y (Sesuai Bab 3)
private BiomeType GetBiomeByY(int y)
{
    if (y > 60) return BiomeType.Castle;
    if (y > 30) return BiomeType.Forest;
    return BiomeType.Beach;
}


    
}