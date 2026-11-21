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

[CreateAssetMenu(fileName = "NewEncounterComposition", menuName = "TLS/Roguelike/Encounter Composition")]
public class EncounterCompositionSO : ScriptableObject
{
    public string CompositionName = "Flanking Squad";
    public EncounterPresetType PresetType = EncounterPresetType.Flanking;
    public List<EnemyArchetype> Enemies = new List<EnemyArchetype>();

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
                break;

            case EncounterPresetType.Flanking:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Skirmisher, EnemyArchetype.Skirmisher, EnemyArchetype.Vanguard });
                break;

            case EncounterPresetType.Heavy:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Bulwark, EnemyArchetype.Vanguard, EnemyArchetype.Vanguard });
                break;

            case EncounterPresetType.Control:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Controller, EnemyArchetype.Skirmisher, EnemyArchetype.Skirmisher });
                break;

            case EncounterPresetType.Elite:
                comp.Enemies.AddRange(new[] { EnemyArchetype.Elite, EnemyArchetype.Skirmisher, EnemyArchetype.Ranger });
                break;
        }

        return comp;
    }
}