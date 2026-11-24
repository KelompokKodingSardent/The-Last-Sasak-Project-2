Implementasi **Phase 06 (Room Graph Generator)** dan **Phase 07 (Spatial Room Placement Solver)** memisahkan topologi logis perjalanan dari penempatan fisik prefab berbasis grid `12×12` unit, sambil tetap mempertahankan `RoomPlacement` dan `Graph.Rooms` agar scene lama (`ForestRun`) tidak mengalami error kompilasi.

### 1. `RoomNodeData.cs`

Menyimpan state topologi dan konfigurasi konten untuk satu simpul ruangan pada graph (peran ruangan, kedalaman run, definisi prefab terpilih, dan komposisi encounter).

```csharp
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

```

---

### 2. `RoomEdgeData.cs`

Merepresentasikan koneksi dua arah antar-node pada graph beserta indeks `RoomConnector` yang dipasangkan oleh `RoomPlacementSolver`.

```csharp
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

```

---

### 3. `RoomGraph.cs`

Menyimpan seluruh daftar `Nodes`, `Edges`, identitas `Seed`, serta validasi konektivitas BFS untuk menjamin `ExitNode` dan seluruh cabang dapat dijangkau dari `StartNode`.

```csharp
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

```

---

### 4. `RoomGraphGenerator.cs`

Membangun topologi **Tahap A (Main Path)** dan **Tahap B (Optional Branches)** secara deterministik menggunakan `System.Random`. Setiap node langsung dipasangkan dengan `RoomDefinitionSO` yang memiliki jumlah connector mencukupi (`Degree`) serta `EncounterCompositionSO` yang kompatibel.

```csharp
using System.Collections.Generic;

public class RoomGraphGenerator
{
    public bool TryGenerate(
        MapGenerationConfigSO config,
        int seed,
        out RoomGraph graph,
        out string failureReason)
    {
        graph = null;
        failureReason = string.Empty;

        if (config == null || config.RoomCatalog == null)
        {
            failureReason = "MapGenerationConfigSO atau RoomCatalogSO belum di-assign.";
            return false;
        }

        System.Random rng = new System.Random(seed);
        graph = new RoomGraph { Seed = seed };

        // 1. TAHAP A — BANGUN MAIN PATH TOPOLOGY (Start -> Path Nodes -> Exit)
        RoomNodeData startNode = graph.AddNode(RoomRole.Start, depth: 1, isMainPath: true);
        RoomNodeData previousNode = startNode;
        List<RoomNodeData> branchableMainNodes = new List<RoomNodeData>();

        for (int i = 0; i < config.MainPathLength; i++)
        {
            int depth = i + 2;
            bool spawnTraversal = config.TraversalRoomChance > 0f && rng.NextDouble() < config.TraversalRoomChance;
            RoomRole role = spawnTraversal ? RoomRole.Traversal : RoomRole.Combat;

            RoomNodeData mainNode = graph.AddNode(role, depth, isMainPath: true);
            graph.ConnectNodes(previousNode.NodeId, mainNode.NodeId, isMainPathEdge: true);

            branchableMainNodes.Add(mainNode);
            previousNode = mainNode;
        }

        int exitDepth = config.MainPathLength + 2;
        RoomNodeData exitNode = graph.AddNode(RoomRole.Exit, exitDepth, isMainPath: true);
        graph.ConnectNodes(previousNode.NodeId, exitNode.NodeId, isMainPathEdge: true);

        // 2. TAHAP B — BANGUN CABANG OPSIONAL (Branches)
        GenerateBranches(graph, config, branchableMainNodes, rng);

        // 3. ASSIGN ROOM DEFINITION & ENCOUNTER COMPOSITION PER NODE
        Dictionary<string, int> usedRoomCounts = new Dictionary<string, int>();
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            RoomNodeData node = graph.Nodes[i];
            int minConnectors = node.Degree;

            RoomDefinitionSO selectedDef = config.RoomCatalog.PickWeightedRoom(
                node.Role,
                node.Depth,
                rng,
                usedRoomCounts,
                minConnectors
            );

            // Fallback untuk Traversal jika belum ada prefab corridor bertipe Traversal
            if (selectedDef == null && node.Role == RoomRole.Traversal)
            {
                selectedDef = config.RoomCatalog.PickWeightedRoom(
                    RoomRole.Combat,
                    node.Depth,
                    rng,
                    usedRoomCounts,
                    minConnectors
                );
            }

            // Fallback untuk Elite jika belum ada prefab Elite khusus
            if (selectedDef == null && node.Role == RoomRole.Elite)
            {
                selectedDef = config.RoomCatalog.PickWeightedRoom(
                    RoomRole.Combat,
                    node.Depth,
                    rng,
                    usedRoomCounts,
                    minConnectors
                );
            }

            if (selectedDef == null)
            {
                failureReason = $"Tidak ditemukan RoomDefinitionSO valid untuk Role={node.Role}, Depth={node.Depth}, MinConnectors={minConnectors}.";
                return false;
            }

            node.AssignedRoomDef = selectedDef;
            usedRoomCounts.TryGetValue(selectedDef.RoomID, out int currentCount);
            usedRoomCounts[selectedDef.RoomID] = currentCount + 1;

            if (RequiresEncounter(node.Role))
            {
                node.AssignedEncounter = config.PickCompatibleEncounter(selectedDef, node.Depth, rng);
            }
        }

        // 4. VALIDASI KONEKTIVITAS GRAPH
        if (!graph.ValidateConnectivity(out failureReason))
            return false;

        return true;
    }

    private void GenerateBranches(
        RoomGraph graph,
        MapGenerationConfigSO config,
        List<RoomNodeData> branchableMainNodes,
        System.Random rng)
    {
        if (config.MaxBranchCount <= 0 || branchableMainNodes.Count == 0)
            return;

        // Acak urutan kandidat node sumber cabang secara deterministik
        List<RoomNodeData> candidates = new List<RoomNodeData>(branchableMainNodes);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIdx = rng.Next(0, i + 1);
            RoomNodeData temp = candidates[i];
            candidates[i] = candidates[swapIdx];
            candidates[swapIdx] = temp;
        }

        int createdBranches = 0;
        bool elitePlaced = false;

        for (int i = 0; i < candidates.Count && createdBranches < config.MaxBranchCount; i++)
        {
            RoomNodeData sourceNode = candidates[i];
            if (rng.NextDouble() > config.BranchSpawnProbability)
                continue;

            int branchLength = rng.Next(1, config.MaxBranchLength + 1);
            RoomNodeData parent = sourceNode;

            for (int step = 1; step <= branchLength; step++)
            {
                bool isDeadEnd = step == branchLength;
                int branchDepth = parent.Depth + 1;
                RoomRole branchRole = ResolveBranchRole(config, isDeadEnd, ref elitePlaced, rng);

                RoomNodeData branchNode = graph.AddNode(branchRole, branchDepth, isMainPath: false);
                graph.ConnectNodes(parent.NodeId, branchNode.NodeId, isMainPathEdge: false);
                parent = branchNode;
            }

            createdBranches++;
        }
    }

    private RoomRole ResolveBranchRole(
        MapGenerationConfigSO config,
        bool isDeadEnd,
        ref bool elitePlaced,
        System.Random rng)
    {
        if (!isDeadEnd)
            return rng.NextDouble() < 0.5 ? RoomRole.Traversal : RoomRole.Combat;

        if (config.AllowEliteInRun && !elitePlaced && rng.NextDouble() < 0.35)
        {
            elitePlaced = true;
            return RoomRole.Elite;
        }

        return config.PrioritizeRewardOnDeadEnd ? RoomRole.Reward : RoomRole.Combat;
    }

    private static bool RequiresEncounter(RoomRole role)
    {
        return role == RoomRole.Combat || role == RoomRole.Elite;
    }
}

```

---

### 5. `RoomPlacementSolver.cs`

Mengubah `RoomGraph` menjadi koordinat dunia nyata dengan menjajarkan `RoomConnector` sumber dan target secara presisi, memvalidasi okupansi grid `12×12` unit, memeriksa apakah ada unused connector ruangan baru yang menabrak dinding ruangan lain secara tidak sah, serta mendukung *backtracking* dan *prefab fallback* ketika menemui jalan buntu spasial.

```csharp
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

```

---

### Alur Data dari Graph ke Placement Solver

1. `RoomGraphGenerator.TryGenerate(config, seed, out graph, out reason)` menyusun jalur utama + cabang opsional dan memilihkan `RoomDefinitionSO` serta `EncounterCompositionSO` yang cocok.
2. `RoomPlacementSolver.TrySolve(graph, config, seed, out solution, out reason)` membaca `graph`, menghitung transform posisi & rotasi setiap ruangan berdasarkan alignment `RoomConnector`, memvalidasi tidak ada tabrakan grid `12×12`, dan mencatat pasangan `FromConnectorIndex` ↔ `ToConnectorIndex` pada setiap `RoomEdgeData`.