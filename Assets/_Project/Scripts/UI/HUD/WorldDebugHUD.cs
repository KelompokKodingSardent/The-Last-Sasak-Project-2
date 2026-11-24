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