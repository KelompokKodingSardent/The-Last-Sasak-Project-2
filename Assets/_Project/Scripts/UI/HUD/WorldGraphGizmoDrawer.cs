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