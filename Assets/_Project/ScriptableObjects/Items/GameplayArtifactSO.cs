using UnityEngine;

public enum ArtifactPlaystyleType
{
    AggressiveCombo,    // Kombo beruntun meningkatkan damage secara progresif
    ParryCounter,       // Parry menghasilkan bonus stagger besar + buff serangan balik
    DodgeMobility,      // Dodge memberikan burst damage counter sementara
    StaggerBreaker,     // Damage ekstra besar ke musuh yang sedang Staggered
    CriticalExecution,  // Bonus critical saat menyerang dari belakang / saat musuh recovery
    SoulRecovery        // Memulihkan HP saat membunuh musuh atau menyelesaikan ruangan
}

[CreateAssetMenu(fileName = "NewGameplayArtifact", menuName = "TLS/Roguelike/Gameplay Artifact")]
public class GameplayArtifactSO : ScriptableObject
{
    [Header("Identitas Artefak")]
    public string ArtifactName = "Sangkep Counter Crest";
    [TextArea(2, 3)] public string Description = "Sukses Parry memicu ledakan stagger dan memperkuat serangan berikutnya.";
    public ArtifactPlaystyleType Playstyle = ArtifactPlaystyleType.ParryCounter;

    [Header("Bonus Dasar (Opsional)")]
    [Min(0.5f)] public float BaseDamageMultiplier = 1.0f;
    public float MaxHealthBonus = 0f;

    [Header("Parry & Dodge Counter")]
    public float ParryBonusStagger = 0f;
    [Min(1f)] public float CounterDamageMultiplier = 1.0f;
    public float CounterWindowDuration = 2.5f;

    [Header("Aggressive Combo")]
    [Range(0f, 0.5f)] public float ComboDamagePerStack = 0f;
    [Min(1)] public int MaxComboStacks = 5;
    public float ComboResetTime = 3.0f;

    [Header("Stagger & Critical Execution")]
    [Min(1f)] public float StaggeredTargetDamageMultiplier = 1.0f;
    [Min(1f)] public float BackstabOrRecoveryCritMultiplier = 1.0f;

    [Header("Recovery")]
    public float HealOnKill = 0f;
    public float HealOnRoomClear = 0f;

    public static GameplayArtifactSO CreateRuntime(ArtifactPlaystyleType style)
    {
        GameplayArtifactSO art = CreateInstance<GameplayArtifactSO>();
        art.Playstyle = style;

        switch (style)
        {
            case ArtifactPlaystyleType.AggressiveCombo:
                art.ArtifactName = "Keris Ritme Beruntun";
                art.Description = "Setiap serangan beruntun menambah +12% damage hingga 5 tumpukan.";
                art.ComboDamagePerStack = 0.12f;
                art.MaxComboStacks = 5;
                art.ComboResetTime = 3.2f;
                break;

            case ArtifactPlaystyleType.ParryCounter:
                art.ArtifactName = "Perisai Pantul Sasak";
                art.Description = "Parry menambah +40 Stagger dan +50% damage selama 2.5 detik.";
                art.ParryBonusStagger = 40f;
                art.CounterDamageMultiplier = 1.50f;
                art.CounterWindowDuration = 2.5f;
                break;

            case ArtifactPlaystyleType.DodgeMobility:
                art.ArtifactName = "Gelang Langkah Angin";
                art.Description = "Dodge mengaktifkan serangan balik +40% damage selama 2.2 detik.";
                art.CounterDamageMultiplier = 1.40f;
                art.CounterWindowDuration = 2.2f;
                break;

            case ArtifactPlaystyleType.StaggerBreaker:
                art.ArtifactName = "Gada Pemecah Pertahanan";
                art.Description = "Menghasilkan +45% damage terhadap musuh yang sedang Staggered.";
                art.StaggeredTargetDamageMultiplier = 1.45f;
                break;

            case ArtifactPlaystyleType.CriticalExecution:
                art.ArtifactName = "Taring Sergap";
                art.Description = "Serangan dari belakang atau saat musuh Recovery menghasilkan 1.75x Critical.";
                art.BackstabOrRecoveryCritMultiplier = 1.75f;
                break;

            case ArtifactPlaystyleType.SoulRecovery:
                art.ArtifactName = "Jimat Hisap Jiwa";
                art.Description = "Memulihkan 6 HP setiap mengalahkan musuh dan 15 HP saat ruangan selesai.";
                art.HealOnKill = 6f;
                art.HealOnRoomClear = 15f;
                break;
        }

        return art;
    }
}