using System.Collections.Generic;
using UnityEngine;

public class RoomFootprint : MonoBehaviour
{
    public const float CellSize = 12f;

    [Header("Grid Dimensions (1 Cell = 12x12 Units)")]
    [Min(1)] public int WidthCells = 2;
    [Min(1)] public int DepthCells = 2;

    [Header("Gizmo Visualization")]
    public bool ShowGridGizmos = true;
    public Color FootprintColor = new Color(0.15f, 0.85f, 0.45f, 0.85f);

    public Vector2Int SizeInCells => new Vector2Int(Mathf.Max(1, WidthCells), Mathf.Max(1, DepthCells));
    public Vector2 WorldDimensions => new Vector2(WidthCells * CellSize, DepthCells * CellSize);

    /// <summary>
    /// Mengembalikan daftar koordinat grid (X, Z) dunia yang ditempati ruangan ini
    /// berdasarkan posisi root (kelipatan 12) dan rotasi kelipatan 90 derajat.
    /// </summary>
    public List<Vector2Int> GetOccupiedCells(Vector3 rootWorldPosition, Quaternion rootWorldRotation)
    {
        List<Vector2Int> cells = new List<Vector2Int>(WidthCells * DepthCells);

        for (int x = 0; x < WidthCells; x++)
        {
            for (int z = 0; z < DepthCells; z++)
            {
                Vector3 localCellCenter = new Vector3((x + 0.5f) * CellSize, 0f, (z + 0.5f) * CellSize);
                Vector3 worldCellCenter = rootWorldPosition + (rootWorldRotation * localCellCenter);
                cells.Add(WorldToGrid(worldCellCenter));
            }
        }

        return cells;
    }

    public List<Vector2Int> GetCurrentOccupiedCells()
    {
        return GetOccupiedCells(transform.position, transform.rotation);
    }

    public Bounds GetLocalBounds()
    {
        Vector3 size = new Vector3(WidthCells * CellSize, 6f, DepthCells * CellSize);
        Vector3 center = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        return new Bounds(center, size);
    }

    public bool IsLocalPointInside(Vector3 localPoint, float margin = 0.05f)
    {
        float maxX = WidthCells * CellSize;
        float maxZ = DepthCells * CellSize;
        return localPoint.x >= -margin && localPoint.x <= maxX + margin &&
               localPoint.z >= -margin && localPoint.z <= maxZ + margin;
    }

    public static Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / CellSize),
            Mathf.FloorToInt(worldPosition.z / CellSize)
        );
    }

    public static Vector3 GridToWorldOrigin(Vector2Int gridCell)
    {
        return new Vector3(gridCell.x * CellSize, 0f, gridCell.y * CellSize);
    }

    private void OnDrawGizmos()
    {
        if (!ShowGridGizmos)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float totalW = WidthCells * CellSize;
        float totalD = DepthCells * CellSize;

        // Gambar garis pembatas setiap cell 12x12 pada Y = 0
        Gizmos.color = FootprintColor;
        for (int x = 0; x <= WidthCells; x++)
        {
            float posX = x * CellSize;
            Gizmos.DrawLine(new Vector3(posX, 0.02f, 0f), new Vector3(posX, 0.02f, totalD));
        }

        for (int z = 0; z <= DepthCells; z++)
        {
            float posZ = z * CellSize;
            Gizmos.DrawLine(new Vector3(0f, 0.02f, posZ), new Vector3(totalW, 0.02f, posZ));
        }

        // Gambar volume bounding box tipis
        Gizmos.color = new Color(FootprintColor.r, FootprintColor.g, FootprintColor.b, 0.25f);
        Bounds localBounds = GetLocalBounds();
        Gizmos.DrawWireCube(localBounds.center, localBounds.size);

        // Penanda pivot (0,0,0)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Vector3.zero, 0.4f);

        Gizmos.matrix = previousMatrix;
    }
}