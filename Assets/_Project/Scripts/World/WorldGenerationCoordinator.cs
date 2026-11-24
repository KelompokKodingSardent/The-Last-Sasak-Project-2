using System;
using UnityEngine;
using UnityEngine.AI;

public class WorldGenerationCoordinator : MonoBehaviour
{
    [Header("Configuration")]
    public MapGenerationConfigSO Config;

    [Header("Scene References")]
    [Tooltip("Parent container tempat seluruh prefab ruangan akan di-instantiate.")]
    public Transform MapContainer;
    public RuntimeNavMeshBuilder NavMeshBuilder;
    public RoomEncounterController EncounterController;
    [Tooltip("Opsional: Jika diisi, Player akan dipindahkan ke titik StartRoom setelah NavMesh siap.")]
    public Transform PlayerTransform;

    [Header("Execution Settings")]
    public bool GenerateOnStart = true;
    [Range(1, 15)] public int MaxPipelineRetries = 6;
    public bool VerboseLogging = true;

    [Header("Runtime Debug Info (Read Only)")]
    [SerializeField] private int lastUsedSeed;
    [SerializeField] private int lastGeneratedRoomCount;
    [SerializeField] private bool lastGenerationSucceeded;

    public int LastUsedSeed => lastUsedSeed;
    public int LastGeneratedRoomCount => lastGeneratedRoomCount;
    public bool LastGenerationSucceeded => lastGenerationSucceeded;
    public GeneratedMapData CurrentMap { get; private set; }
    public event Action<GeneratedMapData> OnWorldGenerated;

    private readonly RoomGraphGenerator graphGenerator = new RoomGraphGenerator();
    private readonly RoomPlacementSolver placementSolver = new RoomPlacementSolver();
    private readonly RoomPrefabAssembler prefabAssembler = new RoomPrefabAssembler();
    private readonly MapValidator mapValidator = new MapValidator();

    private void Start()
    {
        if (GenerateOnStart)
            GenerateWorld();
    }

    [ContextMenu("Generate World Now")]
    public bool GenerateWorld()
    {
        if (Config == null)
        {
            Debug.LogError("[WorldGenerationCoordinator] MapGenerationConfigSO belum di-assign.", this);
            return false;
        }

        int baseSeed = Config.ResolveSeed();
        return GenerateWorld(baseSeed);
    }

    public bool GenerateWorld(int baseSeed)
    {
        if (Config == null || Config.RoomCatalog == null)
        {
            Debug.LogError("[WorldGenerationCoordinator] Config atau RoomCatalog belum lengkap.", this);
            return false;
        }

        EnsureMapContainer();

        // Validasi katalog prefab terlebih dahulu
        if (!Config.RoomCatalog.ValidateAll(out var catalogErrors))
        {
            Debug.LogError($"[WorldGenerationCoordinator] RoomCatalog '{Config.RoomCatalog.name}' gagal validasi:\n• {string.Join("\n• ", catalogErrors)}", this);
            lastGenerationSucceeded = false;
            return false;
        }

        int retries = Mathf.Max(1, MaxPipelineRetries);

        for (int attempt = 0; attempt < retries; attempt++)
        {
            int attemptSeed = attempt == 0 ? baseSeed : unchecked(baseSeed + (attempt * 7919));
            ClearWorld();

            // Phase 06 — Layer 1: Bangun Topologi Graph
            if (!graphGenerator.TryGenerate(Config, attemptSeed, out RoomGraph graph, out string graphReason))
            {
                if (VerboseLogging)
                    Debug.LogWarning($"[WorldGen] Attempt {attempt + 1}/{retries} (Seed {attemptSeed}) gagal di GraphGenerator: {graphReason}");
                continue;
            }

            // Phase 07 — Layer 2: Selesaikan Penempatan Grid 12x12 & Connector Alignment
            if (!placementSolver.TrySolve(graph, Config, attemptSeed, out RoomPlacementSolution solution, out string solveReason))
            {
                if (VerboseLogging)
                    Debug.LogWarning($"[WorldGen] Attempt {attempt + 1}/{retries} (Seed {attemptSeed}) gagal di PlacementSolver: {solveReason}");
                continue;
            }

            // Phase 08 — Assembly Prefab & Sambungkan Doorway
            if (!prefabAssembler.TryAssemble(graph, solution, Config, attemptSeed, attempt, MapContainer, out GeneratedMapData assembledMap, out string assembleReason))
            {
                if (VerboseLogging)
                    Debug.LogWarning($"[WorldGen] Attempt {attempt + 1}/{retries} (Seed {attemptSeed}) gagal di PrefabAssembler: {assembleReason}");
                continue;
            }

            // Phase 09 — Validasi Struktural, Bake NavMesh & Uji Pathfinding Start -> Exit
            MapValidationReport validationReport = mapValidator.ValidateAndBake(assembledMap, NavMeshBuilder);
            if (!validationReport.IsValid)
            {
                if (VerboseLogging)
                    Debug.LogWarning($"[WorldGen] Attempt {attempt + 1}/{retries} (Seed {attemptSeed}) gagal validasi:\n{validationReport.BuildSummary()}");

                RoomPrefabAssembler.DestroyAssembledRooms(assembledMap);
                if (NavMeshBuilder != null)
                    NavMeshBuilder.ClearNavMesh();
                continue;
            }

            // Berhasil menyusun & memvalidasi dunia
            CurrentMap = assembledMap;
            lastUsedSeed = attemptSeed;
            lastGeneratedRoomCount = assembledMap.RoomList.Count;
            lastGenerationSucceeded = true;

            if (EncounterController != null)
                EncounterController.SetSeed(attemptSeed);

            PositionPlayerAtStart(assembledMap);

            if (VerboseLogging)
            {
                Debug.Log($"<color=#4CAF50>[WorldGen SUCCESS]</color> Seed={attemptSeed} | Rooms={assembledMap.RoomList.Count} | Edges={graph.Edges.Count} | Attempt={attempt + 1}");
            }

            OnWorldGenerated?.Invoke(CurrentMap);
            return true;
        }

        lastGenerationSucceeded = false;
        Debug.LogError($"[WorldGenerationCoordinator] Gagal membangkitkan dunia setelah {retries} percobaan (BaseSeed={baseSeed}). Periksa variasi footprint dan jumlah connector pada RoomCatalog.", this);
        return false;
    }

    [ContextMenu("Clear World")]
    public void ClearWorld()
    {
        if (EncounterController != null)
            EncounterController.ClearTracking();

        if (CurrentMap != null)
        {
            RoomPrefabAssembler.DestroyAssembledRooms(CurrentMap);
            CurrentMap = null;
        }

        if (MapContainer != null)
        {
            for (int i = MapContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = MapContainer.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        if (NavMeshBuilder != null)
            NavMeshBuilder.ClearNavMesh();
    }

    private void EnsureMapContainer()
    {
        if (MapContainer != null)
            return;

        GameObject containerObj = new GameObject("GeneratedWorld_Container");
        containerObj.transform.SetParent(transform, false);
        MapContainer = containerObj.transform;
    }

    private void PositionPlayerAtStart(GeneratedMapData mapData)
    {
        if (PlayerTransform == null || mapData == null)
            return;

        Vector3 targetPos = mapData.PlayerSpawnPosition;
        Quaternion targetRot = mapData.PlayerSpawnRotation;

        CharacterController charController = PlayerTransform.GetComponent<CharacterController>();
        if (charController != null)
            charController.enabled = false;

        PlayerTransform.SetPositionAndRotation(targetPos, targetRot);

        if (charController != null)
            charController.enabled = true;

        NavMeshAgent agent = PlayerTransform.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(targetPos);
            if (agent.isOnNavMesh)
                agent.ResetPath();
        }
    }
}