using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

[Serializable]
public struct PoolMetricSnapshot
{
    public string PrefabName;
    public int TotalCreated;
    public int ActiveRented;
    public int InactiveAvailable;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Optional Prewarm Catalog")]
    [SerializeField] private PoolCatalogSO poolCatalog;
    [SerializeField] private Transform poolRootContainer;

    private class PoolWrap
    {
        public GameObject Prefab;
        public ObjectPool<GameObject> Pool;
        public int TotalCreated;
        public int ActiveRented;
    }

    private readonly Dictionary<GameObject, PoolWrap> _pools = new Dictionary<GameObject, PoolWrap>();
    private readonly Dictionary<GameObject, PoolWrap> _instanceToPool = new Dictionary<GameObject, PoolWrap>();
    private int _leaseCounter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (poolRootContainer == null)
        {
            GameObject root = new GameObject("PooledInstances");
            root.transform.SetParent(transform);
            poolRootContainer = root.transform;
        }
    }

    private void Start()
    {
        PrewarmFromCatalog();
    }

    public void PrewarmFromCatalog()
    {
        if (poolCatalog == null || poolCatalog.Entries == null)
            return;

        IReadOnlyList<PoolCatalogSO.PoolEntry> entries = poolCatalog.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            PoolCatalogSO.PoolEntry entry = entries[i];
            if (entry.Prefab == null)
                continue;

            GetOrCreatePool(entry.Prefab, entry.DefaultCapacity, entry.MaxSize);
            if (entry.PrewarmCount > 0)
            {
                Prewarm(entry.Prefab, entry.PrewarmCount);
            }
        }
    }

    public void Prewarm(GameObject prefab, int targetCount)
    {
        if (prefab == null || targetCount <= 0)
            return;

        PoolWrap wrap = GetOrCreatePool(prefab, Mathf.Max(8, targetCount), Mathf.Max(64, targetCount * 2));
        int toCreate = targetCount - wrap.TotalCreated;
        if (toCreate <= 0)
            return;

        List<GameObject> temp = new List<GameObject>(toCreate);
        for (int i = 0; i < toCreate; i++)
        {
            temp.Add(wrap.Pool.Get());
        }

        for (int i = 0; i < temp.Count; i++)
        {
            wrap.Pool.Release(temp[i]);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        PoolWrap wrap = GetOrCreatePool(prefab, 8, 64);
        GameObject instance = wrap.Pool.Get();

        // 1. Posisikan & sinkronkan fisika/NavMesh SEBELUM objek diaktifkan
        instance.transform.SetPositionAndRotation(position, rotation);
        ResetComponentsBeforeActivate(instance);
        SyncNavigationAndPhysics(instance, position);

        // 2. Berikan LeaseId baru & picu IPoolable.OnRentFromPool
        _leaseCounter++;
        PoolLeaseId leaseId = new PoolLeaseId(_leaseCounter);

        PooledObject pooledMeta = instance.GetComponent<PooledObject>();
        if (pooledMeta != null)
            pooledMeta.MarkRented(leaseId);

        _instanceToPool[instance] = wrap;
        wrap.ActiveRented++;

        // 3. Aktifkan GameObject setelah seluruh posisi & state siap
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        PooledObject pooledMeta = instance.GetComponent<PooledObject>();
        if (pooledMeta != null && !pooledMeta.IsRented)
        {
            // Mencegah double-release yang menyebabkan exception pada Unity ObjectPool
            return;
        }

        if (_instanceToPool.TryGetValue(instance, out PoolWrap wrap))
        {
            if (pooledMeta != null)
                pooledMeta.MarkReturned();

            wrap.ActiveRented = Mathf.Max(0, wrap.ActiveRented - 1);
            wrap.Pool.Release(instance);
            return;
        }

        if (pooledMeta != null)
            pooledMeta.MarkReturned();

        instance.SetActive(false);
    }

    public List<PoolMetricSnapshot> GetPoolMetrics()
    {
        List<PoolMetricSnapshot> snapshots = new List<PoolMetricSnapshot>(_pools.Count);
        foreach (KeyValuePair<GameObject, PoolWrap> kvp in _pools)
        {
            PoolWrap wrap = kvp.Value;
            snapshots.Add(new PoolMetricSnapshot
            {
                PrefabName = wrap.Prefab != null ? wrap.Prefab.name : "NullPrefab",
                TotalCreated = wrap.TotalCreated,
                ActiveRented = wrap.ActiveRented,
                InactiveAvailable = wrap.Pool.CountInactive
            });
        }
        return snapshots;
    }

    private PoolWrap GetOrCreatePool(GameObject prefab, int defaultCapacity, int maxSize)
    {
        if (_pools.TryGetValue(prefab, out PoolWrap existing))
            return existing;

        PoolWrap wrap = new PoolWrap { Prefab = prefab };
        wrap.Pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(prefab, poolRootContainer);
                go.SetActive(false);

                PooledObject meta = go.GetComponent<PooledObject>();
                if (meta == null)
                    meta = go.AddComponent<PooledObject>();
                meta.BindPrefab(prefab);

                wrap.TotalCreated++;
                return go;
            },
            actionOnGet: null, // Aktivasi dilakukan secara deterministik di Get() setelah posisi & warp selesai
            actionOnRelease: go =>
            {
                if (go == null)
                    return;

                DisableNavigationBeforeRelease(go);
                go.SetActive(false);
                if (poolRootContainer != null)
                    go.transform.SetParent(poolRootContainer);
            },
            actionOnDestroy: go =>
            {
                if (go != null)
                {
                    _instanceToPool.Remove(go);
                    Destroy(go);
                }
            },
            collectionCheck: true,
            defaultCapacity: Mathf.Max(1, defaultCapacity),
            maxSize: Mathf.Max(1, maxSize));

        _pools.Add(prefab, wrap);
        return wrap;
    }

    private static void ResetComponentsBeforeActivate(GameObject go)
    {
        Health health = go.GetComponent<Health>();
        health?.ResetHealth();

        InvulnerabilityHandler invuln = go.GetComponent<InvulnerabilityHandler>();
        invuln?.Clear();

        EnemyBrain brain = go.GetComponent<EnemyBrain>();
        brain?.ResetBrain();

        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Owner = null;

        go.GetComponent<StaggerComponent>()?.ResetStagger();
    }

    private static void SyncNavigationAndPhysics(GameObject go, Vector3 targetPosition)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (!agent.enabled)
                agent.enabled = true;

            agent.Warp(targetPosition);
            if (agent.isOnNavMesh)
                agent.ResetPath();
        }
    }

    private static void DisableNavigationBeforeRelease(GameObject go)
    {
        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.ResetPath();
            agent.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}