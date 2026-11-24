Berikut implementasi **Phase 03 — Room Data & Catalog**. Seluruh script diletakkan di global namespace agar langsung terhubung dengan `RoomController`, `RoomFootprint`, `RoomConnector`, dan `RoomPrefabValidator` tanpa mengubah referensi yang sudah ada.

### 1. `RoomDefinitionSO.cs`

Mendefinisikan identitas, peran topologi (`RoomRole`), aturan rotasi, batasan kedalaman run, serta sinkronisasi otomatis ukuran `FootprintCells` dan daftar arah `RoomConnector` langsung dari prefab yang di-assign.

```csharp
using System.Collections.Generic;
using UnityEngine;

public enum RoomRole
{
    Start,
    Traversal,
    Combat,
    Reward,
    Elite,
    Exit
}

[CreateAssetMenu(fileName = "RoomDef_Forest_New", menuName = "TLS/World/Room Definition")]
public class RoomDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string RoomID = "forest_combat_open_01";
    public string DisplayName = "Forest Clearing";
    public BiomeType Biome = BiomeType.Forest;
    public RoomRole Role = RoomRole.Combat;
    public ArenaLayoutArchetype LayoutArchetype = ArenaLayoutArchetype.Open;

    [Header("Prefab Reference")]
    public GameObject Prefab;

    [Header("Tags & Encounter Compatibility")]
    public List<string> RoomTags = new List<string>();
    [Tooltip("Jika diisi, generator hanya memilih encounter dari daftar ini untuk ruangan ini.")]
    public List<EncounterCompositionSO> OverrideEncounters = new List<EncounterCompositionSO>();

    [Header("Placement & Rotation Rules")]
    public bool AllowRotate90 = true;
    public bool AllowRotate180 = true;
    public bool AllowRotate270 = true;

    [Header("Run Depth & Weighting")]
    [Min(1)] public int MinRunDepth = 1;
    [Min(1)] public int MaxRunDepth = 99;
    [Min(0.01f)] public float GenerationWeight = 1f;
    [Tooltip("0 = tidak dibatasi dalam satu run.")]
    [Min(0)] public int MaxInstancesPerRun = 0;

    [Header("Cached Prefab Metadata (Auto-Synced)")]
    [SerializeField] private Vector2Int footprintCells = new Vector2Int(2, 2);
    [SerializeField] private List<Direction> localConnectorDirections = new List<Direction>();

    public Vector2Int FootprintCells => footprintCells;
    public IReadOnlyList<Direction> LocalConnectorDirections => localConnectorDirections;

    public List<int> GetAllowedRotationAngles()
    {
        List<int> angles = new List<int>(4) { 0 };
        if (AllowRotate90) angles.Add(90);
        if (AllowRotate180) angles.Add(180);
        if (AllowRotate270) angles.Add(270);
        return angles;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || RoomTags == null)
            return false;

        for (int i = 0; i < RoomTags.Count; i++)
        {
            if (string.Equals(RoomTags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool IsDepthAllowed(int runDepth)
    {
        return runDepth >= MinRunDepth && runDepth <= MaxRunDepth;
    }

    public void SyncFromPrefab()
    {
        if (Prefab == null)
            return;

        RoomFootprint fp = Prefab.GetComponent<RoomFootprint>();
        if (fp != null)
            footprintCells = fp.SizeInCells;

        localConnectorDirections.Clear();
        RoomConnector[] connectors = Prefab.GetComponentsInChildren<RoomConnector>(true);
        for (int i = 0; i < connectors.Length; i++)
        {
            if (connectors[i] != null)
                localConnectorDirections.Add(connectors[i].GetWorldDirection());
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (MaxRunDepth < MinRunDepth)
            MaxRunDepth = MinRunDepth;

        SyncFromPrefab();
    }
#endif
}

```

---

### 2. `RoomCatalogSO.cs`

Menjadi satu-satunya sumber daftar ruangan bagi `RoomGraphGenerator` dan `RoomPlacementSolver`. Menyediakan filter berbasis `RoomRole`, `RunDepth`, jumlah pemakaian dalam satu run, serta validasi seluruh katalog menggunakan `RoomPrefabValidator`.

```csharp
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "RoomCatalog_Forest", menuName = "TLS/World/Room Catalog")]
public class RoomCatalogSO : ScriptableObject
{
    public BiomeType TargetBiome = BiomeType.Forest;
    public List<RoomDefinitionSO> Rooms = new List<RoomDefinitionSO>();

    public List<RoomDefinitionSO> GetValidCandidates(
        RoomRole role,
        int runDepth,
        IReadOnlyDictionary<string, int> instanceCounts = null,
        int minRequiredConnectors = 1)
    {
        List<RoomDefinitionSO> results = new List<RoomDefinitionSO>();
        if (Rooms == null)
            return results;

        for (int i = 0; i < Rooms.Count; i++)
        {
            RoomDefinitionSO def = Rooms[i];
            if (def == null || def.Prefab == null)
                continue;

            if (def.Biome != TargetBiome || def.Role != role)
                continue;

            if (!def.IsDepthAllowed(runDepth))
                continue;

            if (def.LocalConnectorDirections.Count < minRequiredConnectors)
                continue;

            if (def.MaxInstancesPerRun > 0 && instanceCounts != null &&
                instanceCounts.TryGetValue(def.RoomID, out int currentCount) &&
                currentCount >= def.MaxInstancesPerRun)
            {
                continue;
            }

            results.Add(def);
        }

        return results;
    }

    public RoomDefinitionSO PickWeightedRoom(
        RoomRole role,
        int runDepth,
        System.Random rng,
        IReadOnlyDictionary<string, int> instanceCounts = null,
        int minRequiredConnectors = 1)
    {
        List<RoomDefinitionSO> candidates = GetValidCandidates(role, runDepth, instanceCounts, minRequiredConnectors);
        if (candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return candidates[0];

        double totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += Mathf.Max(0.01f, candidates[i].GenerationWeight);

        double roll = (rng != null ? rng.NextDouble() : 0.5) * totalWeight;
        double cumulative = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, candidates[i].GenerationWeight);
            if (roll <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    public bool ValidateAll(out List<string> reportMessages)
    {
        reportMessages = new List<string>();
        bool allPassed = true;
        HashSet<string> seenIds = new HashSet<string>();

        if (Rooms == null || Rooms.Count == 0)
        {
            reportMessages.Add("Catalog kosong: belum ada RoomDefinitionSO yang ditambahkan.");
            return false;
        }

        for (int i = 0; i < Rooms.Count; i++)
        {
            RoomDefinitionSO def = Rooms[i];
            if (def == null)
            {
                reportMessages.Add($"Index [{i}] pada catalog bernilai null.");
                allPassed = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.RoomID) || !seenIds.Add(def.RoomID))
            {
                reportMessages.Add($"RoomID kosong atau duplikat pada asset '{def.name}': '{def.RoomID}'.");
                allPassed = false;
            }

            def.SyncFromPrefab();
            RoomValidationReport prefabReport = RoomPrefabValidator.Validate(def.Prefab);
            if (!prefabReport.IsValid)
            {
                allPassed = false;
                for (int e = 0; e < prefabReport.Errors.Count; e++)
                    reportMessages.Add($"[{def.RoomID}] {prefabReport.Errors[e]}");
            }
        }

        return allPassed;
    }

#if UNITY_EDITOR
    [ContextMenu("Validate All Rooms In Catalog")]
    private void EditorValidateCatalog()
    {
        if (ValidateAll(out List<string> messages))
        {
            Debug.Log($"<color=#4CAF50>[PASS]</color> <b>{name}</b> valid ({Rooms.Count} room definitions).");
        }
        else
        {
            Debug.LogError($"<color=#F44336>[FAIL]</color> <b>{name}</b> memiliki {messages.Count} masalah:");
            for (int i = 0; i < messages.Count; i++)
                Debug.LogError($"   • {messages[i]}", this);
        }
    }
#endif
}

```

---

### 3. `MapGenerationConfigSO.cs`

Mengatur seluruh parameter pembangkitan topologi graph, cabang opsional, batas percobaan placement solver, dan referensi katalog ruangan/encounter untuk satu profil run (misal `ForestMapConfig_Default` dan `ForestMapConfig_QuickTest`).

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapGenConfig_Forest", menuName = "TLS/World/Map Generation Config")]
public class MapGenerationConfigSO : ScriptableObject
{
    [Header("Profile Identity")]
    public string ConfigName = "Level 01 - Forest Standard";
    public BiomeType Biome = BiomeType.Forest;
    public int GenerationVersion = 1;

    [Header("Catalog & Encounter Pool")]
    public RoomCatalogSO RoomCatalog;
    public List<EncounterCompositionSO> EncounterPool = new List<EncounterCompositionSO>();

    [Header("Seed Control")]
    [Tooltip("Jika aktif, generator selalu memakai FixedSeed untuk reproduksi layout.")]
    public bool UseFixedSeed = false;
    public int FixedSeed = 1001;

    [Header("Layer 1 — Main Path Topology")]
    [Tooltip("Jumlah ruangan Combat/Traversal di jalur utama antara Start dan Exit.")]
    [Range(2, 12)] public int MainPathLength = 4;
    [Range(0f, 1f)] public float TraversalRoomChance = 0.25f;

    [Header("Layer 1 — Optional Branches")]
    [Range(0, 5)] public int MaxBranchCount = 2;
    [Range(0f, 1f)] public float BranchSpawnProbability = 0.6f;
    [Range(1, 3)] public int MaxBranchLength = 2;
    public bool PrioritizeRewardOnDeadEnd = true;
    public bool AllowEliteInRun = false;

    [Header("Layer 2 — Spatial Placement Constraints")]
    [Min(5)] public int MaxPlacementAttempts = 40;
    [Min(1)] public int MaxBacktrackSteps = 8;
    [Tooltip("Batas radius grid dari titik Start (0,0) agar map tidak melebar ekstrem.")]
    [Min(10)] public int MaxGridRadius = 24;

    public int ResolveSeed()
    {
        if (UseFixedSeed)
            return FixedSeed;

        return System.Environment.TickCount ^ GetInstanceID();
    }

    public EncounterCompositionSO PickCompatibleEncounter(RoomDefinitionSO roomDef, int runDepth, System.Random rng)
    {
        List<EncounterCompositionSO> source = (roomDef != null && roomDef.OverrideEncounters != null && roomDef.OverrideEncounters.Count > 0)
            ? roomDef.OverrideEncounters
            : EncounterPool;

        if (source == null || source.Count == 0)
            return null;

        List<EncounterCompositionSO> valid = new List<EncounterCompositionSO>();
        double totalWeight = 0;

        for (int i = 0; i < source.Count; i++)
        {
            EncounterCompositionSO enc = source[i];
            if (enc != null && enc.IsCompatibleWith(roomDef, runDepth))
            {
                valid.Add(enc);
                totalWeight += Mathf.Max(0.01f, enc.SelectionWeight);
            }
        }

        if (valid.Count == 0)
            return null;

        double roll = (rng != null ? rng.NextDouble() : 0.5) * totalWeight;
        double cumulative = 0;

        for (int i = 0; i < valid.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, valid[i].SelectionWeight);
            if (roll <= cumulative)
                return valid[i];
        }

        return valid[valid.Count - 1];
    }
}

```

---

### 4. `EncounterCompositionSO.cs` (Diperluas)

Mempertahankan seluruh field dan `CreatePreset()` lama agar scene `CombatTest` dan `ForestRun` tetap bekerja, sekaligus menambahkan aturan kompatibilitas ruangan (`AllowedLayouts`, `RequiredRoomTags`), kedalaman run (`MinRunDepth`, `MaxRunDepth`), bobot pemilihan, dan preferensi tag spawn point.

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

```

---

## Target Pembuatan Asset Level 01 Hutan (`Assets/_Project/ScriptableObjects/World/`)

Setelah keempat script di atas dikompilasi, buat struktur asset awal berikut lewat menu klik-kanan **`Create > TLS > World`**:

| Folder | Nama Asset | Konfigurasi Kunci |
| --- | --- | --- |
| `World/Rooms/` | `RoomDef_Forest_Start_01` | `Role = Start`, `MaxInstancesPerRun = 1`, `Footprint = 1x1` / `2x1` |
| `World/Rooms/` | `RoomDef_Forest_OpenCombat_01` | `Role = Combat`, `LayoutArchetype = Open`, `Footprint = 2x2` |
| `World/Rooms/` | `RoomDef_Forest_OpenCombat_02` | `Role = Combat`, `LayoutArchetype = Open`, `Footprint = 2x2` |
| `World/Rooms/` | `RoomDef_Forest_PillarCombat_01` | `Role = Combat`, `LayoutArchetype = Pillar`, `Footprint = 2x2` |
| `World/Rooms/` | `RoomDef_Forest_PillarCombat_02` | `Role = Combat`, `LayoutArchetype = Pillar`, `Footprint = 2x2` |
| `World/Rooms/` | `RoomDef_Forest_Corridor_01` | `Role = Traversal`, `LayoutArchetype = Narrow`, `Footprint = 1x2` |
| `World/Rooms/` | `RoomDef_Forest_Reward_01` | `Role = Reward`, `MaxInstancesPerRun = 2`, `Footprint = 1x1` |
| `World/Rooms/` | `RoomDef_Forest_Exit_01` | `Role = Exit`, `MaxInstancesPerRun = 1`, `Footprint = 2x2` |
| `World/RoomCatalogs/` | `RoomCatalog_Forest` | Masukkan ke-8 `RoomDefinitionSO` di atas, lalu klik gear icon Inspector → **`Validate All Rooms In Catalog`** |
| `World/Maps/` | `MapGenConfig_Forest` | Hubungkan `RoomCatalog_Forest` dan preset `EncounterCompositionSO` |