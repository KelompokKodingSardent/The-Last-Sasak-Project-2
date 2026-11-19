using System.Collections.Generic;
using UnityEngine;

public class RoomGraph
{
    public readonly List<RoomPlacement> Rooms = new List<RoomPlacement>();

    public RoomPlacement Add(Vector2Int grid, RoomType type)
    {
        RoomPlacement placement = new RoomPlacement { Grid = grid, Type = type };
        Rooms.Add(placement);
        return placement;
    }
}

public class RoomPlacement
{
    public Vector2Int Grid;
    public RoomType Type;
    public Vector3 WorldPosition;
    public RoomController Controller;
}
