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
                case RoomRole.Reward:
                    return RoomType.Start;
                case RoomRole.Exit:
                    return RoomType.Boss;
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