using System.Collections.Generic;
using UnityEngine;

public enum CellType
{
    Free,
    Building,
    Wall,
    Water,
    Mountain
}

/// <summary>
/// Central grid occupancy map used by pathfinding.
/// Static cells are rebuilt from scene props (water/mountains),
/// dynamic cells are updated by BuildingManager on build/destroy.
/// </summary>
public class GridMap : MonoBehaviour
{
    public static GridMap Instance { get; private set; }

    [Header("Grid")]
    public float GridSize = 2f;
    public Vector2Int MinCell = new Vector2Int(-96, -96);
    public Vector2Int MaxCell = new Vector2Int(96, 96);

    readonly Dictionary<Vector2Int, CellType> _staticCells = new Dictionary<Vector2Int, CellType>();
    readonly Dictionary<Vector2Int, CellType> _dynamicCells = new Dictionary<Vector2Int, CellType>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Configure(float gridSize, Vector2Int minCell, Vector2Int maxCell)
    {
        GridSize = Mathf.Max(0.25f, gridSize);
        MinCell = minCell;
        MaxCell = maxCell;
    }

    public bool IsWithinBounds(Vector2Int cell)
    {
        return cell.x >= MinCell.x && cell.x <= MaxCell.x &&
               cell.y >= MinCell.y && cell.y <= MaxCell.y;
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / GridSize),
            Mathf.RoundToInt(world.z / GridSize));
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * GridSize, 0f, cell.y * GridSize);
    }

    public bool IsWalkable(Vector2Int cell)
    {
        if (!IsWithinBounds(cell)) return false;

        CellType type;
        if (_dynamicCells.TryGetValue(cell, out type))
            return type == CellType.Free;

        if (_staticCells.TryGetValue(cell, out type))
            return type == CellType.Free;

        return true;
    }

    public void SetDynamicArea(Vector2Int origin, int width, int height, CellType type)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var c = new Vector2Int(origin.x + x, origin.y + z);
                if (!IsWithinBounds(c)) continue;

                if (type == CellType.Free) _dynamicCells.Remove(c);
                else _dynamicCells[c] = type;
            }
        }
    }

    public void ClearDynamicArea(Vector2Int origin, int width, int height)
    {
        SetDynamicArea(origin, width, height, CellType.Free);
    }

    public void RebuildStaticObstacles()
    {
        _staticCells.Clear();

        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject == null) continue;
            string n = r.gameObject.name;

            CellType type;
            bool shouldMark = false;

            if (n.Contains("Lake") || n.Contains("Water"))
            {
                type = CellType.Water;
                shouldMark = true;
            }
            else if (n.Contains("Mountain"))
            {
                type = CellType.Mountain;
                shouldMark = true;
            }
            else
            {
                continue;
            }

            MarkBounds(r.bounds, type);
        }
    }

    void MarkBounds(Bounds b, CellType type)
    {
        Vector2Int min = WorldToGrid(b.min);
        Vector2Int max = WorldToGrid(b.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                var c = new Vector2Int(x, z);
                if (!IsWithinBounds(c)) continue;
                _staticCells[c] = type;
            }
        }
    }
}