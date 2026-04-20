using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* pathfinder over GridMap with short-lived cache.
/// </summary>
public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance { get; private set; }

    struct CacheEntry
    {
        public float time;
        public List<Vector3> path;
    }

    readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
    const float CacheTTL = 2f;

    static readonly Vector2Int[] Neighbors =
    {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int( 1,  1),
        new Vector2Int(-1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int(-1, -1),
    };

    class Node
    {
        public Vector2Int cell;
        public float g;
        public float h;
        public Node parent;
        public float f => g + h;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
    {
        var grid = GridMap.Instance;
        if (grid == null) return null;

        Vector2Int start = grid.WorldToGrid(startWorld);
        Vector2Int goal = grid.WorldToGrid(endWorld);

        if (!grid.IsWithinBounds(start)) return null;
        if (!grid.IsWalkable(goal))
            goal = FindNearestWalkable(goal, 6);

        if (!grid.IsWithinBounds(goal) || !grid.IsWalkable(goal)) return null;

        string key = BuildKey(start, goal);
        CacheEntry cached;
        if (_cache.TryGetValue(key, out cached) && Time.time - cached.time <= CacheTTL)
            return new List<Vector3>(cached.path);

        var pathCells = FindPathCells(start, goal);
        if (pathCells == null || pathCells.Count == 0) return null;

        var pathWorld = new List<Vector3>(pathCells.Count);
        for (int i = 0; i < pathCells.Count; i++)
            pathWorld.Add(grid.GridToWorld(pathCells[i]));

        _cache[key] = new CacheEntry
        {
            time = Time.time,
            path = new List<Vector3>(pathWorld)
        };

        return pathWorld;
    }

    Vector2Int FindNearestWalkable(Vector2Int center, int radius)
    {
        var grid = GridMap.Instance;
        if (grid == null) return center;
        if (grid.IsWalkable(center)) return center;

        for (int r = 1; r <= radius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    var c = new Vector2Int(center.x + x, center.y + y);
                    if (grid.IsWalkable(c)) return c;
                }
            }
        }

        return center;
    }

    List<Vector2Int> FindPathCells(Vector2Int start, Vector2Int goal)
    {
        var grid = GridMap.Instance;
        if (grid == null) return null;

        var open = new List<Node>(64);
        var openMap = new Dictionary<Vector2Int, Node>(64);
        var closed = new HashSet<Vector2Int>();

        var startNode = new Node
        {
            cell = start,
            g = 0f,
            h = Heuristic(start, goal),
            parent = null
        };

        open.Add(startNode);
        openMap[start] = startNode;

        while (open.Count > 0)
        {
            int bestIndex = 0;
            float bestF = open[0].f;
            for (int i = 1; i < open.Count; i++)
            {
                float f = open[i].f;
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            var current = open[bestIndex];
            open.RemoveAt(bestIndex);
            openMap.Remove(current.cell);
            closed.Add(current.cell);

            if (current.cell == goal)
                return Reconstruct(current);

            for (int i = 0; i < Neighbors.Length; i++)
            {
                Vector2Int dir = Neighbors[i];
                var next = current.cell + dir;

                if (closed.Contains(next)) continue;
                if (!grid.IsWalkable(next)) continue;

                // Prevent cutting corners through blocked orthogonals.
                if (dir.x != 0 && dir.y != 0)
                {
                    var sideA = new Vector2Int(current.cell.x + dir.x, current.cell.y);
                    var sideB = new Vector2Int(current.cell.x, current.cell.y + dir.y);
                    if (!grid.IsWalkable(sideA) || !grid.IsWalkable(sideB))
                        continue;
                }

                float step = (dir.x != 0 && dir.y != 0) ? 1.4f : 1f;
                float candidateG = current.g + step;

                Node existing;
                if (openMap.TryGetValue(next, out existing))
                {
                    if (candidateG < existing.g)
                    {
                        existing.g = candidateG;
                        existing.parent = current;
                    }
                }
                else
                {
                    var n = new Node
                    {
                        cell = next,
                        g = candidateG,
                        h = Heuristic(next, goal),
                        parent = current
                    };
                    open.Add(n);
                    openMap[next] = n;
                }
            }
        }

        return null;
    }

    static List<Vector2Int> Reconstruct(Node end)
    {
        var path = new List<Vector2Int>(32);
        var n = end;
        while (n != null)
        {
            path.Add(n.cell);
            n = n.parent;
        }
        path.Reverse();
        return path;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    static string BuildKey(Vector2Int a, Vector2Int b)
    {
        return a.x + "," + a.y + "->" + b.x + "," + b.y;
    }
}