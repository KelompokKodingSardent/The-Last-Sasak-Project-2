using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float navMeshSampleRadius = 2.5f;
    [SerializeField] private float minPlayerSpawnClearance = 3.5f;

    private readonly List<Health> alive = new List<Health>();
    private EnemyPool enemyPool;
    private ProjectilePool projectilePool;
    private RoomController activeRoom;
    private System.Random rng;
    private bool running;

    private void Awake()
    {
        enemyPool = GetComponent<EnemyPool>();
        projectilePool = FindFirstObjectByType<ProjectilePool>();
        rng = new System.Random(System.Environment.TickCount);
    }

    public void SetSeed(int seed)
    {
        rng = new System.Random(seed);
    }

    public void StartEncounter(RoomController room)
    {
        StartEncounter(room, null);
    }

    public void StartEncounter(RoomController room, EncounterCompositionSO compositionOverride)
    {
        if (room == null || room.Cleared)
            return;

        ClearTracking();
        activeRoom = room;
        running = true;

        int runDepth = RoomManager.Instance != null ? RoomManager.Instance.RunDepth : 1;
        room.BeginRoomEncounter(Player, runDepth);

        if (!SpawnFromPool)
        {
            TrackExisting(room, runDepth);
            return;
        }

        EnemySpawnPoint[] points = room.SpawnPoints;
        if (points == null || points.Length == 0)
        {
            TrackExisting(room, runDepth);
            return;
        }

        EncounterCompositionSO activeComposition = compositionOverride != null
            ? compositionOverride
            : room.CuratedComposition;

        if (activeComposition != null && activeComposition.Enemies != null && activeComposition.Enemies.Count > 0)
        {
            SpawnFromComposition(activeComposition, points, room, runDepth);
            VerifyEncounterHasEnemies();
            return;
        }

        // Fallback: gunakan Archetype langsung pada masing-masing EnemySpawnPoint
        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];
            if (point == null)
                continue;

            GameObject prefab = PrefabFor(point.Archetype);
            Spawn(prefab, point.transform.position, room, runDepth);
        }

        VerifyEncounterHasEnemies();
    }

    private void SpawnFromComposition(
        EncounterCompositionSO composition,
        EnemySpawnPoint[] points,
        RoomController room,
        int runDepth)
    {
        Dictionary<EnemySpawnPoint, int> usageCounts = new Dictionary<EnemySpawnPoint, int>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null && !usageCounts.ContainsKey(points[i]))
                usageCounts.Add(points[i], 0);
        }

        if (usageCounts.Count == 0)
            return;

        List<EnemyArchetype> enemies = composition.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyArchetype archetype = enemies[i];
            string preferredTag = composition.GetPreferredTagFor(archetype);

            EnemySpawnPoint chosenPoint = SelectBestSpawnPoint(points, preferredTag, usageCounts);
            if (chosenPoint == null)
                continue;

            int timesUsed = usageCounts[chosenPoint];
            usageCounts[chosenPoint] = timesUsed + 1;

            Vector3 jitter = timesUsed > 0 ? NextSeededJitter(1.4f) : Vector3.zero;
            Spawn(PrefabFor(archetype), chosenPoint.transform.position + jitter, room, runDepth);
        }
    }

    private EnemySpawnPoint SelectBestSpawnPoint(
        EnemySpawnPoint[] points,
        string preferredTag,
        Dictionary<EnemySpawnPoint, int> usageCounts)
    {
        bool hasPreferredTag = !string.IsNullOrWhiteSpace(preferredTag);

        // Prioritas 1: Tag cocok + jarak aman dari Player
        if (hasPreferredTag)
        {
            EnemySpawnPoint taggedSafe = PickLowestUsageCandidate(points, usageCounts, preferredTag, requireSafeDistance: true);
            if (taggedSafe != null)
                return taggedSafe;

            // Prioritas 2: Tag cocok (abaikan jarak jika semua titik bertag berdekatan dengan Player)
            EnemySpawnPoint taggedAny = PickLowestUsageCandidate(points, usageCounts, preferredTag, requireSafeDistance: false);
            if (taggedAny != null)
                return taggedAny;
        }

        // Prioritas 3: Titik mana saja yang jaraknya aman dari Player
        EnemySpawnPoint generalSafe = PickLowestUsageCandidate(points, usageCounts, null, requireSafeDistance: true);
        if (generalSafe != null)
            return generalSafe;

        // Prioritas 4: Fallback ke titik mana saja dengan pemakaian paling sedikit
        return PickLowestUsageCandidate(points, usageCounts, null, requireSafeDistance: false);
    }

    private EnemySpawnPoint PickLowestUsageCandidate(
        EnemySpawnPoint[] points,
        Dictionary<EnemySpawnPoint, int> usageCounts,
        string requiredTag,
        bool requireSafeDistance)
    {
        int lowestUsage = int.MaxValue;
        List<EnemySpawnPoint> bestCandidates = new List<EnemySpawnPoint>();
        bool filterByTag = !string.IsNullOrWhiteSpace(requiredTag);

        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];
            if (point == null || !usageCounts.TryGetValue(point, out int usage))
                continue;

            if (filterByTag && !point.HasTag(requiredTag))
                continue;

            if (requireSafeDistance && !IsSafeFromPlayer(point.transform.position))
                continue;

            if (usage < lowestUsage)
            {
                lowestUsage = usage;
                bestCandidates.Clear();
                bestCandidates.Add(point);
            }
            else if (usage == lowestUsage)
            {
                bestCandidates.Add(point);
            }
        }

        if (bestCandidates.Count == 0)
            return null;

        if (bestCandidates.Count == 1)
            return bestCandidates[0];

        return bestCandidates[NextInt(0, bestCandidates.Count)];
    }

    private bool IsSafeFromPlayer(Vector3 spawnPos)
    {
        if (Player == null || minPlayerSpawnClearance <= 0f)
            return true;

        float sqrDist = (spawnPos - Player.position).sqrMagnitude;
        return sqrDist >= (minPlayerSpawnClearance * minPlayerSpawnClearance);
    }

    public static bool CanRoomSupportEncounter(RoomController room, EncounterCompositionSO composition)
    {
        if (room == null || composition == null || composition.Enemies == null || composition.Enemies.Count == 0)
            return false;

        EnemySpawnPoint[] points = room.SpawnPoints;
        if (points == null || points.Length == 0)
            return false;

        // Batasi maksimal 2 musuh per spawn point fisik agar tidak menumpuk berlebihan
        return composition.Enemies.Count <= points.Length * 2;
    }

    private void TrackExisting(RoomController room, int runDepth)
    {
        Health[] healths = room.GetComponentsInChildren<Health>(true);
        for (int i = 0; i < healths.Length; i++)
        {
            Health health = healths[i];
            if (health == null || health.CompareTag("Player") || !health.gameObject.activeInHierarchy || health.IsDead)
                continue;

            alive.Add(health);

            EnemyBrain brain = health.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.Bind(brain.Definition, Player, projectilePool);
                ApplyRoomAndProgressionModifiers(brain, room, runDepth);
            }
        }

        VerifyEncounterHasEnemies();
    }

    private GameObject PrefabFor(EnemyArchetype archetype)
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

    private void Spawn(GameObject prefab, Vector3 rawPosition, RoomController room, int runDepth)
    {
        if (prefab == null)
            return;

        Vector3 spawnPosition = rawPosition;
        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            spawnPosition = hit.position;

        GameObject instance = enemyPool != null
            ? enemyPool.Get(prefab, spawnPosition, Quaternion.identity)
            : Instantiate(prefab, spawnPosition, Quaternion.identity);

        EnemyBrain brain = instance.GetComponent<EnemyBrain>();
        Health health = instance.GetComponent<Health>();

        if (brain != null)
        {
            brain.Bind(brain.Definition, Player, projectilePool);
            ApplyRoomAndProgressionModifiers(brain, room, runDepth);
        }

        if (health != null && !alive.Contains(health))
            alive.Add(health);
    }

    private void ApplyRoomAndProgressionModifiers(EnemyBrain brain, RoomController room, int runDepth)
    {
        if (brain == null)
            return;

        if (room != null && room.ActiveRoomModifier != null && room.ActiveRoomModifier.ApplyEnemyModifierToAll)
            brain.AddModifier(EnemyModifierSO.CreateRuntime(room.ActiveRoomModifier.InjectedEnemyModifier));

        if (runDepth >= 2)
        {
            float chance = Mathf.Min(0.75f, (runDepth - 1) * 0.25f);
            if (NextDouble() < chance)
            {
                EnemyModifierType[] scalingPool =
                {
                    EnemyModifierType.Swift,
                    EnemyModifierType.Aggressive,
                    EnemyModifierType.Tactical,
                    EnemyModifierType.Armored
                };
                EnemyModifierType rolled = scalingPool[NextInt(0, scalingPool.Length)];
                brain.AddModifier(EnemyModifierSO.CreateRuntime(rolled));
            }
        }
    }

    private void VerifyEncounterHasEnemies()
    {
        if (running && alive.Count == 0)
            CompleteEncounter();
    }

    private void CompleteEncounter()
    {
        running = false;
        RoomController clearedRoom = activeRoom;
        activeRoom = null;
        GameEvents.RaiseRoomCleared(clearedRoom);
    }

    private void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
    }

    private void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        ClearTracking();
    }

    private void HandleEntityDied(Health health)
    {
        if (!running || health == null)
            return;

        if (!alive.Remove(health))
            return;

        alive.RemoveAll(h => h == null || h.IsDead || !h.gameObject.activeInHierarchy);

        if (alive.Count == 0)
            CompleteEncounter();
    }

    public void ClearTracking()
    {
        alive.Clear();
        activeRoom = null;
        running = false;
    }

    private Vector3 NextSeededJitter(float radius)
    {
        double angle = NextDouble() * Mathf.PI * 2f;
        float dist = Mathf.Sqrt((float)NextDouble()) * radius;
        return new Vector3(Mathf.Cos((float)angle) * dist, 0f, Mathf.Sin((float)angle) * dist);
    }

    private double NextDouble()
    {
        if (rng == null)
            rng = new System.Random(System.Environment.TickCount);
        return rng.NextDouble();
    }

    private int NextInt(int minInclusive, int maxExclusive)
    {
        if (rng == null)
            rng = new System.Random(System.Environment.TickCount);
        return rng.Next(minInclusive, maxExclusive);
    }
}