using UnityEngine;

[CreateAssetMenu(menuName = "TLS/Rooms/Room Catalog")]
public class RoomCatalogSO : ScriptableObject
{
    public RoomDefinitionSO[] Rooms;
}
