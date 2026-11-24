using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomValidationReport
{
    public string RoomName;
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public bool IsValid => Errors.Count == 0;
}

public static class RoomPrefabValidator
{
    private const float PositionTolerance = 0.05f;
    private const float MinDoorwayWidth = 4.0f;
    private const float MinSpawnClearanceFromDoor = 2.5f;

    public static RoomValidationReport Validate(GameObject roomObject)
    {
        RoomValidationReport report = new RoomValidationReport
        {
            RoomName = roomObject != null ? roomObject.name : "NullRoom"
        };

        if (roomObject == null)
        {
            report.Errors.Add("GameObject ruangan bernilai null.");
            return report;
        }

        // 1. Cek Komponen Wajib pada Root
        RoomController controller = roomObject.GetComponent<RoomController>();
        if (controller == null)
            report.Errors.Add("Root prefab tidak memiliki komponen RoomController.");

        RoomFootprint footprint = roomObject.GetComponent<RoomFootprint>();
        if (footprint == null)
        {
            report.Errors.Add("Root prefab tidak memiliki komponen RoomFootprint.");
            return report;
        }

        if (footprint.WidthCells < 1 || footprint.DepthCells < 1)
            report.Errors.Add($"Ukuran cell tidak valid: {footprint.WidthCells}x{footprint.DepthCells}. Minimal 1x1.");

        float maxX = footprint.WidthCells * RoomFootprint.CellSize;
        float maxZ = footprint.DepthCells * RoomFootprint.CellSize;

        // 2. Cek Floor Collider
        Collider[] colliders = roomObject.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
            report.Errors.Add("Tidak ditemukan Collider sama sekali (lantai/dinding belum memiliki collision).");

        // 3. Cek PlayerSpawn Anchor
        if (controller != null)
        {
            if (controller.PlayerSpawn == null)
            {
                report.Warnings.Add("PlayerSpawn belum di-assign pada RoomController.");
            }
            else
            {
                Vector3 localSpawn = roomObject.transform.InverseTransformPoint(controller.PlayerSpawn.position);
                if (!footprint.IsLocalPointInside(localSpawn))
                    report.Errors.Add($"PlayerSpawn berada di luar area footprint ({localSpawn}).");
                if (Mathf.Abs(localSpawn.y) > 0.5f)
                    report.Warnings.Add($"Elevasi PlayerSpawn (Y={localSpawn.y:F2}) menyimpang dari standar lantai Y=0.");
            }
        }

        // 4. Cek RoomConnectors
        RoomConnector[] connectors = roomObject.GetComponentsInChildren<RoomConnector>(true);
        if (connectors.Length == 0)
        {
            report.Errors.Add("Ruangan tidak memiliki RoomConnector sama sekali.");
        }
        else
        {
            HashSet<Vector2> uniqueConnectorCoords = new HashSet<Vector2>();

            for (int i = 0; i < connectors.Length; i++)
            {
                RoomConnector conn = connectors[i];
                Vector3 localPos = roomObject.transform.InverseTransformPoint(conn.transform.position);
                Vector3 localFwd = roomObject.transform.InverseTransformDirection(conn.transform.forward).normalized;

                // Cek Y = 0
                if (Mathf.Abs(localPos.y) > PositionTolerance)
                    report.Errors.Add($"Connector '{conn.name}' memiliki Y={localPos.y:F2} (wajib 0.0).");

                // Cek duplikasi posisi connector
                Vector2 roundedPos = new Vector2(Mathf.Round(localPos.x * 10f) / 10f, Mathf.Round(localPos.z * 10f) / 10f);
                if (!uniqueConnectorCoords.Add(roundedPos))
                    report.Errors.Add($"Connector '{conn.name}' bertumpuk pada koordinat yang sama {roundedPos}.");

                // Cek posisi berada tepat di tepi perimeter & midpoint cell (6, 18, 30...)
                bool onWestEdge = Mathf.Abs(localPos.x - 0f) <= PositionTolerance;
                bool onEastEdge = Mathf.Abs(localPos.x - maxX) <= PositionTolerance;
                bool onSouthEdge = Mathf.Abs(localPos.z - 0f) <= PositionTolerance;
                bool onNorthEdge = Mathf.Abs(localPos.z - maxZ) <= PositionTolerance;

                if (!onWestEdge && !onEastEdge && !onSouthEdge && !onNorthEdge)
                {
                    report.Errors.Add($"Connector '{conn.name}' ({localPos.x:F2}, {localPos.z:F2}) tidak berada di tepi luar footprint ({maxX}x{maxZ}).");
                }
                else
                {
                    if (onWestEdge || onEastEdge)
                    {
                        if (!IsCellMidpoint(localPos.z, footprint.DepthCells))
                            report.Errors.Add($"Connector '{conn.name}' Z={localPos.z:F2} tidak berada di titik tengah cell (6, 18, 30...).");

                        Vector3 expectedFwd = onEastEdge ? Vector3.right : Vector3.left;
                        if (Vector3.Dot(localFwd, expectedFwd) < 0.98f)
                            report.Errors.Add($"Connector '{conn.name}' arah forward ({localFwd}) harus menghadap keluar ({expectedFwd}).");
                    }

                    if (onSouthEdge || onNorthEdge)
                    {
                        if (!IsCellMidpoint(localPos.x, footprint.WidthCells))
                            report.Errors.Add($"Connector '{conn.name}' X={localPos.x:F2} tidak berada di titik tengah cell (6, 18, 30...).");

                        Vector3 expectedFwd = onNorthEdge ? Vector3.forward : Vector3.back;
                        if (Vector3.Dot(localFwd, expectedFwd) < 0.98f)
                            report.Errors.Add($"Connector '{conn.name}' arah forward ({localFwd}) harus menghadap keluar ({expectedFwd}).");
                    }
                }

                // Cek dimensi bukaan & kelengkapan OpenFrame/ClosedWall
                if (conn.OpeningWidth < MinDoorwayWidth)
                    report.Warnings.Add($"Connector '{conn.name}' memiliki OpeningWidth {conn.OpeningWidth}u (disarankan >= 5u).");

                if (conn.OpenFrame == null)
                    report.Errors.Add($"Connector '{conn.name}' belum mengisi referensi OpenFrame.");

                if (conn.ClosedWall == null)
                    report.Errors.Add($"Connector '{conn.name}' belum mengisi referensi ClosedWall.");

                if (conn.OpenFrame != null && conn.OpenFrame == conn.ClosedWall)
                    report.Errors.Add($"Connector '{conn.name}' menggunakan GameObject yang sama untuk OpenFrame dan ClosedWall.");
            }
        }

        // 5. Cek EnemySpawnPoints
        EnemySpawnPoint[] spawnPoints = roomObject.GetComponentsInChildren<EnemySpawnPoint>(true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            EnemySpawnPoint sp = spawnPoints[i];
            Vector3 localSp = roomObject.transform.InverseTransformPoint(sp.transform.position);

            if (!footprint.IsLocalPointInside(localSp, -0.5f))
                report.Errors.Add($"EnemySpawnPoint '{sp.name}' berada di luar batas ruangan.");

            for (int c = 0; c < connectors.Length; c++)
            {
                float distToDoor = Vector3.Distance(sp.transform.position, connectors[c].transform.position);
                if (distToDoor < MinSpawnClearanceFromDoor)
                    report.Warnings.Add($"EnemySpawnPoint '{sp.name}' terlalu dekat ({distToDoor:F1}u) dengan connector '{connectors[c].name}'.");
            }
        }

        return report;
    }

    private static bool IsCellMidpoint(float coord, int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            float expectedMidpoint = (i + 0.5f) * RoomFootprint.CellSize;
            if (Mathf.Abs(coord - expectedMidpoint) <= PositionTolerance)
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    [MenuItem("TLS/World/Validate Selected Room Prefabs")]
    private static void ValidateSelectedPrefabs()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[RoomPrefabValidator] Pilih minimal satu Room Prefab atau GameObject di Project/Hierarchy.");
            return;
        }

        for (int i = 0; i < selected.Length; i++)
        {
            RoomValidationReport report = Validate(selected[i]);
            if (report.IsValid)
            {
                Debug.Log($"<color=#4CAF50>[PASS]</color> <b>{report.RoomName}</b> lolos validasi modular ({report.Warnings.Count} warning).");
            }
            else
            {
                Debug.LogError($"<color=#F44336>[FAIL]</color> <b>{report.RoomName}</b> gagal validasi dengan {report.Errors.Count} error:");
                foreach (string err in report.Errors)
                    Debug.LogError($"   • {err}", selected[i]);
            }

            foreach (string warn in report.Warnings)
                Debug.LogWarning($"   ⚠ [{report.RoomName}] {warn}", selected[i]);
        }
    }
#endif
}