using System.Collections.Generic;
using UnityEngine;

public enum EncounterPresetType
{
    Basic,      // 3 Vanguard — mengasah ritme dodge & parry
    Pressure,   // 2 Vanguard + 1 Ranger — ancaman jarak dekat & jauh
    Flanking,   // 2 Skirmisher + 1 Vanguard — menguji kesadaran posisi 360 derajat
    Heavy,      // 1 Bulwark + 2 Vanguard — memecah guard & timing serangan
    Control,    // 1 Controller + 2 Skirmisher — prioritas target & kontrol zona
    Elite       // 1 Elite + 2 Vanguard/Skirmisher — uji ketahanan build
}

[System.Serializable]
public struct SpawnTagPreference
{
    public EnemyArchetype Archetype;
    public string PreferredSpawnTag;
}

[CreateAssetMenu(fileName = "NewEncounterComposition", menuName = "TLS/Roguelike/Encounter Composition")]
public class EncounterCompositionSO : ScriptableObject
{
    public string CompositionName = "Flanking Squad";
    public EncounterPresetType PresetType = EncounterPresetType.Flanking;
    public List<EnemyArchetype> Enemies = new List<EnemyArchetype>();

    [Header("Phase 03/05 — Room Compatibility & Depth Scaling")]
    [Min(1)] public int MinRunDepth = 1;
    [Min(1)] public int MaxRunDepth = 99;
    [Min(0.01f)] public float SelectionWeight = 1f;

    [Tooltip("Kosongkan jika encounter boleh muncul di semua jenis layout ruangan.")]
    public List<ArenaLayoutArchetype> AllowedLayouts = new List<ArenaLayoutArchetype>();

    [Tooltip("Tag yang wajib dimiliki RoomDefinitionSO (opsional).")]
    public List<string> RequiredRoomTags = new List<string>();

    [Header("Spawn Point Tag Preferences (Optional)")]
    public List<SpawnTagPreference> SpawnTagPreferences = new List<SpawnTagPreference>();

    public bool IsCompatibleWith(RoomDefinitionSO roomDef, int runDepth)
    {
        if (Enemies == null || Enemies.Count == 0)
            return false;

        if (runDepth < MinRunDepth || runDepth > MaxRunDepth)
            return false;

        if (roomDef == null)
            return true;

        if (AllowedLayouts != null && AllowedLayouts.Count > 0 && !AllowedLayouts.Contains(roomDef.LayoutArchetype))
            return false;

        if (RequiredRoomTags != null)
        {
            for (int i = 0; i < RequiredRoomTags.Count; i++)
            {
                if (!roomDef.HasTag(RequiredRoomTags[i]))
                    return false;
            }
        }

        return true;
    }

    public string GetPreferredTagFor(EnemyArchetype archetype)
    {
        if (SpawnTagPreferences == null)
            return string.Empty;

        for (int i = 0; i < SpawnTagPreferences.Count; i++)
        {
            if (SpawnTagPreferences[i].Archetype == archetype)
                return SpawnTagPreferences[i].PreferredSpawnTag;
        }

        return string.Empty;
    }

    public static EncounterCompositionSO CreatePreset(EncounterPresetType type)
    {
        EncounterCompositionSO comp = CreateInstance<EncounterCompositionSO>();
        comp.PresetType = type;
        comp.CompositionName = type.ToString();

        switch (type)
        {
            case EncounterPresetType.Basic:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Vanguard, EnemyArchetype.Vanguard, EnemyArchetype.Vanguard });
                break;

            case EncounterPresetType.Pressure:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Vanguard, EnemyArchetype.Vanguard, EnemyArchetype.Ranger });
                comp.SpawnTagPreferences.Add(new SpawnTagPreference { Archetype = EnemyArchetype.Ranger, PreferredSpawnTag = "RangedPosition" });
                break;

            case EncounterPresetType.Flanking:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Skirmisher, EnemyArchetype.Skirmisher, EnemyArchetype.Vanguard });
                break;

            case EncounterPresetType.Heavy:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Bulwark, EnemyArchetype.Vanguard, EnemyArchetype.Vanguard });
                comp.MinRunDepth = 2;
                break;

            case EncounterPresetType.Control:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Controller, EnemyArchetype.Skirmisher, EnemyArchetype.Skirmisher });
                comp.MinRunDepth = 2;
                break;

            case EncounterPresetType.Elite:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Elite, EnemyArchetype.Skirmisher, EnemyArchetype.Ranger });
                comp.MinRunDepth = 3;
                break;
        }

        return comp;
    }
}