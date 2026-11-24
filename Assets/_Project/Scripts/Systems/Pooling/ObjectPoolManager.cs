using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private class PoolWrap
    {
        public GameObject Prefab;
        public ObjectPool<GameObject> Pool;
    }

    private readonly Dictionary<GameObject, PoolWrap> pools = new Dictionary<GameObject, PoolWrap>();
    private readonly Dictionary<GameObject, PoolWrap> instanceToPool = new Dictionary<GameObject, PoolWrap>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (!pools.TryGetValue(prefab, out PoolWrap wrap))
        {
            wrap = CreatePool(prefab);
            pools.Add(prefab, wrap);
        }

        GameObject instance = wrap.Pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        SyncNavigationAndPhysics(instance, position);
        instanceToPool[instance] = wrap;
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        if (instanceToPool.TryGetValue(instance, out PoolWrap wrap))
        {
            wrap.Pool.Release(instance);
            return;
        }

        instance.SetActive(false);
    }

    private PoolWrap CreatePool(GameObject prefab)
    {
        PoolWrap wrap = new PoolWrap { Prefab = prefab };
        wrap.Pool = new ObjectPool<GameObject>(
            () =>
            {
                GameObject go = Instantiate(prefab);
                go.SetActive(false);
                return go;
            },
            go =>
            {
                ResetPooled(go);
                go.SetActive(true);
            },
            go =>
            {
                DisableNavigationBeforeRelease(go);
                go.SetActive(false);
            },
            go => Destroy(go),
            true,
            8,
            64);
        return wrap;
    }

    private static void ResetPooled(GameObject go)
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