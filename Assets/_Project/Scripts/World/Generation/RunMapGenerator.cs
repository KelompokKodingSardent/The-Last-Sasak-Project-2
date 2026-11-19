using UnityEngine;
using Unity.AI.Navigation;

public class RunMapGenerator : MonoBehaviour
{
    public MapGenerationConfigSO Config;
    public RoomGraph Graph { get; private set; }

    public RoomGraph Generate(Transform parent)
    {
        if (Config == null)
            Config = MapGenerationConfigSO.CreateRuntime(Random.Range(1, 99999), 5);

        Random.InitState(Config.Seed);
        Graph = new RoomGraph();
        Vector2Int pos = Vector2Int.zero;
        for (int i = 0; i < Config.RoomCount; i++)
        {
            RoomType type = i == 0 ? RoomType.Start : (i == Config.RoomCount - 1 ? RoomType.Boss : RoomType.Combat);
            RoomPlacement placement = Graph.Add(pos, type);
            placement.WorldPosition = new Vector3(pos.x * Config.RoomSize, 0f, pos.y * Config.RoomSize);
            placement.Controller = BuildRoom(placement, parent, i == Config.RoomCount - 1);
            pos += Vector2Int.up;

            if (type == RoomType.Combat && Random.value < 0.35f)
            {
                Vector2Int side = pos + Vector2Int.right;
                RoomPlacement treasure = Graph.Add(new Vector2Int(placement.Grid.x + 1, placement.Grid.y), RoomType.Treasure);
                treasure.WorldPosition = new Vector3(side.x * Config.RoomSize, 0f, placement.Grid.y * Config.RoomSize);
                treasure.Controller = BuildRoom(treasure, parent, false);
            }
        }

        return Graph;
    }

    public bool AllReachable()
    {
        return Graph != null && Graph.Rooms.Count > 0;
    }

    RoomController BuildRoom(RoomPlacement placement, Transform parent, bool elite)
    {
        GameObject room = new GameObject($"Room_{placement.Grid.x}_{placement.Grid.y}_{placement.Type}");
        room.transform.SetParent(parent);
        room.transform.position = placement.WorldPosition;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(room.transform, false);
        floor.transform.localScale = Vector3.one * (Config.RoomSize / 10f);
        GameplayLayers.TrySet(floor, GameplayLayers.Environment);

        float half = Config.RoomSize * 0.5f;
        CreateWall(room.transform, new Vector3(half, 1f, 0f), new Vector3(0.4f, 2f, Config.RoomSize));
        CreateWall(room.transform, new Vector3(-half, 1f, 0f), new Vector3(0.4f, 2f, Config.RoomSize));
        CreateWall(room.transform, new Vector3(0f, 1f, half), new Vector3(4f, 2f, 0.4f));
        CreateWall(room.transform, new Vector3(0f, 1f, -half), new Vector3(4f, 2f, 0.4f));

        RoomController controller = room.AddComponent<RoomController>();
        GameObject spawnA = new GameObject("Spawn_Melee");
        spawnA.transform.SetParent(room.transform);
        spawnA.transform.localPosition = new Vector3(-2f, 0f, 3f);
        EnemySpawnPoint melee = spawnA.AddComponent<EnemySpawnPoint>();
        melee.Archetype = elite ? EnemyArchetype.Elite : EnemyArchetype.Melee;

        GameObject spawnB = new GameObject("Spawn_Ranged");
        spawnB.transform.SetParent(room.transform);
        spawnB.transform.localPosition = new Vector3(3f, 0f, 2f);
        EnemySpawnPoint ranged = spawnB.AddComponent<EnemySpawnPoint>();
        ranged.Archetype = EnemyArchetype.Ranged;

        controller.SpawnPoints = new[] { melee, ranged };
        GameObject playerSpawn = new GameObject("PlayerSpawn");
        playerSpawn.transform.SetParent(room.transform);
        playerSpawn.transform.localPosition = new Vector3(0f, 0f, -4f);
        controller.PlayerSpawn = playerSpawn.transform;

        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "ExitGate";
        gate.transform.SetParent(room.transform, false);
        gate.transform.localPosition = new Vector3(0f, 1f, half - 0.2f);
        gate.transform.localScale = new Vector3(3f, 2f, 0.3f);
        GameplayLayers.TrySet(gate, GameplayLayers.Environment);
        controller.LockedExits = new[] { gate };

        GameObject exit = new GameObject("RoomExit");
        exit.transform.SetParent(room.transform, false);
        exit.transform.localPosition = new Vector3(0f, 1f, half);
        BoxCollider trigger = exit.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3f, 2f, 1.5f);
        RoomExit roomExit = exit.AddComponent<RoomExit>();
        roomExit.Room = controller;

        RoomConnector north = exit.AddComponent<RoomConnector>();
        north.Direction = Direction.North;
        north.Open = true;
        return controller;
    }

    static void CreateWall(Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
        GameplayLayers.TrySet(wall, GameplayLayers.Environment);
    }

    public void BakeNavMesh()
    {
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();
    }
}