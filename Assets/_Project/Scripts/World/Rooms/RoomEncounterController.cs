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