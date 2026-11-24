public class RoomEdgeData
{
    public int FromNodeId;
    public int ToNodeId;
    public bool IsMainPathEdge;

    // Diisi oleh RoomPlacementSolver saat connector berhasil disejajarkan
    public int FromConnectorIndex = -1;
    public int ToConnectorIndex = -1;

    public RoomEdgeData(int fromNodeId, int toNodeId, bool isMainPathEdge)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        IsMainPathEdge = isMainPathEdge;
    }

    public int GetOtherNodeId(int nodeId)
    {
        if (nodeId == FromNodeId)
            return ToNodeId;
        if (nodeId == ToNodeId)
            return FromNodeId;
        return -1;
    }
}