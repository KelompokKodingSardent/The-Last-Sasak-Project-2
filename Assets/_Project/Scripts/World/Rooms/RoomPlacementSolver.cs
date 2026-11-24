using System.Collections.Generic;
using UnityEngine;

public class PlacedRoomData
{
    public int NodeId;
    public RoomDefinitionSO RoomDef;
    public Vector3 WorldPosition;
    public Quaternion WorldRotation;
    public int RotationAngle;
    public List<Vector2Int> OccupiedCells;
}

public class RoomPlacementSolution
{
    public readonly Dictionary<int, PlacedRoomData> PlacedRooms = new Dictionary<int, PlacedRoomData>();
    public readonly HashSet<Vector2Int> AllOccupiedCells = new HashSet<Vector2Int>();
}

public class RoomPlacementSolver
{
    private const float AlignmentTolerance = 0.05f;

    public bool TrySolve(
        RoomGraph graph,
        MapGenerationConfigSO config,
        int seed,
        out RoomPlacementSolution solution,
        out string failureReason)
    {
        solution = new RoomPlacementSolution();
        failureReason = string.Empty;

        if (graph == null || graph.Nodes.Count == 0)
        {
            failureReason = "RoomGraph kosong.";
            return false;
        }

        RoomNodeData startNode = graph.GetNode(graph.StartNodeId);
        if (startNode == null || startNode.AssignedRoomDef == null || startNode.AssignedRoomDef.Prefab == null)
        {
            failureReason = "StartNode atau prefab StartRoom tidak valid.";
            return false;
        }

        System.Random rng = new System.Random(seed ^ 0x5F3759DF);

        // Tempatkan StartNode di titik asal dunia (0, 0, 0)
        List<int> startAngles = startNode.AssignedRoomDef.GetAllowedRotationAngles();
        int startAngle = startAngles[rng.Next(0, startAngles.Count)];
        Quaternion startRot = Quaternion.Euler(0f, startAngle, 0f);
        RoomFootprint startFp = startNode.AssignedRoomDef.Prefab.GetComponent<RoomFootprint>();

        if (startFp == null)
        {
            failureReason = $"Prefab '{startNode.AssignedRoomDef.Prefab.name}' tidak memiliki RoomFootprint.";
            return false;
        }

        List<Vector2Int> startCells = startFp.GetOccupiedCells(Vector3.zero, startRot);
        PlacedRoomData startPlacement = new PlacedRoomData
        {
            NodeId = startNode.NodeId,
            RoomDef = startNode.AssignedRoomDef,
            WorldPosition = Vector3.zero,
            WorldRotation = startRot,
            RotationAngle = startAngle,
            OccupiedCells = startCells
        };

        RegisterPlacement(solution, startPlacement);

        // Susun urutan edge dalam bentuk spanning tree BFS dari StartNode
        List<RoomEdgeData> placementOrderEdges = BuildTreeEdgeOrder(graph, startNode.NodeId);
        HashSet<int> usedConnectorKeys = new HashSet<int>();
        int attemptCounter = 0;
        int maxTotalAttempts = Mathf.Max(20, config.MaxPlacementAttempts * graph.Nodes.Count);

        bool solved = PlaceEdgeRecursive(
            0,
            placementOrderEdges,
            graph,
            config,
            solution,
            usedConnectorKeys,
            rng,
            ref attemptCounter,
            maxTotalAttempts
        );

        if (!solved)
        {
            failureReason = $"Placement solver gagal menyusun seluruh {graph.Nodes.Count} ruangan setelah {attemptCounter} percobaan.";
            return false;
        }

        return true;
    }

    private bool PlaceEdgeRecursive(
        int edgeIdx,
        List<RoomEdgeData> treeEdges,
        RoomGraph graph,
        MapGenerationConfigSO config,
        RoomPlacementSolution solution,
        HashSet<int> usedConnectorKeys,
        System.Random rng,
        ref int attemptCounter,
        int maxTotalAttempts)
    {
        if (edgeIdx >= treeEdges.Count)
            return true;

        if (attemptCounter >= maxTotalAttempts)
            return false;

        RoomEdgeData edge = treeEdges[edgeIdx];
        bool fromPlaced = solution.PlacedRooms.ContainsKey(edge.FromNodeId);
        int parentNodeId = fromPlaced ? edge.FromNodeId : edge.ToNodeId;
        int childNodeId = fromPlaced ? edge.ToNodeId : edge.FromNodeId;

        PlacedRoomData parentPlaced = solution.PlacedRooms[parentNodeId];
        RoomNodeData childNode = graph.GetNode(childNodeId);

        if (parentPlaced == null || childNode == null)
            return false;

        // Kumpulkan kandidat RoomDefinition (utama + cadangan dari catalog jika buntu)
        List<RoomDefinitionSO> roomCandidates = BuildRoomCandidates(childNode, config, rng);

        RoomConnector[] parentConnectors = parentPlaced.RoomDef.Prefab.GetComponentsInChildren<RoomConnector>(true);
        List<int> parentConnIndices = CreateShuffledIndices(parentConnectors.Length, rng);

        for (int r = 0; r < roomCandidates.Count; r++)
        {
            RoomDefinitionSO candidateDef = roomCandidates[r];
            if (candidateDef == null || candidateDef.Prefab == null)
                continue;

            RoomFootprint childFp = candidateDef.Prefab.GetComponent<RoomFootprint>();
            RoomConnector[] childConnectors = candidateDef.Prefab.GetComponentsInChildren<RoomConnector>(true);
            if (childFp == null || childConnectors.Length < childNode.Degree)
                continue;

            List<int> allowedAngles = candidateDef.GetAllowedRotationAngles();
            ShuffleList(allowedAngles, rng);
            List<int> childConnIndices = CreateShuffledIndices(childConnectors.Length, rng);

            for (int p = 0; p < parentConnIndices.Count; p++)
            {
                int pConnIdx = parentConnIndices[p];
                int parentKey = MakeConnectorKey(parentNodeId, pConnIdx);
                if (usedConnectorKeys.Contains(parentKey))
                    continue;

                RoomConnector connA = parentConnectors[pConnIdx];
                Vector3 worldPosA = parentPlaced.WorldPosition + (parentPlaced.WorldRotation * connA.transform.localPosition);
                Vector3 worldFwdA = (parentPlaced.WorldRotation * connA.transform.localRotation * Vector3.forward).normalized;

                for (int c = 0; c < childConnIndices.Count; c++)
                {
                    int cConnIdx = childConnIndices[c];
                    RoomConnector connB = childConnectors[cConnIdx];

                    if (connA.SocketType != connB.SocketType)
                        continue;

                    for (int a = 0; a < allowedAngles.Count; a++)
                    {
                        attemptCounter++;
                        if (attemptCounter > maxTotalAttempts)
                            return false;

                        int angle = allowedAngles[a];
                        Quaternion candidateRot = Quaternion.Euler(0f, angle, 0f);
                        Vector3 worldFwdB = (candidateRot * connB.transform.localRotation * Vector3.forward).normalized;

                        // Arah bukaan harus saling berhadapan (-1)
                        if (Vector3.Dot(worldFwdA, worldFwdB) > -0.95f)
                            continue;

                        // Sejajarkan posisi world connector B tepat ke posisi world connector A
                        Vector3 candidatePos = worldPosA - (candidateRot * connB.transform.localPosition);
                        candidatePos = SnapPosition(candidatePos);

                        // Validasi batas radius grid dari titik Start
                        Vector2Int anchorCell = RoomFootprint.WorldToGrid(candidatePos);
                        if (Mathf.Abs(anchorCell.x) > config.MaxGridRadius || Mathf.Abs(anchorCell.y) > config.MaxGridRadius)
                            continue;

                        // Validasi okupansi cell 12x12 (tidak boleh overlap dengan ruangan lain)
                        List<Vector2Int> candidateCells = childFp.GetOccupiedCells(candidatePos, candidateRot);
                        if (HasGridOverlap(candidateCells, solution.AllOccupiedCells))
                            continue;

                        // Komit sementara penempatan ruangan ini
                        int childKey = MakeConnectorKey(childNodeId, cConnIdx);
                        PlacedRoomData childPlacement = new PlacedRoomData
                        {
                            NodeId = childNodeId,
                            RoomDef = candidateDef,
                            WorldPosition = candidatePos,
                            WorldRotation = candidateRot,
                            RotationAngle = angle,
                            OccupiedCells = candidateCells
                        };

                        RoomDefinitionSO previousDef = childNode.AssignedRoomDef;
                        childNode.AssignedRoomDef = candidateDef;
                        AssignEdgeConnectors(edge, parentNodeId, pConnIdx, childNodeId, cConnIdx);
                        usedConnectorKeys.Add(parentKey);
                        usedConnectorKeys.Add(childKey);
                        RegisterPlacement(solution, childPlacement);

                        // Lanjutkan ke edge berikutnya secara rekursif
                        if (PlaceEdgeRecursive(
                            edgeIdx + 1,
                            treeEdges,
                            graph,
                            config,
                            solution,
                            usedConnectorKeys,
                            rng,
                            ref attemptCounter,
                            maxTotalAttempts))
                        {
                            return true;
                        }

                        // Backtrack jika cabang berikutnya buntu
                        UnregisterPlacement(solution, childPlacement);
                        usedConnectorKeys.Remove(parentKey);
                        usedConnectorKeys.Remove(childKey);
                        ResetEdgeConnectors(edge);
                        childNode.AssignedRoomDef = previousDef;
                    }
                }
            }
        }

        return false;
    }

    private static List<RoomEdgeData> BuildTreeEdgeOrder(RoomGraph graph, int startNodeId)
    {
        List<RoomEdgeData> ordered = new List<RoomEdgeData>(graph.Edges.Count);
        HashSet<int> visitedNodes = new HashSet<int> { startNodeId };
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startNodeId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            RoomNodeData current = graph.GetNode(currentId);
            if (current == null)
                continue;

            for (int i = 0; i < current.Edges.Count; i++)
            {
                RoomEdgeData edge = current.Edges[i];
                int neighborId = edge.GetOtherNodeId(currentId);
                if (neighborId >= 0 && visitedNodes.Add(neighborId))
                {
                    ordered.Add(edge);
                    queue.Enqueue(neighborId);
                }
            }
        }

        return ordered;
    }

    private static List<RoomDefinitionSO> BuildRoomCandidates(
        RoomNodeData node,
        MapGenerationConfigSO config,
        System.Random rng)
    {
        List<RoomDefinitionSO> candidates = new List<RoomDefinitionSO>();
        if (node.AssignedRoomDef != null)
            candidates.Add(node.AssignedRoomDef);

        if (config != null && config.RoomCatalog != null)
        {
            List<RoomDefinitionSO> alternatives = config.RoomCatalog.GetValidCandidates(
                node.Role,
                node.Depth,
                instanceCounts: null,
                minRequiredConnectors: node.Degree
            );

            ShuffleList(alternatives, rng);
            for (int i = 0; i < alternatives.Count; i++)
            {
                if (!candidates.Contains(alternatives[i]))
                    candidates.Add(alternatives[i]);
            }
        }

        return candidates;
    }

    private static bool HasGridOverlap(List<Vector2Int> cells, HashSet<Vector2Int> occupied)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (occupied.Contains(cells[i]))
                return true;
        }
        return false;
    }

    private static void RegisterPlacement(RoomPlacementSolution solution, PlacedRoomData data)
    {
        solution.PlacedRooms[data.NodeId] = data;
        for (int i = 0; i < data.OccupiedCells.Count; i++)
            solution.AllOccupiedCells.Add(data.OccupiedCells[i]);
    }

    private static void UnregisterPlacement(RoomPlacementSolution solution, PlacedRoomData data)
    {
        solution.PlacedRooms.Remove(data.NodeId);
        for (int i = 0; i < data.OccupiedCells.Count; i++)
            solution.AllOccupiedCells.Remove(data.OccupiedCells[i]);
    }

    private static void AssignEdgeConnectors(
        RoomEdgeData edge,
        int parentNodeId,
        int parentConnIdx,
        int childNodeId,
        int childConnIdx)
    {
        if (edge.FromNodeId == parentNodeId)
        {
            edge.FromConnectorIndex = parentConnIdx;
            edge.ToConnectorIndex = childConnIdx;
        }
        else
        {
            edge.FromConnectorIndex = childConnIdx;
            edge.ToConnectorIndex = parentConnIdx;
        }
    }

    private static void ResetEdgeConnectors(RoomEdgeData edge)
    {
        edge.FromConnectorIndex = -1;
        edge.ToConnectorIndex = -1;
    }

    private static Vector3 SnapPosition(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / AlignmentTolerance) * AlignmentTolerance;
        float z = Mathf.Round(pos.z / AlignmentTolerance) * AlignmentTolerance;
        return new Vector3(x, 0f, z);
    }

    private static int MakeConnectorKey(int nodeId, int connectorIndex)
    {
        return (nodeId * 1000) + connectorIndex;
    }

    private static List<int> CreateShuffledIndices(int count, System.Random rng)
    {
        List<int> list = new List<int>(count);
        for (int i = 0; i < count; i++)
            list.Add(i);

        ShuffleList(list, rng);
        return list;
    }

    private static void ShuffleList<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}