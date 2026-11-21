using UnityEngine;
using UnityEngine.AI;

public enum ArenaLayoutArchetype
{
    Open,
    Pillar,
    Narrow,
    Hazard
}

public class RoomController : MonoBehaviour
{
    public bool Cleared { get; private set; }
    public Transform PlayerSpawn;
    public EnemySpawnPoint[] SpawnPoints;
    public GameObject[] LockedExits;

    [Header("Phase 5 — Arena Identity & Encounter")]
    public ArenaLayoutArchetype LayoutType = ArenaLayoutArchetype.Open;
    public EncounterCompositionSO CuratedComposition;
    public RoomModifierSO ActiveRoomModifier;
    public bool AutoRollRoomModifier = false;
    public LayerMask PlayerLayerMask = 1 << 8;

    private bool _encounterActive;
    private float _hazardTimer;
    private Transform _trackedPlayer;

    public void BeginRoomEncounter(Transform player, int runDepth)
    {
        Cleared = false;
        _encounterActive = true;
        _trackedPlayer = player;

        if (ActiveRoomModifier == null && AutoRollRoomModifier && runDepth > 1)
        {
            RoomModifierType[] pool =
            {
                RoomModifierType.BloodlustArena,
                RoomModifierType.SwiftSkirmish,
                RoomModifierType.TacticalAmbush,
                RoomModifierType.VolatileGround
            };
            ActiveRoomModifier = RoomModifierSO.CreateRuntime(pool[Random.Range(0, pool.Length)]);
        }

        if (ActiveRoomModifier != null && ActiveRoomModifier.SpawnPeriodicHazards)
            _hazardTimer = ActiveRoomModifier.HazardInterval * 0.65f;
    }

    public void SetCleared(bool cleared)
    {
        Cleared = cleared;
        if (cleared)
            _encounterActive = false;

        if (LockedExits == null)
            return;
        for (int i = 0; i < LockedExits.Length; i++)
        {
            if (LockedExits[i] != null)
                LockedExits[i].SetActive(!cleared);
        }
    }

    private void Update()
    {
        if (!_encounterActive || Cleared || ActiveRoomModifier == null || !ActiveRoomModifier.SpawnPeriodicHazards)
            return;

        _hazardTimer -= Time.deltaTime;
        if (_hazardTimer <= 0f)
        {
            _hazardTimer = ActiveRoomModifier.HazardInterval;
            SpawnEnvironmentalHazard();
        }
    }

    private void SpawnEnvironmentalHazard()
    {
        if (_trackedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _trackedPlayer = p.transform;
        }

        Vector3 anchor = _trackedPlayer != null ? _trackedPlayer.position : transform.position;
        Vector2 offset2D = Random.insideUnitCircle * 3.5f;
        Vector3 candidate = anchor + new Vector3(offset2D.x, 0f, offset2D.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            candidate = hit.position;

        ArenaHazardZone.Create(
            candidate,
            ActiveRoomModifier.HazardRadius,
            ActiveRoomModifier.HazardDamagePerTick,
            ActiveRoomModifier.HazardDuration,
            0.7f,
            gameObject,
            PlayerLayerMask
        );
    }
}