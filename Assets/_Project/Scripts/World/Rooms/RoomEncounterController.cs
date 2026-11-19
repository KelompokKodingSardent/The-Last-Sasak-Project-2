using System.Collections.Generic;
using UnityEngine;

public class RoomEncounterController : MonoBehaviour
{
    public GameObject MeleePrefab;
    public GameObject RangedPrefab;
    public GameObject ElitePrefab;
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
        if (!SpawnFromPool)
        {
            TrackExisting(room);
            return;
        }

        EnemySpawnPoint[] points = room != null ? room.SpawnPoints : null;
        if (points == null || points.Length == 0)
        {
            Spawn(MeleePrefab, transform.position + Vector3.forward * 4f);
            Spawn(RangedPrefab, transform.position + Vector3.right * 4f + Vector3.forward * 2f);
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];
            GameObject prefab = PrefabFor(point.Archetype);
            Spawn(prefab, point.transform.position);
        }
    }

    void TrackExisting(RoomController room)
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
            brain?.Bind(brain.Definition, Player, projectilePool);
        }

        if (alive.Count == 0)
        {
            running = false;
            GameEvents.RaiseRoomCleared();
        }
    }

    GameObject PrefabFor(EnemyArchetype archetype)
    {
        if (archetype == EnemyArchetype.Ranged)
            return RangedPrefab;
        if (archetype == EnemyArchetype.Elite)
            return ElitePrefab != null ? ElitePrefab : MeleePrefab;
        return MeleePrefab;
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        GameObject instance = enemyPool.Get(prefab, position, Quaternion.identity);
        EnemyBrain brain = instance.GetComponent<EnemyBrain>();
        Health health = instance.GetComponent<Health>();
        brain?.Bind(brain.Definition, Player, projectilePool);
        if (health != null)
        {
            health.OnDied += HandleDeath;
            alive.Add(health);
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
