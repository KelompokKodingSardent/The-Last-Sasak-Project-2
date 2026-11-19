using UnityEngine;
using Unity.AI.Navigation;

// Generator ini tidak membuat geometri. Pasang RoomController di scene, lalu isi ExampleRooms.
public class RunMapGenerator : MonoBehaviour
{
    public MapGenerationConfigSO Config;
    public RoomGraph Graph { get; private set; }

    public RoomGraph Generate(Transform parent)
    {
        Graph = new RoomGraph();
        RoomController[] rooms = parent != null
            ? parent.GetComponentsInChildren<RoomController>(true)
            : GetComponentsInChildren<RoomController>(true);

        for (int i = 0; i < rooms.Length; i++)
        {
            RoomController room = rooms[i];
            if (room == null)
                continue;

            RoomType type = i == 0 ? RoomType.Start : (i == rooms.Length - 1 ? RoomType.Boss : RoomType.Combat);
            RoomPlacement placement = Graph.Add(Vector2Int.up * i, type);
            placement.WorldPosition = room.transform.position;
            placement.Controller = room;
        }

        return Graph;
    }

    public bool AllReachable()
    {
        return Graph != null && Graph.Rooms.Count > 0;
    }

    public void BakeNavMesh()
    {
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface != null)
            surface.BuildNavMesh();
    }
}
