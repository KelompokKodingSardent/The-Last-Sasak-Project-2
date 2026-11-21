Berikut implementasi lengkap **Phase 5 (Roguelike Encounter: Room Composition, Room Modifier, Artefak Gameplay-Changing, dan Encounter Difficulty Progression)**.

Seluruh script dirancang agar **100% kompatibel** dengan `ArtifactDefinitionSO`, `Health`, `GameEvents`, `CombatTest`, dan `ForestRun` yang sudah ada di project kamu.

---

### 1. `GameplayArtifactSO.cs` (File Baru)

Mendefinisikan artefak yang **mengubah cara player bertarung** (bukan sekadar menambah statistik datar), lengkap dengan identitas *build* (*Aggressive Combo*, *Parry Counter*, *Dodge Mobility*, *Stagger Breaker*, *Critical Execution*, dan *Soul Recovery*).

```csharp
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

```

---

### 2. `ArtifactController.cs`

Tetap mempertahankan `Equip(ArtifactDefinitionSO)` lama, sekaligus menambahkan dukungan `GameplayArtifactSO` berlapis (*stackable* sepanjang run), mekanik *Combo*, *Counter Window*, *Stagger Punish*, *Backstab Critical*, dan *Soul Recovery*.

```csharp
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public interface IStatModifier
{
    float Modify(float baseValue);
}

public class ArtifactController : MonoBehaviour, IStatModifier
{
    [Header("Legacy Artifact (Kompatibilitas Scene Lama)")]
    public ArtifactDefinitionSO Artifact;
    public float DamageMultiplier = 1f;
    public float HealthBonus;

    [Header("Phase 5 — Gameplay-Changing Artifacts")]
    public List<GameplayArtifactSO> EquippedGameplayArtifacts = new List<GameplayArtifactSO>();

    [Header("Debug Live Combat Buffs (Read-Only)")]
    [SerializeField] private int currentComboStacks;
    [SerializeField] private float activeCounterTimer;
    [SerializeField] private float totalContextualMultiplier = 1f;

    private float _comboResetTimer;
    private float _activeCounterMultiplier = 1f;
    private Health _playerHealth;

    private FieldInfo _healthCurField;
    private PropertyInfo _healthCurProp;
    private MethodInfo _healMethod;

    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        CacheHealthReflection();
    }

    private void Start()
    {
        if (Artifact != null)
            Equip(Artifact);

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            ApplyArtifactStatBonuses(EquippedGameplayArtifacts[i]);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
        GameEvents.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_comboResetTimer > 0f)
        {
            _comboResetTimer -= dt;
            if (_comboResetTimer <= 0f)
                currentComboStacks = 0;
        }

        if (activeCounterTimer > 0f)
        {
            activeCounterTimer -= dt;
            if (activeCounterTimer <= 0f)
                _activeCounterMultiplier = 1f;
        }

        totalContextualMultiplier = GetCurrentBaseAndBuffMultiplier();
    }

    // Kompatibilitas penuh dengan sistem lama
    public void Equip(ArtifactDefinitionSO definition)
    {
        Artifact = definition;
        if (definition == null)
            return;

        DamageMultiplier = definition.DamageMultiplier;
        HealthBonus = definition.HealthBonus;
        if (_playerHealth != null && HealthBonus > 0f)
            _playerHealth.Configure(_playerHealth.MaxHealth + HealthBonus);
    }

    public void EquipGameplayArtifact(GameplayArtifactSO artifact)
    {
        if (artifact == null || EquippedGameplayArtifacts.Contains(artifact))
            return;

        EquippedGameplayArtifacts.Add(artifact);
        ApplyArtifactStatBonuses(artifact);
    }

    private void ApplyArtifactStatBonuses(GameplayArtifactSO artifact)
    {
        if (artifact == null) return;

        if (artifact.BaseDamageMultiplier > 0f && !Mathf.Approximately(artifact.BaseDamageMultiplier, 1f))
            DamageMultiplier *= artifact.BaseDamageMultiplier;

        if (artifact.MaxHealthBonus > 0f && _playerHealth != null)
        {
            HealthBonus += artifact.MaxHealthBonus;
            _playerHealth.Configure(_playerHealth.MaxHealth + artifact.MaxHealthBonus);
        }
    }

    // Dipanggil oleh AttackController / CombatSystem melalui IStatModifier
    public float Modify(float baseValue)
    {
        RegisterComboHit();
        return baseValue * GetCurrentBaseAndBuffMultiplier();
    }

    public float ApplyDamage(float amount)
    {
        return Modify(amount);
    }

    /// <summary>
    /// Evaluasi damage kontekstual terhadap target tertentu (Staggered, Backstab, Recovery, Combo, Counter).
    /// </summary>
    public float EvaluateContextualDamage(float baseValue, GameObject target)
    {
        RegisterComboHit();
        float mult = GetCurrentBaseAndBuffMultiplier();

        if (target != null && EquippedGameplayArtifacts.Count > 0)
        {
            EnemyBrain brain = target.GetComponentInParent<EnemyBrain>();
            bool isStaggered = brain != null && brain.CurrentState == EnemyBrainState.Staggered;
            bool isVulnerableWindow = brain != null &&
                (brain.CurrentState == EnemyBrainState.Recovery || brain.CurrentState == EnemyBrainState.Telegraph);

            Vector3 toPlayer = transform.position - target.transform.position;
            toPlayer.y = 0f;
            bool isBackstab = toPlayer.sqrMagnitude > 0.01f &&
                              Vector3.Angle(target.transform.forward, toPlayer.normalized) > 115f;

            for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
            {
                GameplayArtifactSO art = EquippedGameplayArtifacts[i];
                if (art == null) continue;

                if (isStaggered && art.StaggeredTargetDamageMultiplier > 1f)
                    mult *= art.StaggeredTargetDamageMultiplier;

                if ((isBackstab || isVulnerableWindow) && art.BackstabOrRecoveryCritMultiplier > 1f)
                    mult *= art.BackstabOrRecoveryCritMultiplier;
            }
        }

        return baseValue * mult;
    }

    /// <summary>
    /// Dipanggil saat player berhasil melakukan Parry terhadap serangan musuh.
    /// </summary>
    public void NotifyParrySuccess(GameObject parriedEnemy)
    {
        float extraStagger = 0f;

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art == null) continue;

            if (art.Playstyle == ArtifactPlaystyleType.ParryCounter || art.ParryBonusStagger > 0f)
            {
                extraStagger += art.ParryBonusStagger;
                ActivateCounterWindow(art.CounterDamageMultiplier, art.CounterWindowDuration);
            }
        }

        if (extraStagger > 0f && parriedEnemy != null)
        {
            StaggerComponent stagger = parriedEnemy.GetComponentInParent<StaggerComponent>();
            stagger?.Add(extraStagger, true);
        }
    }

    /// <summary>
    /// Dipanggil saat player melakukan Dodge.
    /// </summary>
    public void NotifyDodgeSuccess()
    {
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art == null) continue;

            if (art.Playstyle == ArtifactPlaystyleType.DodgeMobility && art.CounterDamageMultiplier > 1f)
            {
                ActivateCounterWindow(art.CounterDamageMultiplier, art.CounterWindowDuration);
            }
        }
    }

    private void RegisterComboHit()
    {
        int highestMaxStacks = 0;
        float longestReset = 0f;

        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            GameplayArtifactSO art = EquippedGameplayArtifacts[i];
            if (art != null && art.ComboDamagePerStack > 0f)
            {
                highestMaxStacks = Mathf.Max(highestMaxStacks, art.MaxComboStacks);
                longestReset = Mathf.Max(longestReset, art.ComboResetTime);
            }
        }

        if (highestMaxStacks > 0)
        {
            currentComboStacks = Mathf.Min(highestMaxStacks, currentComboStacks + 1);
            _comboResetTimer = longestReset;
        }
    }

    private void ActivateCounterWindow(float multiplier, float duration)
    {
        _activeCounterMultiplier = Mathf.Max(_activeCounterMultiplier, multiplier);
        activeCounterTimer = Mathf.Max(activeCounterTimer, duration);
    }

    private float GetCurrentBaseAndBuffMultiplier()
    {
        float mult = DamageMultiplier <= 0f ? 1f : DamageMultiplier;

        if (activeCounterTimer > 0f && _activeCounterMultiplier > 1f)
            mult *= _activeCounterMultiplier;

        if (currentComboStacks > 0)
        {
            float perStack = 0f;
            for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
            {
                if (EquippedGameplayArtifacts[i] != null)
                    perStack += EquippedGameplayArtifacts[i].ComboDamagePerStack;
            }
            mult *= (1f + currentComboStacks * perStack);
        }

        return mult;
    }

    private void HandleEntityDied(Health victim)
    {
        if (victim == null || victim == _playerHealth || victim.CompareTag("Player"))
            return;

        float totalHeal = 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                totalHeal += EquippedGameplayArtifacts[i].HealOnKill;
        }

        if (totalHeal > 0f)
            HealPlayer(totalHeal);
    }

    private void HandleRoomCleared()
    {
        float totalHeal = 0f;
        for (int i = 0; i < EquippedGameplayArtifacts.Count; i++)
        {
            if (EquippedGameplayArtifacts[i] != null)
                totalHeal += EquippedGameplayArtifacts[i].HealOnRoomClear;
        }

        if (totalHeal > 0f)
            HealPlayer(totalHeal);
    }

    private void HealPlayer(float amount)
    {
        if (_playerHealth == null || amount <= 0f) return;

        if (_healMethod != null)
        {
            _healMethod.Invoke(_playerHealth, new object[] { amount });
            return;
        }

        if (_healthCurField != null)
        {
            float cur = System.Convert.ToSingle(_healthCurField.GetValue(_playerHealth));
            float next = Mathf.Min(_playerHealth.MaxHealth, cur + amount);
            _healthCurField.SetValue(_playerHealth, next);
        }
        else if (_healthCurProp != null && _healthCurProp.CanWrite)
        {
            float cur = System.Convert.ToSingle(_healthCurProp.GetValue(_playerHealth));
            float next = Mathf.Min(_playerHealth.MaxHealth, cur + amount);
            _healthCurProp.SetValue(_playerHealth, next);
        }
    }

    private void CacheHealthReflection()
    {
        if (_playerHealth == null) return;
        var t = _playerHealth.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        _healMethod = t.GetMethod("Heal", flags) ?? t.GetMethod("Restore", flags) ?? t.GetMethod("AddHealth", flags);
        _healthCurField = t.GetField("Current", flags) ?? t.GetField("CurrentHealth", flags) ?? t.GetField("currentHealth", flags);
        _healthCurProp = t.GetProperty("Current", flags) ?? t.GetProperty("CurrentHealth", flags);
    }
}

```

---

### 3. `EncounterCompositionSO.cs` & `RoomModifierSO.cs` (File Baru)

Memungkinkan setiap ruangan memiliki **komposisi musuh terkurasi** (`Basic`, `Pressure`, `Flanking`, `Heavy`, `Control`, `Elite`) dan **kondisi arena** (`VolatileGround`, `BloodlustArena`, `IronFortress`, `SwiftSkirmish`, `TacticalAmbush`).

```csharp
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

```

```csharp
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

```

---

### 4. `RoomController.cs`

Menambahkan identitas layout arena (`Open`, `Pillar`, `Narrow`, `Hazard`), slot `EncounterCompositionSO` & `RoomModifierSO`, serta eksekusi *Volatile Ground* saat ruangan sedang aktif.

```csharp
using UnityEngine;
using UnityEngine.AI;

public enum ArenaLayoutArchetype
{
    Open,
    Pillar,
    Narrow,
    Hazard
}

public class RoomController : MonoBehaviour
{
    public bool Cleared { get; private set; }
    public Transform PlayerSpawn;
    public EnemySpawnPoint[] SpawnPoints;
    public GameObject[] LockedExits;

    [Header("Phase 5 — Arena Identity & Encounter")]
    public ArenaLayoutArchetype LayoutType = ArenaLayoutArchetype.Open;
    public EncounterCompositionSO CuratedComposition;
    public RoomModifierSO ActiveRoomModifier;
    public bool AutoRollRoomModifier = false;
    public LayerMask PlayerLayerMask = 1 << 8;

    private bool _encounterActive;
    private float _hazardTimer;
    private Transform _trackedPlayer;

    public void BeginRoomEncounter(Transform player, int runDepth)
    {
        Cleared = false;
        _encounterActive = true;
        _trackedPlayer = player;

        if (ActiveRoomModifier == null && AutoRollRoomModifier && runDepth > 1)
        {
            RoomModifierType[] pool =
            {
                RoomModifierType.BloodlustArena,
                RoomModifierType.SwiftSkirmish,
                RoomModifierType.TacticalAmbush,
                RoomModifierType.VolatileGround
            };
            ActiveRoomModifier = RoomModifierSO.CreateRuntime(pool[Random.Range(0, pool.Length)]);
        }

        if (ActiveRoomModifier != null && ActiveRoomModifier.SpawnPeriodicHazards)
            _hazardTimer = ActiveRoomModifier.HazardInterval * 0.65f;
    }

    public void SetCleared(bool cleared)
    {
        Cleared = cleared;
        if (cleared)
            _encounterActive = false;

        if (LockedExits == null)
            return;
        for (int i = 0; i < LockedExits.Length; i++)
        {
            if (LockedExits[i] != null)
                LockedExits[i].SetActive(!cleared);
        }
    }

    private void Update()
    {
        if (!_encounterActive || Cleared || ActiveRoomModifier == null || !ActiveRoomModifier.SpawnPeriodicHazards)
            return;

        _hazardTimer -= Time.deltaTime;
        if (_hazardTimer <= 0f)
        {
            _hazardTimer = ActiveRoomModifier.HazardInterval;
            SpawnEnvironmentalHazard();
        }
    }

    private void SpawnEnvironmentalHazard()
    {
        if (_trackedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _trackedPlayer = p.transform;
        }

        Vector3 anchor = _trackedPlayer != null ? _trackedPlayer.position : transform.position;
        Vector2 offset2D = Random.insideUnitCircle * 3.5f;
        Vector3 candidate = anchor + new Vector3(offset2D.x, 0f, offset2D.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            candidate = hit.position;

        ArenaHazardZone.Create(
            candidate,
            ActiveRoomModifier.HazardRadius,
            ActiveRoomModifier.HazardDamagePerTick,
            ActiveRoomModifier.HazardDuration,
            0.7f,
            gameObject,
            PlayerLayerMask
        );
    }
}

```

---

### 5. `RoomEncounterController.cs`

Mengelola *spawn* komposisi terkurasi (`CuratedComposition`), mendukung semua prefab archetype (`Vanguard`, `Skirmisher`, `Ranger`, `Bulwark`, `Controller`, `Boss`), menerapkan **RoomModifierSO**, serta menyuntikkan **Difficulty Progression Modifier** seiring bertambahnya kedalaman ruangan (`RunDepth`).

```csharp
using System.Collections.Generic;
using UnityEngine;

public class RoomEncounterController : MonoBehaviour
{
    [Header("Enemy Prefabs (Phase 1-5)")]
    public GameObject MeleePrefab;
    public GameObject SkirmisherPrefab;
    public GameObject RangedPrefab;
    public GameObject ElitePrefab;
    public GameObject ControllerPrefab;
    public GameObject BossPrefab;

    [Header("Runtime Settings")]
    public Transform Player;
    public bool SpawnFromPool;

    readonly List<Health> alive = new List<Health>();
    EnemyPool enemyPool;
    ProjectilePool projectilePool;
    bool running;

    void Awake()
    {
        enemyPool = GetComponent<EnemyPool>();
        projectilePool = FindFirstObjectByType<ProjectilePool>();
    }

    public void StartEncounter(RoomController room)
    {
        ClearTracking();
        running = true;

        int runDepth = RoomManager.Instance != null ? RoomManager.Instance.RunDepth : 1;
        if (room != null)
            room.BeginRoomEncounter(Player, runDepth);

        if (!SpawnFromPool)
        {
            TrackExisting(room, runDepth);
            return;
        }

        EnemySpawnPoint[] points = room != null ? room.SpawnPoints : null;
        if (points == null || points.Length == 0)
        {
            TrackExisting(room, runDepth);
            return;
        }

        // Gunakan CuratedComposition jika diisi pada RoomController
        if (room.CuratedComposition != null && room.CuratedComposition.Enemies.Count > 0)
        {
            List<EnemyArchetype> list = room.CuratedComposition.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                EnemySpawnPoint point = points[i % points.Length];
                Vector3 jitter = i >= points.Length ? (Vector3)(Random.insideUnitCircle * 1.4f) : Vector3.zero;
                jitter.y = 0f;
                Spawn(PrefabFor(list[i]), point.transform.position + jitter, room, runDepth);
            }
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];
            GameObject prefab = PrefabFor(point.Archetype);
            Spawn(prefab, point.transform.position, room, runDepth);
        }
    }

    void TrackExisting(RoomController room, int runDepth)
    {
        if (room == null)
            return;

        Health[] healths = room.GetComponentsInChildren<Health>(true);
        for (int i = 0; i < healths.Length; i++)
        {
            Health health = healths[i];
            if (health == null || health.CompareTag("Player") || !health.gameObject.activeInHierarchy)
                continue;

            health.OnDied += HandleDeath;
            alive.Add(health);

            EnemyBrain brain = health.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.Bind(brain.Definition, Player, projectilePool);
                ApplyRoomAndProgressionModifiers(brain, room, runDepth);
            }
        }

        if (alive.Count == 0)
        {
            running = false;
            GameEvents.RaiseRoomCleared();
        }
    }

    GameObject PrefabFor(EnemyArchetype archetype)
    {
        switch (archetype)
        {
            case EnemyArchetype.Skirmisher:
                return SkirmisherPrefab != null ? SkirmisherPrefab : MeleePrefab;
            case EnemyArchetype.Ranged:
            case EnemyArchetype.Ranger:
                return RangedPrefab;
            case EnemyArchetype.Controller:
                return ControllerPrefab != null ? ControllerPrefab : RangedPrefab;
            case EnemyArchetype.Elite:
            case EnemyArchetype.Bulwark:
                return ElitePrefab != null ? ElitePrefab : MeleePrefab;
            case EnemyArchetype.Boss:
                return BossPrefab != null ? BossPrefab : (ElitePrefab != null ? ElitePrefab : MeleePrefab);
            default:
                return MeleePrefab;
        }
    }

    void Spawn(GameObject prefab, Vector3 position, RoomController room, int runDepth)
    {
        if (prefab == null)
            return;

        GameObject instance = enemyPool != null
            ? enemyPool.Get(prefab, position, Quaternion.identity)
            : Instantiate(prefab, position, Quaternion.identity);

        EnemyBrain brain = instance.GetComponent<EnemyBrain>();
        Health health = instance.GetComponent<Health>();

        if (brain != null)
        {
            brain.Bind(brain.Definition, Player, projectilePool);
            ApplyRoomAndProgressionModifiers(brain, room, runDepth);
        }

        if (health != null)
        {
            health.OnDied += HandleDeath;
            alive.Add(health);
        }
    }

    private void ApplyRoomAndProgressionModifiers(EnemyBrain brain, RoomController room, int runDepth)
    {
        if (brain == null) return;

        // 1. Terapkan Room Modifier jika aktif
        if (room != null && room.ActiveRoomModifier != null && room.ActiveRoomModifier.ApplyEnemyModifierToAll)
        {
            brain.AddModifier(EnemyModifierSO.CreateRuntime(room.ActiveRoomModifier.InjectedEnemyModifier));
        }

        // 2. Encounter Difficulty Progression berdasarkan kedalaman ruangan (RunDepth)
        if (runDepth >= 2 && Random.value < Mathf.Min(0.75f, (runDepth - 1) * 0.25f))
        {
            EnemyModifierType[] scalingPool =
            {
                EnemyModifierType.Swift,
                EnemyModifierType.Aggressive,
                EnemyModifierType.Tactical,
                EnemyModifierType.Armored
            };
            EnemyModifierType rolled = scalingPool[Random.Range(0, scalingPool.Length)];
            brain.AddModifier(EnemyModifierSO.CreateRuntime(rolled));
        }
    }

    void HandleDeath()
    {
        alive.RemoveAll(h => h == null || h.IsDead || !h.gameObject.activeInHierarchy);
        if (running && alive.Count == 0)
        {
            running = false;
            GameEvents.RaiseRoomCleared();
        }
    }

    void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
    }

    void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        ClearTracking();
    }

    void HandleEntityDied(Health health)
    {
        if (health == null)
            return;
        HandleDeath();
    }

    void ClearTracking()
    {
        for (int i = 0; i < alive.Count; i++)
        {
            if (alive[i] != null)
                alive[i].OnDied -= HandleDeath;
        }

        alive.Clear();
        running = false;
    }
}

```

---

### 6. `RoomManager.cs`

Melacak `RunDepth` (progres ruangan dalam satu *run*) serta menyediakan opsi `GrantRandomArtifactOnClear` agar setiap ruangan yang selesai langsung membentuk identitas *build* player untuk ruangan berikutnya.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public RoomController ActiveRoom { get; private set; }
    public RoomGraph Graph;
    public RoomController[] ExampleRooms;
    public Transform Player;
    public bool StartOnPlay = true;

    [Header("Phase 5 — Run Progression & Roguelike Reward")]
    [SerializeField] private int runDepth = 1;
    public bool GrantRandomArtifactOnClear = true;
    public List<GameplayArtifactSO> RunArtifactPool = new List<GameplayArtifactSO>();

    public int RunDepth => runDepth;

    RoomEncounterController encounter;
    int index;

    void Awake()
    {
        Instance = this;
        encounter = GetComponent<RoomEncounterController>();
    }

    void Start()
    {
        if (!StartOnPlay)
            return;
        if (Player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Player = player.transform;
        }

        runDepth = 1;

        if (ExampleRooms != null && ExampleRooms.Length > 0)
            Activate(ExampleRooms[0], Player);
    }

    public void Activate(RoomController room, Transform player)
    {
        ActiveRoom = room;
        if (Graph != null)
        {
            for (int i = 0; i < Graph.Rooms.Count; i++)
            {
                if (Graph.Rooms[i].Controller == room)
                    index = i;
            }
        }

        if (encounter != null)
        {
            encounter.Player = player;
            encounter.StartEncounter(room);
        }
    }

    public void Advance(Transform player)
    {
        if (player == null)
            return;

        runDepth++;

        if (ExampleRooms != null && ExampleRooms.Length > 0)
        {
            if (index + 1 >= ExampleRooms.Length)
                return;
            index++;
            RoomController nextRoom = ExampleRooms[index];
            if (nextRoom != null && nextRoom.PlayerSpawn != null)
                player.position = nextRoom.PlayerSpawn.position + Vector3.up;
            Activate(nextRoom, player);
            return;
        }

        if (Graph == null || index + 1 >= Graph.Rooms.Count)
            return;

        index++;
        RoomPlacement next = Graph.Rooms[index];
        if (next.Controller != null && next.Controller.PlayerSpawn != null)
            player.position = next.Controller.PlayerSpawn.position + Vector3.up;
        else
            player.position = next.WorldPosition + Vector3.up;

        Activate(next.Controller, player);
    }

    void OnEnable()
    {
        GameEvents.OnRoomCleared += HandleCleared;
    }

    void OnDisable()
    {
        GameEvents.OnRoomCleared -= HandleCleared;
        if (Instance == this)
            Instance = null;
    }

    void HandleCleared()
    {
        ActiveRoom?.SetCleared(true);

        if (GrantRandomArtifactOnClear && Player != null)
        {
            ArtifactController artCtrl = Player.GetComponent<ArtifactController>();
            if (artCtrl != null)
            {
                GameplayArtifactSO reward = PickArtifactReward();
                if (reward != null)
                    artCtrl.EquipGameplayArtifact(reward);
            }
        }
    }

    private GameplayArtifactSO PickArtifactReward()
    {
        if (RunArtifactPool != null && RunArtifactPool.Count > 0)
            return RunArtifactPool[Random.Range(0, RunArtifactPool.Count)];

        ArtifactPlaystyleType[] styles =
        {
            ArtifactPlaystyleType.AggressiveCombo,
            ArtifactPlaystyleType.ParryCounter,
            ArtifactPlaystyleType.DodgeMobility,
            ArtifactPlaystyleType.StaggerBreaker,
            ArtifactPlaystyleType.CriticalExecution,
            ArtifactPlaystyleType.SoulRecovery
        };
        return GameplayArtifactSO.CreateRuntime(styles[Random.Range(0, styles.Length)]);
    }
}

```

---

### Setup Cepat di Unity Editor untuk Menguji Seluruh Siklus Phase 1–5 (`ForestRun` & `CombatTest`)

1. **Hubungkan Event Parry & Dodge Player ke `ArtifactController`:**
* Pada `CombatResolver.cs` (saat `parryResolver.TryResolve(...)` berhasil), tambahkan satu baris:
```csharp
targetObject.GetComponent<ArtifactController>()?.NotifyParrySuccess(info.Instigator);

```


* Pada `DodgeController.cs` (saat dodge dimulai), tambahkan satu baris:
```csharp
GetComponent<ArtifactController>()?.NotifyDodgeSuccess();

```




2. **Atur Komposisi & Modifier Ruangan di `ForestRun`:**
* Pada `Room_Start` (`RoomController`): set **Layout Type** = `Open`, **Active Room Modifier** = kosong (ruangan pembuka bersih).
* Pada `Room_Combat` (`RoomController`): centang **Auto Roll Room Modifier** (`true`) atau buat asset `RoomModifierSO` (**Create → TLS → Roguelike → Room Modifier**) seperti `VolatileGround` atau `BloodlustArena`, lalu drag ke **Active Room Modifier**.
* Jika memakai `SpawnFromPool = true`, buat asset `EncounterCompositionSO` (**Create → TLS → Roguelike → Encounter Composition**) berisi komposisi seperti `Control` (`1 Controller + 2 Skirmisher`) atau `Heavy` (`1 Bulwark + 2 Vanguard`) dan assign ke **Curated Composition**.


3. **Verifikasi Alur Lengkap:**
* Masuk ke ruangan pertama → lawan musuh dengan peran berbeda (`Front`, `Flank`, `Ranged`) → *Room Cleared* otomatis memberikan artefak *gameplay-changing* ke `ArtifactController` → gerbang terbuka → pindah ke ruangan berikutnya dengan `RunDepth = 2` yang otomatis menyuntikkan *Enemy Modifier* dan *Room Modifier* baru.



Jika kamu ingin menambahkan **UI pilihan 3 Artefak (draft screen)** saat ruangan selesai atau ingin merapikan integrasi `AttackController.cs` dengan `EvaluateContextualDamage(...)`, kirimkan file terkait dan kita langsung sambungkan.