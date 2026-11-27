Implementasi **Phase 08 (Prefab Assembly & World Coordinator)** dan **Phase 09 (Map Validation & NavMesh Verification)** menghubungkan output dari `RoomGraphGenerator` dan `RoomPlacementSolver` menjadi dunia 3D utuh, menyambungkan setiap `RoomConnector`, melakukan baking `NavMeshSurface`, dan memverifikasi jalur berjalan dari `Start` ke `Exit` sebelum pemain mulai bergerak.

### 1. `GeneratedMapData.cs`

Menyimpan seluruh artefak hasil generasi satu run (graph topologi, solusi penempatan spasial, instance `RoomController` yang terpasang di scene, serta titik spawn pemain).

```csharp
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

```

---

### 2. `RoomPrefabAssembler.cs`

Melakukan instansiasi prefab ruangan sesuai `WorldPosition` dan `WorldRotation` dari `RoomPlacementSolution`, menutup seluruh `RoomConnector` secara default, lalu membuka pasangan `OpenFrame` hanya pada connector yang terhubung oleh `RoomEdgeData`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class RoomPrefabAssembler
{
    private const float ConnectorMatchTolerance = 0.2f;

    public bool TryAssemble(
        RoomGraph graph,
        RoomPlacementSolution solution,
        MapGenerationConfigSO config,
        int seed,
        int attemptIndex,
        Transform container,
        out GeneratedMapData mapData,
        out string failureReason)
    {
        mapData = new GeneratedMapData
        {
            Seed = seed,
            AttemptIndex = attemptIndex,
            Config = config,
            Graph = graph,
            PlacementSolution = solution
        };
        failureReason = string.Empty;

        if (graph == null || solution == null || container == null)
        {
            failureReason = "Parameter assembly (graph, solution, atau container) bernilai null.";
            return false;
        }

        // 1. Instansiasi seluruh prefab ruangan sesuai hasil PlacementSolver
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            RoomNodeData node = graph.Nodes[i];
            if (!solution.PlacedRooms.TryGetValue(node.NodeId, out PlacedRoomData placed) ||
                placed.RoomDef == null || placed.RoomDef.Prefab == null)
            {
                DestroyAssembledRooms(mapData);
                failureReason = $"Data penempatan atau prefab untuk NodeId={node.NodeId} tidak ditemukan.";
                return false;
            }

            GameObject instanceObj = Object.Instantiate(
                placed.RoomDef.Prefab,
                placed.WorldPosition,
                placed.WorldRotation,
                container
            );
            instanceObj.name = $"Room_{node.NodeId:00}_{node.Role}_{placed.RoomDef.RoomID}";

            RoomController controller = instanceObj.GetComponent<RoomController>();
            RoomFootprint footprint = instanceObj.GetComponent<RoomFootprint>();
            RoomConnector[] connectors = instanceObj.GetComponentsInChildren<RoomConnector>(true);

            // Reset semua connector menjadi tertutup (ClosedWall aktif, OpenFrame nonaktif)
            for (int c = 0; c < connectors.Length; c++)
                connectors[c].ResetConnection();

            // Pasang komposisi encounter jika node memiliki encounter
            if (controller != null && node.AssignedEncounter != null)
                controller.CuratedComposition = node.AssignedEncounter;

            AssembledRoomInstance assembled = new AssembledRoomInstance
            {
                NodeId = node.NodeId,
                NodeData = node,
                PlacementData = placed,
                RoomObject = instanceObj,
                Controller = controller,
                Footprint = footprint,
                Connectors = connectors
            };

            mapData.RoomList.Add(assembled);
            mapData.RoomsByNodeId[node.NodeId] = assembled;

            if (node.NodeId == graph.StartNodeId)
                mapData.StartRoom = assembled;
            else if (node.NodeId == graph.ExitNodeId)
                mapData.ExitRoom = assembled;

            // Sinkronkan juga ke legacy list RoomGraph.Rooms
            RoomPlacement legacyPlacement = graph.Add(
                RoomFootprint.WorldToGrid(placed.WorldPosition),
                MapRoleToLegacyType(node.Role)
            );
            legacyPlacement.WorldPosition = placed.WorldPosition;
            legacyPlacement.Controller = controller;
        }

        // 2. Hubungkan pasangan RoomConnector berdasarkan RoomEdgeData
        for (int e = 0; e < graph.Edges.Count; e++)
        {
            RoomEdgeData edge = graph.Edges[e];
            if (!mapData.TryGetRoom(edge.FromNodeId, out AssembledRoomInstance roomA) ||
                !mapData.TryGetRoom(edge.ToNodeId, out AssembledRoomInstance roomB))
            {
                DestroyAssembledRooms(mapData);
                failureReason = $"Edge ({edge.FromNodeId} <-> {edge.ToNodeId}) merujuk node yang tidak terpasang.";
                return false;
            }

            if (!TryBindEdgeConnectors(edge, roomA, roomB, out string edgeError))
            {
                DestroyAssembledRooms(mapData);
                failureReason = edgeError;
                return false;
            }
        }

        // 3. Tentukan titik spawn pemain pada StartRoom
        if (mapData.StartRoom != null)
        {
            if (mapData.StartRoom.Controller != null && mapData.StartRoom.Controller.PlayerSpawn != null)
            {
                Transform spawnAnchor = mapData.StartRoom.Controller.PlayerSpawn;
                mapData.PlayerSpawnPosition = spawnAnchor.position;
                mapData.PlayerSpawnRotation = spawnAnchor.rotation;
            }
            else
            {
                mapData.PlayerSpawnPosition = mapData.StartRoom.GetWorldCenter();
                mapData.PlayerSpawnRotation = mapData.StartRoom.RoomObject.transform.rotation;
            }
        }

        return true;
    }

    private bool TryBindEdgeConnectors(
        RoomEdgeData edge,
        AssembledRoomInstance roomA,
        AssembledRoomInstance roomB,
        out string error)
    {
        error = string.Empty;

        RoomConnector connA = GetConnectorSafe(roomA.Connectors, edge.FromConnectorIndex);
        RoomConnector connB = GetConnectorSafe(roomB.Connectors, edge.ToConnectorIndex);

        if (connA == null || connB == null)
        {
            error = $"Indeks connector tidak valid pada edge ({edge.FromNodeId} [{edge.FromConnectorIndex}] <-> {edge.ToNodeId} [{edge.ToConnectorIndex}]).";
            return false;
        }

        float distance = Vector3.Distance(connA.transform.position, connB.transform.position);
        if (distance > ConnectorMatchTolerance)
        {
            error = $"Connector '{connA.name}' dan '{connB.name}' tidak sejajar (jarak = {distance:F3}u).";
            return false;
        }

        if (!connA.IsCompatibleWith(connB))
        {
            error = $"Connector '{connA.name}' dan '{connB.name}' tidak kompatibel atau arah hadap tidak berlawanan.";
            return false;
        }

        connA.SetConnected(connB);
        connB.SetConnected(connA);
        return true;
    }

    private static RoomConnector GetConnectorSafe(RoomConnector[] connectors, int index)
    {
        if (connectors == null || index < 0 || index >= connectors.Length)
            return null;
        return connectors[index];
    }

    private static RoomType MapRoleToLegacyType(RoomRole role)
    {
        switch (role)
        {
            case RoomRole.Start:
                return RoomType.Start;
            case RoomRole.Exit:
                return RoomType.Boss;
            case RoomRole.Reward:
                return RoomType.Reward;
            default:
                return RoomType.Combat;
        }
    }

    public static void DestroyAssembledRooms(GeneratedMapData mapData)
    {
        if (mapData == null)
            return;

        for (int i = mapData.RoomList.Count - 1; i >= 0; i--)
        {
            GameObject obj = mapData.RoomList[i].RoomObject;
            if (obj == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        mapData.RoomList.Clear();
        mapData.RoomsByNodeId.Clear();
    }
}

```

---

### 3. `MapValidator.cs`

Menjalankan audit pasca-assembly sebelum pemain memasuki map: memeriksa kelengkapan node, kecocokan sambungan connector, penutupan dinding perimeter, kelayakan spawn musuh, serta melakukan baking `NavMeshSurface` dan menguji `NavMesh.CalculatePath` dari ruang `Start` ke seluruh ruangan (terutama `Exit`).

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MapValidationReport
{
    public bool IsValid => Errors.Count == 0;
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public string BuildSummary()
    {
        if (IsValid)
            return $"[MapValidator] PASS ({Warnings.Count} warning).";

        return $"[MapValidator] FAIL ({Errors.Count} error, {Warnings.Count} warning): {string.Join(" | ", Errors)}";
    }
}

public class MapValidator
{
    private const float NavMeshCheckRadius = 3.0f;

    public MapValidationReport ValidateAndBake(
        GeneratedMapData mapData,
        RuntimeNavMeshBuilder navMeshBuilder)
    {
        MapValidationReport report = new MapValidationReport();

        if (mapData == null || mapData.Graph == null)
        {
            report.Errors.Add("GeneratedMapData atau RoomGraph bernilai null.");
            return report;
        }

        // 1. Validasi Keberadaan Start & Exit Room
        if (mapData.StartRoom == null || mapData.StartRoom.RoomObject == null)
            report.Errors.Add("StartRoom tidak ditemukan pada hasil assembly.");

        if (mapData.ExitRoom == null || mapData.ExitRoom.RoomObject == null)
            report.Errors.Add("ExitRoom tidak ditemukan pada hasil assembly.");

        // 2. Validasi Setiap Ruangan Terpasang & Socket Perimeter
        for (int i = 0; i < mapData.RoomList.Count; i++)
        {
            AssembledRoomInstance room = mapData.RoomList[i];
            if (room.RoomObject == null)
            {
                report.Errors.Add($"RoomObject untuk NodeId={room.NodeId} hilang.");
                continue;
            }

            // Cek apakah jumlah connector aktif sesuai dengan derajat node pada graph
            int connectedCount = 0;
            if (room.Connectors != null)
            {
                for (int c = 0; c < room.Connectors.Length; c++)
                {
                    RoomConnector conn = room.Connectors[c];
                    if (conn.IsConnected)
                    {
                        connectedCount++;
                        if (conn.OpenFrame == null || !conn.OpenFrame.activeSelf)
                            report.Errors.Add($"Connector terhubung '{conn.name}' pada '{room.RoomObject.name}' gagal mengaktifkan OpenFrame.");
                    }
                    else
                    {
                        if (conn.ClosedWall == null || !conn.ClosedWall.activeSelf)
                            report.Errors.Add($"Connector tertutup '{conn.name}' pada '{room.RoomObject.name}' tidak mengaktifkan ClosedWall (risiko lubang ke void).");
                    }
                }
            }

            if (connectedCount != room.NodeData.Degree)
            {
                report.Errors.Add($"Jumlah connector aktif ({connectedCount}) pada '{room.RoomObject.name}' tidak sama dengan graph degree ({room.NodeData.Degree}).");
            }

            // Cek kapasitas titik spawn musuh pada ruangan Combat/Elite
            if ((room.NodeData.Role == RoomRole.Combat || room.NodeData.Role == RoomRole.Elite) &&
                room.Controller != null && room.NodeData.AssignedEncounter != null)
            {
                if (!RoomEncounterController.CanRoomSupportEncounter(room.Controller, room.NodeData.AssignedEncounter))
                {
                    report.Warnings.Add($"Ruangan '{room.RoomObject.name}' kekurangan EnemySpawnPoint untuk encounter '{room.NodeData.AssignedEncounter.CompositionName}'.");
                }
            }
        }

        if (!report.IsValid)
            return report;

        // 3. Bake Runtime NavMesh & Validasi Keterjangkauan Jalur (Start -> Exit & Semua Ruangan)
        if (navMeshBuilder == null)
        {
            report.Warnings.Add("RuntimeNavMeshBuilder tidak di-assign; validasi NavMesh dilewati.");
            return report;
        }

        if (!navMeshBuilder.BakeNavMeshRuntime())
        {
            report.Errors.Add("RuntimeNavMeshBuilder gagal membangun NavMeshSurface.");
            return report;
        }

        if (!navMeshBuilder.TrySamplePosition(mapData.PlayerSpawnPosition, NavMeshCheckRadius, out Vector3 navStartPos))
        {
            report.Errors.Add($"Titik PlayerSpawn ({mapData.PlayerSpawnPosition}) tidak berada di atas NavMesh.");
            return report;
        }

        mapData.PlayerSpawnPosition = navStartPos;
        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < mapData.RoomList.Count; i++)
        {
            AssembledRoomInstance targetRoom = mapData.RoomList[i];
            if (targetRoom == mapData.StartRoom)
                continue;

            Vector3 targetRawPos = targetRoom.GetWorldCenter();
            if (!navMeshBuilder.TrySamplePosition(targetRawPos, NavMeshCheckRadius, out Vector3 navTargetPos))
            {
                report.Errors.Add($"Area tengah ruangan '{targetRoom.RoomObject.name}' tidak memiliki permukaan NavMesh.");
                continue;
            }

            bool pathFound = NavMesh.CalculatePath(navStartPos, navTargetPos, NavMesh.AllAreas, path);
            if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
            {
                report.Errors.Add($"Jalur NavMesh dari StartRoom menuju '{targetRoom.RoomObject.name}' terputus (Status: {path.status}).");
            }
        }

        return report;
    }
}

```

---

### 4. `WorldGenerationCoordinator.cs`

Komponen utama pada scene `WorldGenerationTest` (dan level prosedural berikutnya). Mengelola siklus penuh: pembersihan dunia lama, loop retry deterministik jika terjadi tabrakan spasial, pemanggilan `MapValidator`, sinkronisasi seed ke `RoomEncounterController`, serta penempatan aman objek `Player` ke `StartRoom`.

```csharp
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

```

---

## Panduan Setup Scene `WorldGenerationTest.unity`

1. Buat scene baru bernama **`WorldGenerationTest`** di `Assets/_Project/Scenes/`.
2. Tambahkan tiga root GameObject utama:
* **`WorldSystems`**: Pasang **`WorldGenerationCoordinator`**, **`RuntimeNavMeshBuilder`** (`BakeOnStart = false`), **`RoomEncounterController`** (`SpawnFromPool = true`), dan **`EnemyPool`** / **`ObjectPoolManager`**.
* **`GeneratedWorld_Container`**: Empty Transform di `(0, 0, 0)` dan hubungkan ke `MapContainer` pada `WorldGenerationCoordinator`.
* **`Player`** (dan kamera / HUD combat dari `CombatTest`): Hubungkan ke `PlayerTransform` pada `WorldGenerationCoordinator` serta `Player` pada `RoomEncounterController`.


3. Assign asset `MapGenConfig_Forest` ke field `Config` pada `WorldGenerationCoordinator`.
4. Klik tombol **Play** (atau klik kanan komponen `WorldGenerationCoordinator` di Edit Mode → **`Generate World Now`**) untuk melihat ruangan terangkai otomatis, doorway tersambung mulus, dan NavMesh hijau ter-bake di seluruh jalur `Start` → `Exit`.