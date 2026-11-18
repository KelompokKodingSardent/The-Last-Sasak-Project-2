using UnityEngine;
using Unity.AI.Navigation; // Wajib menggunakan package AI Navigation

[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    private void Awake()
    {
        // Mengambil komponen NavMeshSurface yang menempel pada GameObject ini
        navMeshSurface = GetComponent<NavMeshSurface>();
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