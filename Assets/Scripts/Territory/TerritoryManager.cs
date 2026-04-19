using UnityEngine;
using System.Collections.Generic;

public class TerritoryManager : MonoBehaviour
{
    public static TerritoryManager Instance { get; private set; }

    [Header("Territory Visuals")]
    public float TerritoryRadius = 8f;
    public Color TerritoryColor = new Color(0.3f, 0.6f, 1f, 0.15f);
    public Color BorderColor = new Color(0.3f, 0.6f, 1f, 0.6f);

    private HashSet<Vector2Int> _territory = new HashSet<Vector2Int>();
    private List<GameObject> _territoryMarkers = new List<GameObject>();
    private GameObject _territoryParent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _territoryParent = new GameObject("TerritoryMarkers");
    }

    void Start()
    {
        // Initial territory around origin
        ExpandTerritory(Vector2Int.zero, 4, 4);
    }

    public void ExpandTerritory(Vector2Int center, int width, int height)
    {
        int radius = Mathf.RoundToInt(TerritoryRadius / 2f);
        List<Vector2Int> newCells = new List<Vector2Int>();

        for (int x = -radius; x <= radius + width; x++)
        {
            for (int z = -radius; z <= radius + height; z++)
            {
                Vector2Int cell = new Vector2Int(center.x + x, center.y + z);
                if (!_territory.Contains(cell))
                {
                    _territory.Add(cell);
                    newCells.Add(cell);
                }
            }
        }

        foreach (var cell in newCells)
            CreateTerritoryMarker(cell);
    }

    void CreateTerritoryMarker(Vector2Int cell)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Plane);
        marker.transform.SetParent(_territoryParent.transform);
        marker.transform.position = new Vector3(cell.x * 2f, 0.01f, cell.y * 2f);
        marker.transform.localScale = new Vector3(0.19f, 1f, 0.19f);
        Destroy(marker.GetComponent<Collider>());

        var rend = marker.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = TerritoryColor;
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        rend.material = mat;

        _territoryMarkers.Add(marker);
    }

    public bool IsInTerritory(Vector2Int cell) => _territory.Contains(cell);
    public int GetTerritorySize() => _territory.Count;
}
