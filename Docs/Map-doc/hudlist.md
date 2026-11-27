Implementasi **Phase 11 — Debug Overlay & Graph Visualizer** menyediakan alat uji cepat di runtime (`OnGUI`, tanpa perlu setup Canvas UI) dan visualisasi topologi graph di Scene View agar kamu bisa menguji berbagai seed, memeriksa cabang jalur, dan memverifikasi alur `Start` → `Exit` dalam hitungan detik.

### 1. `WorldDebugHUD.cs`

Menampilkan panel kontrol di pojok kiri atas layar saat Play Mode (bisa disembunyikan/ditampilkan dengan tombol **`F1`**). Mendukung input seed manual, regenerasi seed acak, pembersihan encounter aktif secara instan untuk pengujian traversal pintu, serta teleportasi cepat ke ruang `Exit`.

```csharp
using UnityEngine;
using UnityEngine.AI;

public class WorldDebugHUD : MonoBehaviour
{
    [Header("References (Auto-Resolved if Empty)")]
    public WorldGenerationCoordinator WorldCoordinator;
    public RoomManager RoomManagerRef;

    [Header("HUD Settings")]
    public KeyCode ToggleKey = KeyCode.F1;
    public bool StartExpanded = true;

    private bool isExpanded;
    private string seedInputText = "1001";
    private string statusMessage = "Siap.";
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;

    private void Awake()
    {
        isExpanded = StartExpanded;

        if (WorldCoordinator == null)
            WorldCoordinator = FindFirstObjectByType<WorldGenerationCoordinator>();

        if (RoomManagerRef == null)
            RoomManagerRef = FindFirstObjectByType<RoomManager>();
    }

    private void OnEnable()
    {
        if (WorldCoordinator != null)
            WorldCoordinator.OnWorldGenerated += HandleWorldGenerated;
    }

    private void Start()
    {
        if (WorldCoordinator != null && WorldCoordinator.CurrentMap != null)
            HandleWorldGenerated(WorldCoordinator.CurrentMap);
    }

    private void OnDisable()
    {
        if (WorldCoordinator != null)
            WorldCoordinator.OnWorldGenerated -= HandleWorldGenerated;
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
            isExpanded = !isExpanded;
    }

    private void HandleWorldGenerated(GeneratedMapData mapData)
    {
        if (mapData == null)
            return;

        seedInputText = mapData.Seed.ToString();
        statusMessage = $"Generated ({mapData.RoomList.Count} rooms, attempt #{mapData.AttemptIndex + 1})";
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!isExpanded)
        {
            if (GUI.Button(new Rect(12f, 12f, 150f, 28f), $"World Debug [{ToggleKey}]"))
                isExpanded = true;
            return;
        }

        const float width = 320f;
        const float height = 345f;
        GUILayout.BeginArea(new Rect(12f, 12f, width, height), panelStyle);

        // Header Bar
        GUILayout.BeginHorizontal();
        GUILayout.Label($"THE LAST SASAK — WORLD DEBUG [{ToggleKey}]", headerStyle);
        if (GUILayout.Button("—", GUILayout.Width(28f)))
            isExpanded = false;
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        // Seed Input & Regeneration Controls
        GUILayout.BeginHorizontal();
        GUILayout.Label("Seed:", labelStyle, GUILayout.Width(45f));
        seedInputText = GUILayout.TextField(seedInputText, GUILayout.Width(135f));
        if (GUILayout.Button("Generate", GUILayout.Width(115f)))
            GenerateFromInputSeed();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Random Seed & Generate"))
            GenerateRandomSeed();

        if (GUILayout.Button("Reset Run", GUILayout.Width(95f)))
            ResetRun();
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);

        // Map & Topology Telemetry
        GeneratedMapData map = WorldCoordinator != null ? WorldCoordinator.CurrentMap : null;
        int depth = RoomManagerRef != null ? RoomManagerRef.RunDepth : 1;

        if (map != null && map.Graph != null)
        {
            int mainCount = 0;
            int branchCount = 0;
            for (int i = 0; i < map.Graph.Nodes.Count; i++)
            {
                if (map.Graph.Nodes[i].IsMainPath)
                    mainCount++;
                else
                    branchCount++;
            }

            GUILayout.Label($"Run Depth: {depth}   |   Active Seed: {map.Seed}", labelStyle);
            GUILayout.Label($"Total Rooms: {map.RoomList.Count} (Main: {mainCount}, Branch: {branchCount})", labelStyle);
            GUILayout.Label($"Graph Edges: {map.Graph.Edges.Count}", labelStyle);
        }
        else
        {
            GUILayout.Label($"Run Depth: {depth}   |   Map: Belum ter-generate", labelStyle);
        }

        GUILayout.Space(6f);

        // Current Active Room Info
        RoomController activeRoom = RoomManagerRef != null ? RoomManagerRef.CurrentRoom : null;
        if (activeRoom != null)
        {
            string encounterName = activeRoom.CuratedComposition != null
                ? activeRoom.CuratedComposition.CompositionName
                : "None";

            GUILayout.Label($"Current Room: #{activeRoom.NodeId:00} [{activeRoom.Role}]", labelStyle);
            GUILayout.Label($"Room State: {activeRoom.State} | Layout: {activeRoom.LayoutArchetype}", labelStyle);
            GUILayout.Label($"Encounter: {encounterName}", labelStyle);
        }
        else
        {
            GUILayout.Label("Current Room: Di luar trigger ruangan", labelStyle);
        }

        GUILayout.Space(8f);

        // QA Traversal Helpers
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Clear Room"))
            ForceClearCurrentRoom(activeRoom);

        if (GUILayout.Button("Teleport to Exit"))
            TeleportPlayerToExit(map);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label($"Status: {statusMessage}", labelStyle);

        GUILayout.EndArea();
    }

    private void GenerateFromInputSeed()
    {
        if (WorldCoordinator == null)
        {
            statusMessage = "WorldGenerationCoordinator tidak ditemukan.";
            return;
        }

        if (!int.TryParse(seedInputText, out int parsedSeed))
            parsedSeed = seedInputText.GetHashCode();

        bool ok = WorldCoordinator.GenerateWorld(parsedSeed);
        if (!ok)
            statusMessage = $"Gagal generate seed {parsedSeed}.";
    }

    private void GenerateRandomSeed()
    {
        if (WorldCoordinator == null)
        {
            statusMessage = "WorldGenerationCoordinator tidak ditemukan.";
            return;
        }

        int randomSeed = UnityEngine.Random.Range(1000, 999999);
        seedInputText = randomSeed.ToString();
        bool ok = WorldCoordinator.GenerateWorld(randomSeed);
        if (!ok)
            statusMessage = $"Gagal generate random seed {randomSeed}.";
    }

    private void ResetRun()
    {
        if (RoomManagerRef != null)
            RoomManagerRef.ResetRun();
        else if (WorldCoordinator != null)
            WorldCoordinator.GenerateWorld();
    }

    private void ForceClearCurrentRoom(RoomController activeRoom)
    {
        if (activeRoom == null)
        {
            statusMessage = "Tidak ada ruangan aktif untuk di-clear.";
            return;
        }

        GameEvents.RaiseRoomCleared(activeRoom);
        statusMessage = $"Room #{activeRoom.NodeId:00} dipaksa Cleared.";
    }

    private void TeleportPlayerToExit(GeneratedMapData map)
    {
        if (map == null || map.ExitRoom == null || WorldCoordinator == null || WorldCoordinator.PlayerTransform == null)
        {
            statusMessage = "ExitRoom atau PlayerTransform tidak tersedia.";
            return;
        }

        Vector3 dest = map.ExitRoom.GetWorldCenter();
        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            dest = hit.position;

        Transform player = WorldCoordinator.PlayerTransform;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        player.position = dest;

        if (cc != null)
            cc.enabled = true;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.Warp(dest);

        statusMessage = "Player dipindahkan ke ExitRoom.";
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.12f, 0.88f));
        bgTex.Apply();

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = bgTex },
            padding = new RectOffset(10, 10, 8, 8)
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 11,
            normal = { textColor = new Color(0.45f, 0.92f, 0.62f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
    }
}

```

---

### 2. `WorldGraphGizmoDrawer.cs`

Menggambar node ruangan, koneksi edge jalur utama vs cabang opsional, titik sambungan pintu aktif, dan label informasi node langsung di Scene View.

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldGraphGizmoDrawer : MonoBehaviour
{
    [Header("References")]
    public WorldGenerationCoordinator WorldCoordinator;

    [Header("Gizmo Display Toggles")]
    public bool DrawGraphOverlay = true;
    public bool DrawNodeLabels = true;
    public bool DrawConnectorLinks = true;
    public bool ColorByRuntimeState = false;

    [Header("Visual Offsets")]
    [SerializeField] private float verticalOffset = 3.5f;
    [SerializeField] private float nodeSphereRadius = 1.15f;

    private void OnValidate()
    {
        if (WorldCoordinator == null)
            WorldCoordinator = GetComponent<WorldGenerationCoordinator>();
    }

    private void OnDrawGizmos()
    {
        if (!DrawGraphOverlay)
            return;

        if (WorldCoordinator == null)
            WorldCoordinator = GetComponent<WorldGenerationCoordinator>();

        if (WorldCoordinator == null || WorldCoordinator.CurrentMap == null)
            return;

        GeneratedMapData map = WorldCoordinator.CurrentMap;
        RoomGraph graph = map.Graph;
        if (graph == null)
            return;

        // 1. Gambar Garis Koneksi Edge Antar-Ruangan
        for (int i = 0; i < graph.Edges.Count; i++)
        {
            RoomEdgeData edge = graph.Edges[i];
            if (!map.TryGetRoom(edge.FromNodeId, out AssembledRoomInstance fromRoom) ||
                !map.TryGetRoom(edge.ToNodeId, out AssembledRoomInstance toRoom))
            {
                continue;
            }

            Vector3 posA = fromRoom.GetWorldCenter() + Vector3.up * verticalOffset;
            Vector3 posB = toRoom.GetWorldCenter() + Vector3.up * verticalOffset;

            Color edgeColor = edge.IsMainPathEdge
                ? new Color(1f, 0.85f, 0.2f, 0.95f)   // Emas untuk jalur utama
                : new Color(0.25f, 0.85f, 1f, 0.9f);   // Biru muda untuk cabang opsional

            DrawThickLine(posA, posB, edgeColor, edge.IsMainPathEdge ? 4f : 2.5f);

            // Gambar titik tengah edge sebagai penanda arah koneksi
            Vector3 mid = Vector3.Lerp(posA, posB, 0.5f);
            Gizmos.color = edgeColor;
            Gizmos.DrawSphere(mid, 0.35f);
        }

        // 2. Gambar Node Ruangan & Label Metadata
        for (int i = 0; i < map.RoomList.Count; i++)
        {
            AssembledRoomInstance room = map.RoomList[i];
            if (room == null || room.NodeData == null)
                continue;

            Vector3 nodePos = room.GetWorldCenter() + Vector3.up * verticalOffset;
            Color nodeColor = ResolveNodeColor(room);

            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(nodePos, nodeSphereRadius);
            Gizmos.DrawLine(room.GetWorldCenter(), nodePos);

            if (DrawConnectorLinks && room.Connectors != null)
            {
                for (int c = 0; c < room.Connectors.Length; c++)
                {
                    RoomConnector conn = room.Connectors[c];
                    if (conn != null && conn.IsConnected)
                    {
                        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
                        Gizmos.DrawWireCube(conn.transform.position + Vector3.up * 2f, new Vector3(1.2f, 4f, 1.2f));
                    }
                }
            }

#if UNITY_EDITOR
            if (DrawNodeLabels)
            {
                string stateSuffix = Application.isPlaying && room.Controller != null
                    ? $"\n[{room.Controller.State}]"
                    : string.Empty;

                string pathTag = room.NodeData.IsMainPath ? "MAIN" : "BRANCH";
                string labelText = $"#{room.NodeId:00} {room.NodeData.Role} (D:{room.NodeData.Depth} | {pathTag}){stateSuffix}";

                GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = Color.white }
                };

                Handles.Label(nodePos + Vector3.up * 1.6f, labelText, style);
            }
#endif
        }
    }

    private Color ResolveNodeColor(AssembledRoomInstance room)
    {
        if (ColorByRuntimeState && Application.isPlaying && room.Controller != null)
        {
            switch (room.Controller.State)
            {
                case RoomState.ActiveCombat:
                    return new Color(1f, 0.2f, 0.2f, 0.95f);
                case RoomState.Cleared:
                case RoomState.RewardReady:
                    return new Color(0.2f, 0.9f, 0.4f, 0.95f);
                default:
                    return new Color(0.6f, 0.65f, 0.7f, 0.9f);
            }
        }

        switch (room.NodeData.Role)
        {
            case RoomRole.Start:
                return new Color(0.15f, 0.95f, 0.55f, 0.95f);
            case RoomRole.Exit:
                return new Color(0.95f, 0.25f, 0.65f, 0.95f);
            case RoomRole.Elite:
                return new Color(0.7f, 0.25f, 0.95f, 0.95f);
            case RoomRole.Reward:
                return new Color(1f, 0.8f, 0.15f, 0.95f);
            case RoomRole.Traversal:
                return new Color(0.35f, 0.75f, 0.95f, 0.95f);
            default:
                return new Color(0.95f, 0.42f, 0.2f, 0.95f);
        }
    }

    private static void DrawThickLine(Vector3 a, Vector3 b, Color color, float thickness)
    {
#if UNITY_EDITOR
        Color prev = Handles.color;
        Handles.color = color;
        Handles.DrawAAPolyLine(thickness, a, b);
        Handles.color = prev;
#else
        Gizmos.color = color;
        Gizmos.DrawLine(a, b);
#endif
    }
}

```

---

### Cara Pasang di Scene `WorldGenerationTest`

1. Pilih GameObject **`WorldSystems`** di Hierarchy.
2. Klik **Add Component** dan tambahkan **`WorldDebugHUD`** serta **`WorldGraphGizmoDrawer`**.
3. Saat kamu klik **Play**, panel debug langsung muncul di pojok kiri atas (tekan **`F1`** untuk buka/tutup) dan garis graph emas/biru beserta label node langsung terlihat di **Scene View**.