using System.Collections.Generic;

public class RoomNodeData
{
    public int NodeId;
    public RoomRole Role;
    public int Depth;
    public bool IsMainPath;

    public RoomDefinitionSO AssignedRoomDef;
    public EncounterCompositionSO AssignedEncounter;

    public readonly List<RoomEdgeData> Edges = new List<RoomEdgeData>();

    public int Degree => Edges.Count;

    public RoomNodeData(int nodeId, RoomRole role, int depth, bool isMainPath)
    {
        NodeId = nodeId;
        Role = role;
        Depth = depth;
        IsMainPath = isMainPath;
    }

    public bool IsConnectedTo(int otherNodeId)
    {
        for (int i = 0; i < Edges.Count; i++)
        {
            if (Edges[i].GetOtherNodeId(NodeId) == otherNodeId)
                return true;
        }
        return false;
    }
}