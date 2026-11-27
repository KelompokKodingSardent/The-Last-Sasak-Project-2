using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    [Serializable]
    public struct LootDropEntry
    {
        public LootDefinitionSO lootDefinition;
        [Range(0f, 1f)]
        public float dropChance;
        [Min(1)]
        public int minAmount;
        [Min(1)]
        public int maxAmount;
    }

    public class LootDropController : MonoBehaviour
    {
        [Header("Drop Spawn Settings")]
        [SerializeField] private float horizontalScatterRadius = 0.75f;
        [SerializeField] private float verticalSpawnOffset = 0.25f;
        [SerializeField] private Transform pooledContainer;

        private readonly Dictionary<LootPickup, Queue<LootPickup>> _pools = new Dictionary<LootPickup, Queue<LootPickup>>();

        public event Action<LootPickup> OnLootSpawned;

        private void Awake()
        {
            if (pooledContainer == null)
            {
                GameObject containerObj = new GameObject("PooledLootPickups");
                containerObj.transform.SetParent(transform);
                pooledContainer = containerObj.transform;
            }
        }

        public void RollAndSpawnDrops(IReadOnlyList<LootDropEntry> dropTable, Vector3 originPosition)
        {
            if (dropTable == null || dropTable.Count == 0)
            {
                return;
            }

            for (int i = 0; i < dropTable.Count; i++)
            {
                LootDropEntry entry = dropTable[i];
                if (entry.lootDefinition == null || entry.dropChance <= 0f)
                {
                    continue;
                }

                if (UnityEngine.Random.value <= entry.dropChance)
                {
                    int min = Mathf.Max(1, entry.minAmount);
                    int max = Mathf.Max(min, entry.maxAmount);
                    int rolledAmount = UnityEngine.Random.Range(min, max + 1);

                    Vector3 spawnPos = ComputeScatteredPosition(originPosition);
                    SpawnLoot(entry.lootDefinition, spawnPos, rolledAmount);
                }
            }
        }

        public LootPickup SpawnLoot(LootDefinitionSO definition, Vector3 worldPosition, int overrideAmount = -1)
        {
            if (definition == null)
            {
                return null;
            }

            if (definition.PickupPrefab == null)
            {
                Debug.LogError($"[LootDropController] '{definition.LootId}' tidak memiliki PickupPrefab.", definition);
                return null;
            }

            LootPickup instance = GetOrCreateFromPool(definition.PickupPrefab);
            instance.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

            int finalAmount = overrideAmount > 0 ? overrideAmount : definition.Amount;
            instance.Initialize(definition, finalAmount, this, definition.PickupPrefab);
            instance.gameObject.SetActive(true);

            OnLootSpawned?.Invoke(instance);
            return instance;
        }

        public void ReturnPickupToPool(LootPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            LootPickup prefabKey = pickup.SourcePrefab;
            pickup.ResetPickupState();
            pickup.gameObject.SetActive(false);

            if (pooledContainer != null)
            {
                pickup.transform.SetParent(pooledContainer);
            }

            if (prefabKey == null)
            {
                return;
            }

            if (!_pools.TryGetValue(prefabKey, out Queue<LootPickup> queue))
            {
                queue = new Queue<LootPickup>();
                _pools[prefabKey] = queue;
            }

            queue.Enqueue(pickup);
        }

        private LootPickup GetOrCreateFromPool(LootPickup prefab)
        {
            if (_pools.TryGetValue(prefab, out Queue<LootPickup> queue))
            {
                while (queue.Count > 0)
                {
                    LootPickup pooled = queue.Dequeue();
                    if (pooled != null)
                    {
                        return pooled;
                    }
                }
            }

            LootPickup created = Instantiate(prefab, pooledContainer);
            return created;
        }

        private Vector3 ComputeScatteredPosition(Vector3 origin)
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * horizontalScatterRadius;
            return origin + new Vector3(circle.x, verticalSpawnOffset, circle.y);
        }
    }
}