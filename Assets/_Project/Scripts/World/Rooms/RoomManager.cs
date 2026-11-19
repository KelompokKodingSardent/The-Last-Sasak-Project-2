using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public RoomController ActiveRoom { get; private set; }
    public RoomGraph Graph;
    public RoomController[] ExampleRooms;
    public Transform Player;
    public bool StartOnPlay = true;
    RoomEncounterController encounter;
    int index;

    void Awake()
    {
        Instance = this;
        encounter = GetComponent<RoomEncounterController>();
    }

    void Start()
    {
        if (!StartOnPlay)
            return;
        if (Player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Player = player.transform;
        }

        if (ExampleRooms != null && ExampleRooms.Length > 0)
            Activate(ExampleRooms[0], Player);
    }

    public void Activate(RoomController room, Transform player)
    {
        ActiveRoom = room;
        if (Graph != null)
        {
            for (int i = 0; i < Graph.Rooms.Count; i++)
            {
                if (Graph.Rooms[i].Controller == room)
                    index = i;
            }
        }

        if (encounter != null)
        {
            encounter.Player = player;
            encounter.StartEncounter(room);
        }
    }

    public void Advance(Transform player)
    {
        if (player == null)
            return;

        if (ExampleRooms != null && ExampleRooms.Length > 0)
        {
            if (index + 1 >= ExampleRooms.Length)
                return;
            index++;
            RoomController nextRoom = ExampleRooms[index];
            if (nextRoom != null && nextRoom.PlayerSpawn != null)
                player.position = nextRoom.PlayerSpawn.position + Vector3.up;
            Activate(nextRoom, player);
            return;
        }

        if (Graph == null || index + 1 >= Graph.Rooms.Count)
            return;

        index++;
        RoomPlacement next = Graph.Rooms[index];
        if (next.Controller != null && next.Controller.PlayerSpawn != null)
            player.position = next.Controller.PlayerSpawn.position + Vector3.up;
        else
            player.position = next.WorldPosition + Vector3.up;

        Activate(next.Controller, player);
    }

    void OnEnable()
    {
        GameEvents.OnRoomCleared += HandleCleared;
    }

    void OnDisable()
    {
        GameEvents.OnRoomCleared -= HandleCleared;
        if (Instance == this)
            Instance = null;
    }

    void HandleCleared()
    {
        ActiveRoom?.SetCleared(true);
    }
}
