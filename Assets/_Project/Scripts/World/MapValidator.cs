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