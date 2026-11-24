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