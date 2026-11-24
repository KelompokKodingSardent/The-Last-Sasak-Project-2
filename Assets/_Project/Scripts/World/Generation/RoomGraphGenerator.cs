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