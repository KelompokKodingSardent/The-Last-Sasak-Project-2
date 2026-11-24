using UnityEngine;

public enum RoomState
{
    Unvisited,
    ActiveCombat,
    Cleared,
    RewardReady
}

public enum ArenaLayoutArchetype
{
    Open,
    Pillar,
    Narrow,
    Arena
}

public class RoomController : MonoBehaviour
{
    [Header("Room Identity & Layout")]
    public RoomRole Role = RoomRole.Combat;
    public ArenaLayoutArchetype LayoutArchetype = ArenaLayoutArchetype.Open;

    [Header("Anchors & Encounter")]
    public Transform PlayerSpawn;
    public EnemySpawnPoint[] SpawnPoints;
    public EncounterCompositionSO CuratedComposition;
    public RoomModifierSO ActiveRoomModifier;

    [Header("Physical Traversal Trigger")]
    [Tooltip("Otomatis membuat BoxCollider trigger di dalam batas RoomFootprint jika belum ada.")]
    [SerializeField] private bool autoCreateEntryTrigger = true;
    [Tooltip("Jarak masuk dari tepi pintu agar CombatGate tidak menjepit Player di ambang pintu.")]
    [SerializeField] private float entryTriggerInset = 1.75f;

    public RoomState State { get; private set; } = RoomState.Unvisited;
    public bool Cleared => State == RoomState.Cleared || State == RoomState.RewardReady;
    public int NodeId { get; private set; } = -1;
    public bool IsMainPath { get; private set; } = true;
    public RoomConnector[] Connectors { get; private set; }

    private BoxCollider entryTrigger;
    private bool initializedFromGraph;

    private void Awake()
    {
        CacheComponents();
        EnsureEntryTrigger();
    }

    private void CacheComponents()
    {
        Connectors = GetComponentsInChildren<RoomConnector>(true);

        if (SpawnPoints == null || SpawnPoints.Length == 0)
            SpawnPoints = GetComponentsInChildren<EnemySpawnPoint>(true);
    }

    /// <summary>
    /// Dipanggil oleh RoomManager / WorldGenerationCoordinator setelah graph terpasang.
    /// </summary>
    public void InitializeFromGraph(RoomNodeData nodeData)
    {
        CacheComponents();
        EnsureEntryTrigger();

        if (nodeData == null)
            return;

        initializedFromGraph = true;
        NodeId = nodeData.NodeId;
        Role = nodeData.Role;
        IsMainPath = nodeData.IsMainPath;

        if (nodeData.AssignedRoomDef != null)
            LayoutArchetype = nodeData.AssignedRoomDef.LayoutArchetype;

        if (nodeData.AssignedEncounter != null)
            CuratedComposition = nodeData.AssignedEncounter;

        // Ruang Start langsung Cleared agar gerbang tidak terkunci saat spawn
        if (Role == RoomRole.Start)
        {
            State = RoomState.Cleared;
            SetAllCombatGatesLocked(false);
        }
        else
        {
            State = RoomState.Unvisited;
            SetAllCombatGatesLocked(false);
        }
    }

    private void EnsureEntryTrigger()
    {
        if (!autoCreateEntryTrigger || entryTrigger != null)
            return;

        // Cari trigger collider yang sudah ada di root jika desainer memasangnya manual
        BoxCollider[] existingBoxes = GetComponents<BoxCollider>();
        for (int i = 0; i < existingBoxes.Length; i++)
        {
            if (existingBoxes[i].isTrigger)
            {
                entryTrigger = existingBoxes[i];
                return;
            }
        }

        RoomFootprint footprint = GetComponent<RoomFootprint>();
        if (footprint == null)
            return;

        float totalW = footprint.WidthCells * RoomFootprint.CellSize;
        float totalD = footprint.DepthCells * RoomFootprint.CellSize;
        float inset = Mathf.Min(entryTriggerInset, Mathf.Min(totalW, totalD) * 0.25f);

        entryTrigger = gameObject.AddComponent<BoxCollider>();
        entryTrigger.isTrigger = true;
        entryTrigger.center = new Vector3(totalW * 0.5f, 2.0f, totalD * 0.5f);
        entryTrigger.size = new Vector3(
            Mathf.Max(2f, totalW - (inset * 2f)),
            4.0f,
            Mathf.Max(2f, totalD - (inset * 2f))
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        HandlePlayerEntered(other.transform);
    }

    public void HandlePlayerEntered(Transform playerTransform)
    {
        GameEvents.RaiseRoomEntered(this);

        if (State != RoomState.Unvisited)
            return;

        if (ShouldTriggerCombatOnEnter())
        {
            State = RoomState.ActiveCombat;
            SetAllCombatGatesLocked(true);

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.NotifyCombatRoomEntered(this, playerTransform);
            }
            else
            {
                RoomEncounterController fallbackEncounter = FindFirstObjectByType<RoomEncounterController>();
                if (fallbackEncounter != null)
                {
                    if (fallbackEncounter.Player == null)
                        fallbackEncounter.Player = playerTransform;
                    fallbackEncounter.StartEncounter(this);
                }
            }
            return;
        }

        if (Role == RoomRole.Reward)
        {
            State = RoomState.RewardReady;
            return;
        }

        State = RoomState.Cleared;
    }

    /// <summary>
    /// Dipanggil oleh RoomEncounterController saat gelombang musuh dimulai.
    /// </summary>
    public void BeginRoomEncounter(Transform player, int runDepth)
    {
        if (Cleared)
            return;

        State = RoomState.ActiveCombat;
        SetAllCombatGatesLocked(true);
    }

    /// <summary>
    /// Dipanggil saat seluruh musuh di ruangan ini dikalahkan.
    /// </summary>
    public void CompleteRoomEncounter()
    {
        if (Cleared)
            return;

        State = RoomState.Cleared;
        SetAllCombatGatesLocked(false);
    }

    public void SetAllCombatGatesLocked(bool locked)
    {
        if (Connectors == null)
            return;

        for (int i = 0; i < Connectors.Length; i++)
        {
            RoomConnector conn = Connectors[i];
            if (conn == null || !conn.IsConnected)
                continue;

            conn.SetCombatGateLocked(locked);

            // Kunci juga CombatGate milik ruangan tetangga yang terhubung pada pintu ini
            if (conn.ConnectedPartner != null)
                conn.ConnectedPartner.SetCombatGateLocked(locked);
        }
    }

    private bool ShouldTriggerCombatOnEnter()
    {
        if (!initializedFromGraph)
        {
            // Mode legacy / standalone scene: cek apakah ada encounter atau spawn point
            return (CuratedComposition != null && CuratedComposition.Enemies.Count > 0) ||
                   (SpawnPoints != null && SpawnPoints.Length > 0);
        }

        if (Role == RoomRole.Combat || Role == RoomRole.Elite)
            return true;

        // Izinkan Exit room memiliki encounter penjaga jika di-assign CuratedComposition
        if (Role == RoomRole.Exit && CuratedComposition != null && CuratedComposition.Enemies.Count > 0)
            return true;

        return false;
    }
}