using System.Collections.Generic;
using UnityEngine;

public class SetDressingGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct BiomeDressing
    {
        public BiomeType Biome;
        public GameObject[] BackgroundPrefabs; // Pohon lebat, air laut, tebing batu
        public GameObject[] FringeDetails;     // Rumput kecil, kerikil, lumut, tong
    }

    [Header("Set Dressing Settings")]
    public int DressingPadding = 4; // Jarak jangkauan hiasan di luar Playable Area
    [Range(0f, 1f)] public float ScatterChance = 0.6f; // Peluang munculnya detail di perbatasan

    [Header("Biome Assets Mapping")]
    public BiomeDressing[] BiomeAssets;

    private Dictionary<BiomeType, BiomeDressing> assetDictionary = new Dictionary<BiomeType, BiomeDressing>();

    private void Awake()
    {
        foreach (var asset in BiomeAssets)
        {
            if (!assetDictionary.ContainsKey(asset.Biome))
                assetDictionary.Add(asset.Biome, asset);
        }
    }

    // Fungsi Utama: Dipanggil dari MapGenerator setelah fisik ruangan selesai di-spawn
    public void GenerateSetDressing(Dictionary<Vector2Int, MapNode> mapGraph, int gridSize, Transform parentTransform)
    {
        if (mapGraph.Count == 0) return;

        // 1. TENTUKAN BOUNDING BOX PLAYABLE AREA
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var pos in mapGraph.Keys)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        // 2. FLOOD FILL AREA NON-PLAYABLE (DI LUAR GRID UTAMA)
        for (int x = minX - DressingPadding; x <= maxX + DressingPadding; x++)
        {
            for (int y = minY - DressingPadding; y <= maxY + DressingPadding; y++)
            {
                Vector2Int currentGrid = new Vector2Int(x, y);

                // Jika grid ini adalah Playable Area, lewati
                if (mapGraph.ContainsKey(currentGrid)) continue;

                // Tentukan bioma berdasarkan ketinggian Y
                BiomeType currentBiome = GetBiomeByY(y);
                Vector3 worldPos = new Vector3(x * gridSize, 0, y * gridSize);

                // Check apakah grid hampa ini berbatasan langsung dengan Playable Area
                bool isBorder = IsAdjacentToPlayable(currentGrid, mapGraph);

                // Spawn Background Solid
                SpawnBackgroundTile(currentBiome, worldPos, parentTransform);

                // 3. FRINGE SCATTERING (Menyamarkan Garis Grid di Perbatasan)
                if (isBorder && Random.value <= ScatterChance)
                {
                    SpawnFringeScatter(currentBiome, worldPos, parentTransform);
                }
            }
        }
    }

    private bool IsAdjacentToPlayable(Vector2Int gridPos, Dictionary<Vector2Int, MapNode> mapGraph)
    {
        Vector2Int[] neighbors = new Vector2Int[]
        {
            gridPos + Vector2Int.up,
            gridPos + Vector2Int.down,
            gridPos + Vector2Int.left,
            gridPos + Vector2Int.right
        };

        foreach (var neighbor in neighbors)
        {
            if (mapGraph.ContainsKey(neighbor)) return true;
        }
        return false;
    }

    private void SpawnBackgroundTile(BiomeType biome, Vector3 position, Transform parent)
    {
        if (!assetDictionary.ContainsKey(biome)) return;

        GameObject[] prefabs = assetDictionary[biome].BackgroundPrefabs;
        if (prefabs == null || prefabs.Length == 0) return;

        GameObject chosenPrefab = prefabs[Random.Range(0, prefabs.Length)];
        
        // Rotasi acak kelipatan 90 derajat agar variasi visual terlihat organik
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
        Instantiate(chosenPrefab, position, randomRotation, parent);
    }

    private void SpawnFringeScatter(BiomeType biome, Vector3 basePosition, Transform parent)
    {
        if (!assetDictionary.ContainsKey(biome)) return;

        GameObject[] details = assetDictionary[biome].FringeDetails;
        if (details == null || details.Length == 0) return;

        GameObject chosenDetail = details[Random.Range(0, details.Length)];

        // Beri sedikit offset posisi acak di dalam sel grid agar tidak terlalu simetris
        Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        Instantiate(chosenDetail, basePosition + randomOffset, randomRotation, parent);
    }

    private BiomeType GetBiomeByY(int y)
    {
        if (y > 60) return BiomeType.Castle;
        if (y > 30) return BiomeType.Forest;
        return BiomeType.Beach;
    }
}
