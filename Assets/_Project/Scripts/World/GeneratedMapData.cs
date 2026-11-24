using System.Collections.Generic;
using UnityEngine;

public class AssembledRoomInstance
{
    public int NodeId;
    public RoomNodeData NodeData;
    public PlacedRoomData PlacementData;
    public GameObject RoomObject;
    public RoomController Controller;
    public RoomFootprint Footprint;
    public RoomConnector[] Connectors;

    public Vector3 GetWorldCenter()
    {
        if (Footprint != null)
        {
            Vector3 localCenter = Footprint.GetLocalBounds().center;
            localCenter.y = 0f;
            return RoomObject.transform.TransformPoint(localCenter);
        }

        return RoomObject != null ? RoomObject.transform.position : Vector3.zero;
    }
}

public class GeneratedMapData
{
    public int Seed;
    public int AttemptIndex;
    public MapGenerationConfigSO Config;
    public RoomGraph Graph;
    public RoomPlacementSolution PlacementSolution;

    public readonly List<AssembledRoomInstance> RoomList = new List<AssembledRoomInstance>();
    public readonly Dictionary<int, AssembledRoomInstance> RoomsByNodeId = new Dictionary<int, AssembledRoomInstance>();

    public AssembledRoomInstance StartRoom;
    public AssembledRoomInstance ExitRoom;
    public Vector3 PlayerSpawnPosition;
    public Quaternion PlayerSpawnRotation = Quaternion.identity;

    public bool TryGetRoom(int nodeId, out AssembledRoomInstance instance)
    {
        return RoomsByNodeId.TryGetValue(nodeId, out instance);
    }

    public AssembledRoomInstance FindByController(RoomController controller)
    {
        if (controller == null)
            return null;

        for (int i = 0; i < RoomList.Count; i++)
        {
            if (RoomList[i].Controller == controller)
                return RoomList[i];
        }

        return null;
    }
}