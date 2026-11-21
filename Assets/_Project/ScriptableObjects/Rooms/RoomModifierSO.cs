using UnityEngine;

public enum RoomModifierType
{
    Normal,
    BloodlustArena,     // Semua musuh di ruangan mendapat modifier Aggressive
    IronFortress,       // Semua musuh mendapat modifier Armored
    SwiftSkirmish,      // Semua musuh mendapat modifier Swift
    TacticalAmbush,     // Semua musuh mendapat modifier Tactical
    VolatileGround      // Arena memunculkan zona hazard berkala di sekitar player
}

[CreateAssetMenu(fileName = "NewRoomModifier", menuName = "TLS/Roguelike/Room Modifier")]
public class RoomModifierSO : ScriptableObject
{
    public string ModifierTitle = "Volatile Ground";
    [TextArea(2, 3)] public string Description = "Energi tidak stabil meledak secara berkala di lantai arena.";
    public RoomModifierType ModifierType = RoomModifierType.VolatileGround;

    [Header("Enemy Modifier Injection (Opsional)")]
    public bool ApplyEnemyModifierToAll = false;
    public EnemyModifierType InjectedEnemyModifier = EnemyModifierType.Aggressive;

    [Header("Environmental Hazard Settings")]
    public bool SpawnPeriodicHazards = false;
    public float HazardInterval = 4.5f;
    public float HazardRadius = 2.0f;
    public float HazardDuration = 2.8f;
    public float HazardDamagePerTick = 8f;

    public static RoomModifierSO CreateRuntime(RoomModifierType type)
    {
        RoomModifierSO mod = CreateInstance<RoomModifierSO>();
        mod.ModifierType = type;
        mod.ModifierTitle = type.ToString();

        switch (type)
        {
            case RoomModifierType.BloodlustArena:
                mod.ModifierTitle = "Bloodlust Arena";
                mod.Description = "Semua musuh bertarung lebih agresif dengan jeda serangan lebih singkat.";
                mod.ApplyEnemyModifierToAll = true;
                mod.InjectedEnemyModifier = EnemyModifierType.Aggressive;
                break;

            case RoomModifierType.IronFortress:
                mod.ModifierTitle = "Iron Fortress";
                mod.Description = "Musuh memiliki ketahanan stagger lebih tinggi dan perlindungan frontal.";
                mod.ApplyEnemyModifierToAll = true;
                mod.InjectedEnemyModifier = EnemyModifierType.Armored;
                break;

            case RoomModifierType.SwiftSkirmish:
                mod.ModifierTitle = "Swift Skirmish";
                mod.Description = "Musuh bergerak dan melakukan reposition jauh lebih cepat.";
                mod.ApplyEnemyModifierToAll = true;
                mod.InjectedEnemyModifier = EnemyModifierType.Swift;
                break;

            case RoomModifierType.TacticalAmbush:
                mod.ModifierTitle = "Tactical Ambush";
                mod.Description = "Musuh aktif mencari sudut flank dan berganti posisi setelah menyerang.";
                mod.ApplyEnemyModifierToAll = true;
                mod.InjectedEnemyModifier = EnemyModifierType.Tactical;
                break;

            case RoomModifierType.VolatileGround:
                mod.ModifierTitle = "Volatile Ground";
                mod.Description = "Zona hazard muncul di arena secara berkala selama pertarungan.";
                mod.SpawnPeriodicHazards = true;
                mod.HazardInterval = 4.5f;
                mod.HazardRadius = 2.0f;
                mod.HazardDuration = 2.8f;
                mod.HazardDamagePerTick = 8f;
                break;
        }

        return mod;
    }
}