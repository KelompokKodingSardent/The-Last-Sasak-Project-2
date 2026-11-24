using System.Collections.Generic;
using UnityEngine;

public class RoomGraph
{
    public int Seed;
    public int StartNodeId = -1;
    public int ExitNodeId = -1;

    public readonly List<RoomNodeData> Nodes = new List<RoomNodeData>();
    public readonly List<RoomEdgeData> Edges = new List<RoomEdgeData>();

    private readonly Dictionary<int, RoomNodeData> nodeLookup = new Dictionary<int, RoomNodeData>();

    // Kompatibilitas mundur untuk RunMapGenerator & RoomManager lama
    public readonly List<RoomPlacement> Rooms = new List<RoomPlacement>();

    public RoomPlacement Add(Vector2Int grid, RoomType type)
    {
        RoomPlacement placement = new RoomPlacement { Grid = grid, Type = type };
        Rooms.Add(placement);
        return placement;
    }

    public RoomNodeData AddNode(RoomRole role, int depth, bool isMainPath)
    {
        int id = Nodes.Count;
        RoomNodeData node = new RoomNodeData(id, role, depth, isMainPath);
        Nodes.Add(node);
        nodeLookup[id] = node;

        if (role == RoomRole.Start && StartNodeId < 0)
            StartNodeId = id;
        else if (role == RoomRole.Exit)
            ExitNodeId = id;

        return node;
    }

    public RoomEdgeData ConnectNodes(int nodeIdA, int nodeIdB, bool isMainPathEdge)
    {
        RoomNodeData nodeA = GetNode(nodeIdA);
        RoomNodeData nodeB = GetNode(nodeIdB);
        if (nodeA == null || nodeB == null || nodeIdA == nodeIdB)
            return null;

        if (nodeA.IsConnectedTo(nodeIdB))
            return null;

        RoomEdgeData edge = new RoomEdgeData(nodeIdA, nodeIdB, isMainPathEdge);
        Edges.Add(edge);
        nodeA.Edges.Add(edge);
        nodeB.Edges.Add(edge);
        return edge;
    }

    public RoomNodeData GetNode(int nodeId)
    {
        nodeLookup.TryGetValue(nodeId, out RoomNodeData node);
        return node;
    }

    public bool ValidateConnectivity(out string errorMessage)
    {
        if (Nodes.Count == 0)
        {
            errorMessage = "Graph kosong: tidak ada node ruangan.";
            return false;
        }

        RoomNodeData start = GetNode(StartNodeId);
        if (start == null)
        {
            errorMessage = "StartNode tidak ditemukan pada graph.";
            return false;
        }

        RoomNodeData exit = GetNode(ExitNodeId);
        if (exit == null)
        {
            errorMessage = "ExitNode tidak ditemukan pada graph.";
            return false;
        }

        HashSet<int> visited = new HashSet<int>();
        Queue<int> queue = new Queue<int>();

        visited.Add(start.NodeId);
        queue.Enqueue(start.NodeId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            RoomNodeData current = GetNode(currentId);
            if (current == null)
                continue;

            for (int i = 0; i < current.Edges.Count; i++)
            {
                int neighborId = current.Edges[i].GetOtherNodeId(currentId);
                if (neighborId >= 0 && visited.Add(neighborId))
                    queue.Enqueue(neighborId);
            }
        }

        if (!visited.Contains(exit.NodeId))
        {
            errorMessage = "ExitNode tidak dapat dijangkau dari StartNode.";
            return false;
        }

        if (visited.Count != Nodes.Count)
        {
            errorMessage = $"Terdapat {Nodes.Count - visited.Count} node terisolasi yang tidak terhubung ke StartNode.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

public class RoomPlacement
{
    public Vector2Int Grid;
    public RoomType Type;
    public Vector3 WorldPosition;
    public RoomController Controller;
}