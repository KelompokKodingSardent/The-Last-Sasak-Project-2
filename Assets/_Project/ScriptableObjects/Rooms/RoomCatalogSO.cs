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