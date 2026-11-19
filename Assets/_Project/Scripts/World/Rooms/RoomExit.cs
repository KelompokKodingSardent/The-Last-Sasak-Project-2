using UnityEngine;

public class RoomExit : MonoBehaviour
{
    public RoomController Room;
    public Vector3 NextOffset = new Vector3(0f, 0f, 16f);

    void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;
        if (Room != null && !Room.Cleared)
            return;

        RoomManager.Instance?.Advance(other.transform);
    }
}
