using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolCatalog", menuName = "The Last Sasak/Systems/Pool Catalog")]
public class PoolCatalogSO : ScriptableObject
{
    [Serializable]
    public struct PoolEntry
    {
        public GameObject Prefab;
        [Min(0)] public int PrewarmCount;
        [Min(1)] public int DefaultCapacity;
        [Min(1)] public int MaxSize;
    }

    [SerializeField] private List<PoolEntry> entries = new List<PoolEntry>();

    public IReadOnlyList<PoolEntry> Entries => entries;

    private void OnValidate()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            PoolEntry entry = entries[i];
            if (entry.DefaultCapacity < 1) entry.DefaultCapacity = 8;
            if (entry.MaxSize < entry.DefaultCapacity) entry.MaxSize = Mathf.Max(64, entry.DefaultCapacity);
            entries[i] = entry;
        }
    }
}