using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnPlayerDied;
    public static event Action<RoomController> OnRoomCleared;
    public static event Action<RoomController> OnRoomInstanceCleared;
    public static event Action<RoomController> OnRoomEntered;
    public static event Action<Health> OnEntityDied;
    public static event Action<int> OnCurrencyChanged;
    public static event Action<int, int> OnExperienceChanged;

    // Lifecycle Run Events (Phase 01 & 04)
    public static event Action<RunId, string> OnRunStarted;
    public static event Action<RunId, RunEndReason> OnRunEnded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEventsOnLoad()
    {
        ClearAll();
    }

    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaiseRoomCleared(RoomController room) => OnRoomCleared?.Invoke(room);
    public static void RaiseRoomInstanceCleared(RoomController room) => OnRoomInstanceCleared?.Invoke(room);
    public static void RaiseRoomEntered(RoomController room) => OnRoomEntered?.Invoke(room);
    public static void RaiseEntityDied(Health health) => OnEntityDied?.Invoke(health);
    public static void RaiseCurrencyChanged(int amount) => OnCurrencyChanged?.Invoke(amount);
    public static void RaiseExperienceChanged(int currentExp, int level) => OnExperienceChanged?.Invoke(currentExp, level);

    public static void RaiseRunStarted(RunId runId, string sceneName) => OnRunStarted?.Invoke(runId, sceneName);
    public static void RaiseRunEnded(RunId runId, RunEndReason reason) => OnRunEnded?.Invoke(runId, reason);

    public static void ClearAll()
    {
        OnGameStateChanged = null;
        OnPlayerDied = null;
        OnRoomCleared = null;
        OnRoomInstanceCleared = null;
        OnRoomEntered = null;
        OnEntityDied = null;
        OnCurrencyChanged = null;
        OnExperienceChanged = null;
        OnRunStarted = null;
        OnRunEnded = null;
    }
}