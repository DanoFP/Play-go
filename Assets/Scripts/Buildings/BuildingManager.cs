using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Setup")]
    public List<BuildingData> AvailableBuildings = new List<BuildingData>();
    public Material PreviewMaterial;
    public LayerMask GroundLayer;

    [Header("Grid")]
    public float GridSize = 2f;

    private BuildingData _selectedBuildingData;
    private GameObject _previewObject;
    private Dictionary<Vector2Int, Building> _placedBuildings = new Dictionary<Vector2Int, Building>();
    private HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();
    private Building _currentlySelected;
    private bool _isPlacementMode = false;

    public bool IsPlacementMode => _isPlacementMode;
    public Building SelectedBuilding => _currentlySelected;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (_isPlacementMode)
        {
            UpdatePreview();
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                TryPlaceBuilding();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
        }
        else
        {
            HandleSelection();
        }
    }

    void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Building b = hit.collider.GetComponentInParent<Building>();
                if (b != null)
                {
                    SelectBuilding(b);
                    return;
                }
            }
            DeselectAll();
        }
    }

    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;
        if (!ResourceManager.Instance.CanAfford(data.GetCostDict()))
        {
            UIManager.Instance?.ShowMessage("Not enough resources!");
            return;
        }
        _selectedBuildingData = data;
        _isPlacementMode = true;
        DeselectAll();
        CreatePreview(data);
    }

    void CreatePreview(BuildingData data)
    {
        if (_previewObject != null) Destroy(_previewObject);
        _previewObject = CreateBuildingMesh(data);
        var renderers = _previewObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        }
        // Remove colliders from preview
        foreach (var c in _previewObject.GetComponentsInChildren<Collider>())
            Destroy(c);
    }

    void UpdatePreview()
    {
        if (_previewObject == null) return;
        Vector3 worldPos = GetMouseWorldPosition();
        Vector2Int gridPos = WorldToGrid(worldPos);
        Vector3 snappedPos = GridToWorld(gridPos);
        _previewObject.transform.position = snappedPos;

        bool canPlace = CanPlaceAt(gridPos, _selectedBuildingData.Width, _selectedBuildingData.Height);
        Color previewColor = canPlace ? new Color(0.2f, 0.9f, 0.2f, 0.5f) : new Color(0.9f, 0.2f, 0.2f, 0.5f);
        foreach (var r in _previewObject.GetComponentsInChildren<Renderer>())
            r.material.color = previewColor;
    }

    void TryPlaceBuilding()
    {
        Vector3 worldPos = GetMouseWorldPosition();
        Vector2Int gridPos = WorldToGrid(worldPos);

        if (!CanPlaceAt(gridPos, _selectedBuildingData.Width, _selectedBuildingData.Height))
        {
            UIManager.Instance?.ShowMessage("Cannot place here!");
            return;
        }

        if (!ResourceManager.Instance.SpendResources(_selectedBuildingData.GetCostDict()))
        {
            UIManager.Instance?.ShowMessage("Not enough resources!");
            return;
        }

        PlaceBuilding(_selectedBuildingData, gridPos);
        CancelPlacement();
    }

    void PlaceBuilding(BuildingData data, Vector2Int gridPos)
    {
        Vector3 worldPos = GridToWorld(gridPos);
        GameObject obj = CreateBuildingMesh(data);
        obj.transform.position = worldPos;

        Building building = obj.AddComponent<Building>();
        building.Data = data;
        building.GridPosition = gridPos;

        // Mark cells as occupied
        for (int x = 0; x < data.Width; x++)
            for (int z = 0; z < data.Height; z++)
                _occupiedCells.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));

        _placedBuildings[gridPos] = building;
    }

    GameObject CreateBuildingMesh(BuildingData data)
    {
        GameObject root = new GameObject(data.BuildingName);
        float w = data.Width * GridSize * 0.9f;
        float h = data.Height * GridSize * 0.9f;

        switch (data.Type)
        {
            case BuildingType.TownCenter:
                CreateTownCenter(root, w, h, data.BuildingColor);
                break;
            case BuildingType.Tower:
                CreateTower(root, w, h, data.BuildingColor);
                break;
            case BuildingType.Farm:
                CreateFarm(root, w, h, data.BuildingColor);
                break;
            default:
                CreateGenericBuilding(root, w, h, data.BuildingColor);
                break;
        }

        return root;
    }

    void CreateGenericBuilding(GameObject root, float w, float h, Color color)
    {
        // Base
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(w, 1f, h);
        body.GetComponent<Renderer>().material.color = color;

        // Roof
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 1.2f, 0);
        roof.transform.localScale = new Vector3(w * 0.8f, 0.4f, h * 0.8f);
        roof.GetComponent<Renderer>().material.color = color * 0.7f;
        Destroy(roof.GetComponent<Collider>());
    }

    void CreateTownCenter(GameObject root, float w, float h, Color color)
    {
        CreateGenericBuilding(root, w, h, color);
        // Central tower
        var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.transform.SetParent(root.transform);
        tower.transform.localPosition = new Vector3(0, 1.5f, 0);
        tower.transform.localScale = new Vector3(w * 0.3f, 1f, w * 0.3f);
        tower.GetComponent<Renderer>().material.color = color * 1.2f;
        Destroy(tower.GetComponent<Collider>());
    }

    void CreateTower(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.5f, 0);
        body.transform.localScale = new Vector3(w * 0.7f, 1.5f, w * 0.7f);
        body.GetComponent<Renderer>().material.color = color;

        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 3.2f, 0);
        top.transform.localScale = new Vector3(w * 0.9f, 0.5f, w * 0.9f);
        top.GetComponent<Renderer>().material.color = color * 0.8f;
        Destroy(top.GetComponent<Collider>());
    }

    void CreateFarm(GameObject root, float w, float h, Color color)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(root.transform);
        ground.transform.localPosition = new Vector3(0, 0.05f, 0);
        ground.transform.localScale = new Vector3(w, 0.1f, h);
        ground.GetComponent<Renderer>().material.color = new Color(0.6f, 0.8f, 0.3f);

        // Rows of crops
        for (int i = -1; i <= 1; i++)
        {
            var row = GameObject.CreatePrimitive(PrimitiveType.Cube);
            row.transform.SetParent(root.transform);
            row.transform.localPosition = new Vector3(i * (w / 3f), 0.2f, 0);
            row.transform.localScale = new Vector3(w * 0.15f, 0.3f, h * 0.9f);
            row.GetComponent<Renderer>().material.color = new Color(0.2f, 0.7f, 0.1f);
            Destroy(row.GetComponent<Collider>());
        }
    }

    bool CanPlaceAt(Vector2Int gridPos, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (_occupiedCells.Contains(new Vector2Int(gridPos.x + x, gridPos.y + z)))
                    return false;
        return true;
    }

    public void RemoveBuilding(Building building)
    {
        if (building == null || building.Data == null) return;
        for (int x = 0; x < building.Data.Width; x++)
            for (int z = 0; z < building.Data.Height; z++)
                _occupiedCells.Remove(new Vector2Int(building.GridPosition.x + x, building.GridPosition.y + z));
        _placedBuildings.Remove(building.GridPosition);
    }

    public void SelectBuilding(Building b)
    {
        DeselectAll();
        _currentlySelected = b;
        b.Select();
        UIManager.Instance?.ShowBuildingInfo(b);
    }

    public void DeselectAll()
    {
        if (_currentlySelected != null)
        {
            _currentlySelected.Deselect();
            _currentlySelected = null;
        }
        UIManager.Instance?.HideBuildingInfo();
    }

    public void CancelPlacement()
    {
        _isPlacementMode = false;
        _selectedBuildingData = null;
        if (_previewObject != null) { Destroy(_previewObject); _previewObject = null; }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        return Vector3.zero;
    }

    Vector2Int WorldToGrid(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / GridSize),
            Mathf.RoundToInt(world.z / GridSize)
        );
    }

    Vector3 GridToWorld(Vector2Int grid)
    {
        return new Vector3(grid.x * GridSize, 0f, grid.y * GridSize);
    }

    public int GetBuildingCount() => _placedBuildings.Count;
}
