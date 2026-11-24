using System.Collections.Generic;
using UnityEngine;

public enum RoomRole
{
    Start,
    Traversal,
    Combat,
    Reward,
    Elite,
    Exit
}

[CreateAssetMenu(fileName = "RoomDef_Forest_New", menuName = "TLS/World/Room Definition")]
public class RoomDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string RoomID = "forest_combat_open_01";
    public string DisplayName = "Forest Clearing";
    public BiomeType Biome = BiomeType.Forest;
    public RoomRole Role = RoomRole.Combat;
    public ArenaLayoutArchetype LayoutArchetype = ArenaLayoutArchetype.Open;

    [Header("Prefab Reference")]
    public GameObject Prefab;

    [Header("Tags & Encounter Compatibility")]
    public List<string> RoomTags = new List<string>();
    [Tooltip("Jika diisi, generator hanya memilih encounter dari daftar ini untuk ruangan ini.")]
    public List<EncounterCompositionSO> OverrideEncounters = new List<EncounterCompositionSO>();

    [Header("Placement & Rotation Rules")]
    public bool AllowRotate90 = true;
    public bool AllowRotate180 = true;
    public bool AllowRotate270 = true;

    [Header("Run Depth & Weighting")]
    [Min(1)] public int MinRunDepth = 1;
    [Min(1)] public int MaxRunDepth = 99;
    [Min(0.01f)] public float GenerationWeight = 1f;
    [Tooltip("0 = tidak dibatasi dalam satu run.")]
    [Min(0)] public int MaxInstancesPerRun = 0;

    [Header("Cached Prefab Metadata (Auto-Synced)")]
    [SerializeField] private Vector2Int footprintCells = new Vector2Int(2, 2);
    [SerializeField] private List<Direction> localConnectorDirections = new List<Direction>();

    public Vector2Int FootprintCells => footprintCells;
    public IReadOnlyList<Direction> LocalConnectorDirections => localConnectorDirections;

    public List<int> GetAllowedRotationAngles()
    {
        List<int> angles = new List<int>(4) { 0 };
        if (AllowRotate90) angles.Add(90);
        if (AllowRotate180) angles.Add(180);
        if (AllowRotate270) angles.Add(270);
        return angles;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || RoomTags == null)
            return false;

        for (int i = 0; i < RoomTags.Count; i++)
        {
            if (string.Equals(RoomTags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool IsDepthAllowed(int runDepth)
    {
        return runDepth >= MinRunDepth && runDepth <= MaxRunDepth;
    }

    public void SyncFromPrefab()
    {
        if (Prefab == null)
            return;

        RoomFootprint fp = Prefab.GetComponent<RoomFootprint>();
        if (fp != null)
            footprintCells = fp.SizeInCells;

        localConnectorDirections.Clear();
        RoomConnector[] connectors = Prefab.GetComponentsInChildren<RoomConnector>(true);
        for (int i = 0; i < connectors.Length; i++)
        {
            if (connectors[i] != null)
                localConnectorDirections.Add(connectors[i].GetWorldDirection());
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (MaxRunDepth < MinRunDepth)
            MaxRunDepth = MinRunDepth;

        SyncFromPrefab();
    }
#endif
}