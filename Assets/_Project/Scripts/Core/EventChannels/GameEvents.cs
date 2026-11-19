using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnPlayerDied;
    public static event Action OnRoomCleared;
    public static event Action<Health> OnEntityDied;
    public static event Action<int> OnCurrencyChanged;
    public static event Action<int, int> OnExperienceChanged;

    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaiseRoomCleared() => OnRoomCleared?.Invoke();
    public static void RaiseEntityDied(Health health) => OnEntityDied?.Invoke(health);
    public static void RaiseCurrencyChanged(int amount) => OnCurrencyChanged?.Invoke(amount);
    public static void RaiseExperienceChanged(int current, int level) => OnExperienceChanged?.Invoke(current, level);

    public static void ClearAll()
    {
        OnGameStateChanged = null;
        OnPlayerDied = null;
        OnRoomCleared = null;
        OnEntityDied = null;
        OnCurrencyChanged = null;
        OnExperienceChanged = null;
    }
}
