using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    [Tooltip("Biarkan true untuk scene statis (ForestRun), set false untuk scene prosedural (WorldGenerationTest).")]
    public bool BakeOnStart = true;

    public bool IsBaked { get; private set; }

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    private void Start()
    {
        if (BakeOnStart)
            BakeNavMeshRuntime();
    }

    public bool BakeNavMeshRuntime()
    {
        if (navMeshSurface == null)
            navMeshSurface = GetComponent<NavMeshSurface>();

        if (navMeshSurface == null)
        {
            Debug.LogError("[RuntimeNavMeshBuilder] Komponen NavMeshSurface tidak ditemukan.");
            IsBaked = false;
            return false;
        }

        navMeshSurface.BuildNavMesh();
        IsBaked = navMeshSurface.navMeshData != null;
        return IsBaked;
    }

    public void ClearNavMesh()
    {
        if (navMeshSurface != null)
            navMeshSurface.RemoveData();

        IsBaked = false;
    }

    public bool TrySamplePosition(Vector3 rawPosition, float maxDistance, out Vector3 validPosition)
    {
        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            validPosition = hit.position;
            return true;
        }

        validPosition = rawPosition;
        return false;
    }
}