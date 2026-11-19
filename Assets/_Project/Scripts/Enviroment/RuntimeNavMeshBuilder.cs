using UnityEngine;
using Unity.AI.Navigation; // Wajib menggunakan package AI Navigation

[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    public bool BakeOnStart = true;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    void Start()
    {
        if (BakeOnStart)
            BakeNavMeshRuntime();
    }

    // Dipanggil oleh MapGenerator setelah semua aset 3D terpasang di posisi akhirnya
    public void BakeNavMeshRuntime()
    {
        if (navMeshSurface != null)
        {
            // Membangun ulang NavMesh secara real-time
            navMeshSurface.BuildNavMesh();
            Debug.Log("Runtime NavMesh Baking Selesai! AI siap bergerak.");
        }
        else
        {
            Debug.LogError("Komponen NavMeshSurface tidak ditemukan!");
        }
    }
}