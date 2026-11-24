using System;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Scene References (Auto-Resolved if Empty)")]
    public WorldGenerationCoordinator WorldCoordinator;
    public RoomEncounterController EncounterController;

    [Header("Run Progression State")]
    [Min(1)] [SerializeField] private int startingRunDepth = 1;

    public int RunDepth { get; private set; } = 1;
    public RoomController CurrentRoom { get; private set; }
    public GeneratedMapData ActiveMap { get; private set; }

    public event Action<int> OnRunDepthChanged;
    public event Action<RoomController> OnActiveRoomChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RunDepth = Mathf.Max(1, startingRunDepth);

        if (WorldCoordinator == null)
            WorldCoordinator = FindFirstObjectByType<WorldGenerationCoordinator>();

        if (EncounterController == null)
            EncounterController = FindFirstObjectByType<RoomEncounterController>();
    }

    private void OnEnable()
    {
        if (WorldCoordinator != null)
            WorldCoordinator.OnWorldGenerated += HandleWorldGenerated;

        GameEvents.OnRoomEntered += HandleRoomEntered;
        GameEvents.OnRoomInstanceCleared += HandleRoomInstanceCleared;
    }

    private void Start()
    {
        // Tangkap map jika WorldGenerationCoordinator sudah selesai generate di Start() lebih dulu
        if (WorldCoordinator != null && WorldCoordinator.CurrentMap != null && ActiveMap != WorldCoordinator.CurrentMap)
        {
            HandleWorldGenerated(WorldCoordinator.CurrentMap);
        }
    }

    private void OnDisable()
    {
        if (WorldCoordinator != null)
            WorldCoordinator.OnWorldGenerated -= HandleWorldGenerated;

        GameEvents.OnRoomEntered -= HandleRoomEntered;
        GameEvents.OnRoomInstanceCleared -= HandleRoomInstanceCleared;
    }

    private void HandleWorldGenerated(GeneratedMapData mapData)
    {
        ActiveMap = mapData;
        if (mapData == null)
            return;

        for (int i = 0; i < mapData.RoomList.Count; i++)
        {
            AssembledRoomInstance assembled = mapData.RoomList[i];
            if (assembled.Controller != null)
                assembled.Controller.InitializeFromGraph(assembled.NodeData);
        }

        if (mapData.StartRoom != null && mapData.StartRoom.Controller != null)
        {
            CurrentRoom = mapData.StartRoom.Controller;
            OnActiveRoomChanged?.Invoke(CurrentRoom);
        }
    }

    private void HandleRoomEntered(RoomController room)
    {
        if (room == null)
            return;

        if (CurrentRoom != room)
        {
            CurrentRoom = room;
            OnActiveRoomChanged?.Invoke(CurrentRoom);
        }
    }

    public void NotifyCombatRoomEntered(RoomController room, Transform playerTransform)
    {
        if (room == null || EncounterController == null)
            return;

        if (EncounterController.Player == null && playerTransform != null)
            EncounterController.Player = playerTransform;

        EncounterController.StartEncounter(room);
    }

    private void HandleRoomInstanceCleared(RoomController clearedRoom)
    {
        RoomController targetRoom = clearedRoom != null ? clearedRoom : CurrentRoom;
        if (targetRoom != null)
            targetRoom.CompleteRoomEncounter();
    }

    public bool AreAllMainPathCombatRoomsCleared()
    {
        if (ActiveMap == null)
            return true;

        for (int i = 0; i < ActiveMap.RoomList.Count; i++)
        {
            AssembledRoomInstance room = ActiveMap.RoomList[i];
            if (room.NodeData == null || !room.NodeData.IsMainPath)
                continue;

            if (room.NodeData.Role == RoomRole.Combat || room.NodeData.Role == RoomRole.Elite)
            {
                if (room.Controller != null && !room.Controller.Cleared)
                    return false;
            }
        }

        return true;
    }

    public void AdvanceToNextDepth()
    {
        RunDepth++;
        OnRunDepthChanged?.Invoke(RunDepth);

        if (WorldCoordinator != null)
        {
            WorldCoordinator.GenerateWorld();
        }
        else
        {
            Debug.Log($"[RoomManager] RunDepth meningkat ke {RunDepth} (mode non-prosedural).");
        }
    }

    public void ResetRun()
    {
        RunDepth = Mathf.Max(1, startingRunDepth);
        OnRunDepthChanged?.Invoke(RunDepth);

        if (WorldCoordinator != null)
            WorldCoordinator.GenerateWorld();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}