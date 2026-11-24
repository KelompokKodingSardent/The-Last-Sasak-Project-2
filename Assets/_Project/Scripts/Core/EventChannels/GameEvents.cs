using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnPlayerDied;
    public static event Action OnRoomCleared;
    public static event Action<RoomController> OnRoomInstanceCleared;
    public static event Action<RoomController> OnRoomEntered;
    public static event Action<Health> OnEntityDied;
    public static event Action<int> OnCurrencyChanged;
    public static event Action<int, int> OnExperienceChanged;

    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();

    public static void RaiseRoomCleared()
    {
        OnRoomInstanceCleared?.Invoke(null);
        OnRoomCleared?.Invoke();
    }

    public static void RaiseRoomCleared(RoomController room)
    {
        OnRoomInstanceCleared?.Invoke(room);
        OnRoomCleared?.Invoke();
    }

    public static void RaiseRoomEntered(RoomController room) => OnRoomEntered?.Invoke(room);
    public static void RaiseEntityDied(Health health) => OnEntityDied?.Invoke(health);
    public static void RaiseCurrencyChanged(int amount) => OnCurrencyChanged?.Invoke(amount);
    public static void RaiseExperienceChanged(int current, int level) => OnExperienceChanged?.Invoke(current, level);

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
    }
}