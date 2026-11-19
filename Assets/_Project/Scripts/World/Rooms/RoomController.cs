using UnityEngine;

public class RoomController : MonoBehaviour
{
    public bool Cleared { get; private set; }
    public Transform PlayerSpawn;
    public EnemySpawnPoint[] SpawnPoints;
    public GameObject[] LockedExits;

    public void SetCleared(bool cleared)
    {
        Cleared = cleared;
        if (LockedExits == null)
            return;
        for (int i = 0; i < LockedExits.Length; i++)
        {
            if (LockedExits[i] != null)
                LockedExits[i].SetActive(!cleared);
        }
    }
}
