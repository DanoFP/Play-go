using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Setup")]
    public List<BuildingData> AvailableBuildings = new List<BuildingData>();
    public LayerMask GroundLayer;

    [Header("Grid")]
    public float GridSize = 2f;

    // ── Trainable units per building type ─────────────────────────────────────
    // Populated by SceneSetup.
    public Dictionary<BuildingType, List<UnitData>> TrainableUnits
        = new Dictionary<BuildingType, List<UnitData>>();

    // ── Internal state ────────────────────────────────────────────────────────
    BuildingData  _selectedBuildingData;
    GameObject    _previewObject;
    Dictionary<Vector2Int, Building> _placedBuildings = new Dictionary<Vector2Int, Building>();
    HashSet<Vector2Int>              _occupiedCells   = new HashSet<Vector2Int>();

    Building     _currentlySelected;
    Villager     _selectedVillager;
    MilitaryUnit _selectedMilitary;
    bool         _isPlacementMode;

    // ── Multi-select drag ─────────────────────────────────────────────────────
    List<MilitaryUnit> _selectedGroup = new List<MilitaryUnit>();
    bool    _isDragging;
    Vector2 _dragStart;
    Vector2 _dragEnd;
    const float DragThreshold = 6f; // pixels before drag starts

    // ── Wall line placement ───────────────────────────────────────────────────
    bool       _wallMode;
    bool       _wallFirstSet;
    Vector2Int _wallStart;
    BuildingData _wallData;

    // ── Public accessors ──────────────────────────────────────────────────────
    public bool         IsPlacementMode  => _isPlacementMode;
    public Building     SelectedBuilding => _currentlySelected;
    public Villager     SelectedVillager => _selectedVillager;
    public MilitaryUnit SelectedMilitary => _selectedMilitary;
    public IReadOnlyList<MilitaryUnit> SelectedGroup => _selectedGroup;

    // Selection rect in screen space (for UIManager to draw)
    public bool    IsDragging  => _isDragging;
    public Vector2 DragStart   => _dragStart;
    public Vector2 DragEnd     => _dragEnd;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        GridMap.Instance?.Configure(GridSize, new Vector2Int(-96, -96), new Vector2Int(96, 96));
    }

    void Update()
    {
        if (_wallMode)
        {
            HandleWallPlacement();
        }
        else if (_isPlacementMode)
        {
            UpdatePreview();
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                TryPlaceBuilding();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
        }
        else
        {
            HandleDragSelect();
            HandleVillagerCommand();
            HandleGroupCommand();
            HandleMilitaryCommand();
        }
    }

    // ── Selection (single click + drag-rect) ─────────────────────────────────

    void HandleDragSelect()
    {
        if (IsPointerOverUI()) return;
        if (Camera.main == null) return;

        // Mouse button down — start potential drag
        if (Input.GetMouseButtonDown(0))
        {
            _dragStart   = Input.mousePosition;
            _dragEnd     = _dragStart;
            _isDragging  = false;
        }

        // Mouse held — update drag end, start dragging if threshold exceeded
        if (Input.GetMouseButton(0))
        {
            _dragEnd = Input.mousePosition;
            if (!_isDragging && Vector2.Distance(_dragStart, _dragEnd) > DragThreshold)
                _isDragging = true;
        }

        // Mouse button up — finalize
        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {
                FinalizeDragSelect();
                _isDragging = false;
            }
            else
            {
                HandleSingleClick();
            }
        }
    }

    void HandleSingleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            var villager = hit.collider.GetComponentInParent<Villager>();
            if (villager != null) { SelectVillager(villager); return; }

            var military = hit.collider.GetComponentInParent<MilitaryUnit>();
            if (military != null && !military.IsAI) { SelectMilitary(military); return; }

            var building = hit.collider.GetComponentInParent<Building>();
            if (building != null && !building.IsAI) { SelectBuilding(building); return; }
        }
        DeselectAll();
    }

    void FinalizeDragSelect()
    {
        DeselectAll();

        Rect rect = GetScreenRect(_dragStart, _dragEnd);
        foreach (var u in MilitaryUnit.AllUnits)
        {
            if (u == null || !u.IsAlive || u.IsAI) continue;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(u.transform.position);
            if (screenPos.z < 0f) continue; // behind camera
            // Flip Y: Unity screen space has (0,0) bottom-left, IMGUI has (0,0) top-left
            Vector2 sp = new Vector2(screenPos.x, screenPos.y);
            if (rect.Contains(sp))
                AddToGroup(u);
        }

        if (_selectedGroup.Count > 0)
            UIManager.Instance?.ShowGroupInfo(_selectedGroup.Count);
    }

    static Rect GetScreenRect(Vector2 a, Vector2 b) =>
        new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                 Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    void AddToGroup(MilitaryUnit u)
    {
        if (!_selectedGroup.Contains(u))
        {
            _selectedGroup.Add(u);
            u.Select();
        }
    }

    void HandleGroupCommand()
    {
        if (_selectedGroup.Count == 0 || !Input.GetMouseButtonDown(1)) return;
        if (IsPointerOverUI() || Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            var enemyUnit = hit.collider.GetComponentInParent<MilitaryUnit>();
            if (enemyUnit != null && enemyUnit.IsAI)
            {
                foreach (var u in _selectedGroup) u?.CommandAttack(enemyUnit);
                return;
            }
            var enemyBuilding = hit.collider.GetComponentInParent<Building>();
            if (enemyBuilding != null && enemyBuilding.IsAI)
            {
                foreach (var u in _selectedGroup) u?.CommandAttackBuilding(enemyBuilding);
                return;
            }
        }

        // Move group in spread formation
        Plane gnd = new Plane(Vector3.up, Vector3.zero);
        if (gnd.Raycast(ray, out float enter))
        {
            Vector3 center = ray.GetPoint(enter);
            int i = 0;
            foreach (var u in _selectedGroup)
            {
                if (u == null) continue;
                float row = Mathf.Floor(i / 3f);
                float col = i % 3f - 1f;
                u.CommandMoveTo(center + new Vector3(col * 1.5f, 0f, -row * 1.5f));
                i++;
            }
        }
    }

    void HandleVillagerCommand()
    {
        if (_selectedVillager == null || !Input.GetMouseButtonDown(1)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
            _selectedVillager.CommandMoveTo(ray.GetPoint(enter));
    }

    void HandleMilitaryCommand()
    {
        if (_selectedMilitary == null || !Input.GetMouseButtonDown(1)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Right-click on enemy unit → attack
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            var enemyUnit = hit.collider.GetComponentInParent<MilitaryUnit>();
            if (enemyUnit != null && enemyUnit.IsAI)
            {
                _selectedMilitary.CommandAttack(enemyUnit);
                return;
            }

            var enemyBuilding = hit.collider.GetComponentInParent<Building>();
            if (enemyBuilding != null && enemyBuilding.IsAI)
            {
                _selectedMilitary.CommandAttackBuilding(enemyBuilding);
                return;
            }
        }

        // Right-click on ground → move
        Plane gnd = new Plane(Vector3.up, Vector3.zero);
        if (gnd.Raycast(ray, out float enter))
            _selectedMilitary.CommandMoveTo(ray.GetPoint(enter));
    }

    public void SelectVillager(Villager v)
    {
        DeselectAll();
        _selectedVillager = v;
        v.Select();
    }

    public void SelectMilitary(MilitaryUnit u)
    {
        DeselectAll();
        _selectedMilitary = u;
        u.Select();
        UIManager.Instance?.ShowMilitaryInfo(u);
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
        _selectedVillager?.Deselect(); _selectedVillager = null;
        _selectedMilitary?.Deselect(); _selectedMilitary = null;
        _currentlySelected?.Deselect(); _currentlySelected = null;

        foreach (var u in _selectedGroup) u?.Deselect();
        _selectedGroup.Clear();

        UIManager.Instance?.HideBuildingInfo();
        UIManager.Instance?.HideMilitaryInfo();
        UIManager.Instance?.HideGroupInfo();
    }

    // ── Placement ─────────────────────────────────────────────────────────────

    public void StartPlacement(BuildingData data)
    {
        if (data == null) return;

        if (AgeManager.Instance != null && !AgeManager.Instance.CanBuild(data))
        {
            UIManager.Instance?.ShowMessage("Requires " + AgeManager.GetAgeLabel(data.MinAge));
            return;
        }

        if (!ResourceManager.Instance.CanAfford(data.GetCostDict()))
        {
            UIManager.Instance?.ShowMessage("Not enough resources!");
            return;
        }
        DeselectAll();

        if (data.Type == BuildingType.Wall)
        {
            _wallData      = data;
            _wallMode      = true;
            _wallFirstSet  = false;
            UIManager.Instance?.ShowMessage("Click start point, then end point for wall line.", 4f);
            return;
        }

        _selectedBuildingData = data;
        _isPlacementMode = true;
        CreatePreview(data);
    }

    // ── Wall line placement ───────────────────────────────────────────────────

    void HandleWallPlacement()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            _wallMode     = false;
            _wallFirstSet = false;
            _wallData     = null;
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI()) return;
        if (Camera.main == null) return;

        Vector3    worldPos = GetMouseWorldPosition();
        Vector2Int gridPos  = WorldToGrid(worldPos);

        if (!_wallFirstSet)
        {
            _wallStart    = gridPos;
            _wallFirstSet = true;
            UIManager.Instance?.ShowMessage("Now click end point.", 3f);
        }
        else
        {
            PlaceWallLine(_wallStart, gridPos);
            _wallMode     = false;
            _wallFirstSet = false;
            _wallData     = null;
        }
    }

    void PlaceWallLine(Vector2Int from, Vector2Int to)
    {
        // Bresenham line algorithm
        var cells = BresenhamLine(from.x, from.y, to.x, to.y);

        int placed = 0;
        foreach (var cell in cells)
        {
            if (CanPlaceAt(cell, 1, 1))
            {
                if (!ResourceManager.Instance.SpendResources(_wallData.GetCostDict())) break;
                var obj = CreateBuildingMesh(_wallData);
                obj.transform.position = GridToWorld(cell);

                // Rotate segment to face along line
                if (cells.Count > 1)
                {
                    int dx = to.x - from.x, dz = to.y - from.y;
                    if (Mathf.Abs(dz) > Mathf.Abs(dx))
                        obj.transform.rotation = Quaternion.Euler(0, 90, 0);
                }

                var building = obj.AddComponent<Building>();
                building.Data         = _wallData;
                building.GridPosition = cell;
                building.IsAI         = false;

                _occupiedCells.Add(cell);
                _placedBuildings[cell] = building;
                GridMap.Instance?.SetDynamicArea(cell, 1, 1, CellType.Wall);
                placed++;
            }
        }

        if (placed > 0)
            GameManager.Instance?.AddScore(placed * (_wallData.GoldCost + _wallData.WoodCost + _wallData.StoneCost));
    }

    static List<Vector2Int> BresenhamLine(int x0, int y0, int x1, int y1)
    {
        var list = new List<Vector2Int>();
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            list.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
        return list;
    }

    void CreatePreview(BuildingData data)
    {
        if (_previewObject != null) Destroy(_previewObject);
        _previewObject = CreateBuildingMesh(data);
        foreach (var r in _previewObject.GetComponentsInChildren<Renderer>())
            r.material.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        foreach (var c in _previewObject.GetComponentsInChildren<Collider>())
            Destroy(c);
    }

    void UpdatePreview()
    {
        if (_previewObject == null) return;
        Vector3    worldPos = GetMouseWorldPosition();
        Vector2Int gridPos  = WorldToGrid(worldPos);
        _previewObject.transform.position = GridToWorld(gridPos);

        bool canPlace = CanPlaceAt(gridPos, _selectedBuildingData.Width, _selectedBuildingData.Height);
        Color c = canPlace ? new Color(0.2f, 0.9f, 0.2f, 0.5f) : new Color(0.9f, 0.2f, 0.2f, 0.5f);
        foreach (var r in _previewObject.GetComponentsInChildren<Renderer>())
            r.material.color = c;
    }

    void TryPlaceBuilding()
    {
        Vector3    worldPos = GetMouseWorldPosition();
        Vector2Int gridPos  = WorldToGrid(worldPos);

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
        var obj = CreateBuildingMesh(data);
        obj.transform.position = GridToWorld(gridPos);

        var building = obj.AddComponent<Building>();
        building.Data         = data;
        building.GridPosition = gridPos;
        building.IsAI         = false;

        for (int x = 0; x < data.Width; x++)
            for (int z = 0; z < data.Height; z++)
                _occupiedCells.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));

        _placedBuildings[gridPos] = building;

        CellType cellType = data.Type == BuildingType.Wall ? CellType.Wall : CellType.Building;
        GridMap.Instance?.SetDynamicArea(gridPos, data.Width, data.Height, cellType);
    }

    public void CancelPlacement()
    {
        _isPlacementMode      = false;
        _selectedBuildingData = null;
        if (_previewObject != null) { Destroy(_previewObject); _previewObject = null; }
    }

    public void RemoveBuilding(Building building)
    {
        if (building == null || building.Data == null) return;
        for (int x = 0; x < building.Data.Width; x++)
            for (int z = 0; z < building.Data.Height; z++)
                _occupiedCells.Remove(new Vector2Int(building.GridPosition.x + x, building.GridPosition.y + z));
        _placedBuildings.Remove(building.GridPosition);
        GridMap.Instance?.ClearDynamicArea(building.GridPosition, building.Data.Width, building.Data.Height);
    }

    // ── AI building placement (no grid validation, no resource check) ─────────

    public Building SpawnAIBuilding(BuildingData data, Vector3 worldPos)
    {
        var obj = CreateBuildingMesh(data);
        obj.transform.position = worldPos;

        var gridPos = WorldToGrid(worldPos);

        var building = obj.AddComponent<Building>();
        building.Data         = data;
        building.GridPosition = gridPos;
        building.IsAI         = true;

        for (int x = 0; x < data.Width; x++)
            for (int z = 0; z < data.Height; z++)
                _occupiedCells.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));

        _placedBuildings[gridPos] = building;
        GridMap.Instance?.SetDynamicArea(gridPos, data.Width, data.Height, CellType.Building);
        return building;
    }

    public Building SpawnPlayerBuilding(BuildingData data, Vector3 worldPos)
    {
        if (data == null) return null;

        var obj = CreateBuildingMesh(data);
        obj.transform.position = worldPos;

        var gridPos = WorldToGrid(worldPos);

        var building = obj.AddComponent<Building>();
        building.Data = data;
        building.GridPosition = gridPos;
        building.IsAI = false;

        for (int x = 0; x < data.Width; x++)
            for (int z = 0; z < data.Height; z++)
                _occupiedCells.Add(new Vector2Int(gridPos.x + x, gridPos.y + z));

        _placedBuildings[gridPos] = building;
        GridMap.Instance?.SetDynamicArea(gridPos, data.Width, data.Height, CellType.Building);
        return building;
    }

    // ── Mesh creation ─────────────────────────────────────────────────────────

    public GameObject CreateBuildingMesh(BuildingData data)
    {
        var root = new GameObject(data.BuildingName);
        float w = data.Width  * GridSize * 0.9f;
        float h = data.Height * GridSize * 0.9f;

        switch (data.Type)
        {
            case BuildingType.TownCenter:   CreateTownCenter(root, w, h, data.BuildingColor); break;
            case BuildingType.Tower:        CreateTower(root, w, h, data.BuildingColor);      break;
            case BuildingType.Farm:         CreateFarm(root, w, h, data.BuildingColor);       break;
            case BuildingType.Barracks:     CreateBarracks(root, w, h, data.BuildingColor);   break;
            case BuildingType.ArcheryRange: CreateArcheryRange(root, w, h, data.BuildingColor); break;
            case BuildingType.Blacksmith:   CreateBlacksmith(root, w, h, data.BuildingColor); break;
            case BuildingType.University:   CreateUniversity(root, w, h, data.BuildingColor); break;
            case BuildingType.Monastery:    CreateMonastery(root, w, h, data.BuildingColor); break;
            case BuildingType.SiegeWorkshop: CreateSiegeWorkshop(root, w, h, data.BuildingColor); break;
            case BuildingType.Castle:       CreateCastle(root, w, h, data.BuildingColor); break;
            case BuildingType.Stable:       CreateStable(root, w, h, data.BuildingColor); break;
            case BuildingType.LumberCamp:   CreateLumberCamp(root, w, h, data.BuildingColor); break;
            case BuildingType.MiningCamp:   CreateMiningCamp(root, w, h, data.BuildingColor); break;
            case BuildingType.Mill:         CreateMill(root, w, h, data.BuildingColor);       break;
            case BuildingType.Wall:         CreateWallSegment(root, w, h, data.BuildingColor); break;
            default:                        CreateGenericBuilding(root, w, h, data.BuildingColor); break;
        }
        return root;
    }

    void CreateGenericBuilding(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale    = new Vector3(w, 1f, h);
        body.GetComponent<Renderer>().material.color = color;

        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 1.2f, 0);
        roof.transform.localScale    = new Vector3(w * 0.8f, 0.4f, h * 0.8f);
        roof.GetComponent<Renderer>().material.color = color * 0.7f;
        Destroy(roof.GetComponent<Collider>());
    }

    void CreateTownCenter(GameObject root, float w, float h, Color color)
    {
        CreateGenericBuilding(root, w, h, color);
        var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.transform.SetParent(root.transform);
        tower.transform.localPosition = new Vector3(0, 1.5f, 0);
        tower.transform.localScale    = new Vector3(w * 0.3f, 1f, w * 0.3f);
        tower.GetComponent<Renderer>().material.color = color * 1.2f;
        Destroy(tower.GetComponent<Collider>());
    }

    void CreateTower(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.5f, 0);
        body.transform.localScale    = new Vector3(w * 0.7f, 1.5f, w * 0.7f);
        body.GetComponent<Renderer>().material.color = color;

        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 3.2f, 0);
        top.transform.localScale    = new Vector3(w * 0.9f, 0.5f, w * 0.9f);
        top.GetComponent<Renderer>().material.color = color * 0.8f;
        Destroy(top.GetComponent<Collider>());
    }

    void CreateFarm(GameObject root, float w, float h, Color color)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(root.transform);
        ground.transform.localPosition = new Vector3(0, 0.05f, 0);
        ground.transform.localScale    = new Vector3(w, 0.1f, h);
        ground.GetComponent<Renderer>().material.color = new Color(0.6f, 0.8f, 0.3f);

        for (int i = -1; i <= 1; i++)
        {
            var row = GameObject.CreatePrimitive(PrimitiveType.Cube);
            row.transform.SetParent(root.transform);
            row.transform.localPosition = new Vector3(i * (w / 3f), 0.2f, 0);
            row.transform.localScale    = new Vector3(w * 0.15f, 0.3f, h * 0.9f);
            row.GetComponent<Renderer>().material.color = new Color(0.2f, 0.7f, 0.1f);
            Destroy(row.GetComponent<Collider>());
        }
    }

    void CreateBarracks(GameObject root, float w, float h, Color color)
    {
        // Main hall
        var hall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hall.transform.SetParent(root.transform);
        hall.transform.localPosition = new Vector3(0, 0.55f, 0);
        hall.transform.localScale    = new Vector3(w, 1.1f, h);
        hall.GetComponent<Renderer>().material.color = color;

        // Flagpole
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(w * 0.38f, 1.5f, 0);
        pole.transform.localScale    = new Vector3(0.08f, 0.8f, 0.08f);
        pole.GetComponent<Renderer>().material.color = new Color(0.6f, 0.4f, 0.2f);
        Destroy(pole.GetComponent<Collider>());

        // Flag
        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.transform.SetParent(root.transform);
        flag.transform.localPosition = new Vector3(w * 0.38f + 0.18f, 2.1f, 0);
        flag.transform.localScale    = new Vector3(0.35f, 0.22f, 0.06f);
        flag.GetComponent<Renderer>().material.color = new Color(0.85f, 0.15f, 0.15f);
        Destroy(flag.GetComponent<Collider>());

        // Roof parapet (crenellations suggestion)
        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 1.18f, 0);
        roof.transform.localScale    = new Vector3(w * 0.95f, 0.18f, h * 0.95f);
        roof.GetComponent<Renderer>().material.color = color * 0.75f;
        Destroy(roof.GetComponent<Collider>());
    }

    void CreateArcheryRange(GameObject root, float w, float h, Color color)
    {
        // Low open structure
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = new Vector3(0, 0.12f, 0);
        floor.transform.localScale    = new Vector3(w, 0.24f, h);
        floor.GetComponent<Renderer>().material.color = new Color(0.55f, 0.42f, 0.28f);

        // Two side walls
        for (int side = -1; side <= 1; side += 2)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.localPosition = new Vector3(side * w * 0.46f, 0.6f, 0);
            wall.transform.localScale    = new Vector3(0.18f, 1.0f, h * 0.9f);
            wall.GetComponent<Renderer>().material.color = color;
            Destroy(wall.GetComponent<Collider>());
        }

        // Target dummy (sphere on a stick)
        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.transform.SetParent(root.transform);
        stick.transform.localPosition = new Vector3(0, 0.5f, h * 0.3f);
        stick.transform.localScale    = new Vector3(0.07f, 0.45f, 0.07f);
        stick.GetComponent<Renderer>().material.color = new Color(0.6f, 0.35f, 0.15f);
        Destroy(stick.GetComponent<Collider>());

        var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.transform.SetParent(root.transform);
        target.transform.localPosition = new Vector3(0, 1.05f, h * 0.3f);
        target.transform.localScale    = new Vector3(0.38f, 0.38f, 0.15f);
        target.GetComponent<Renderer>().material.color = new Color(0.9f, 0.3f, 0.1f);
        Destroy(target.GetComponent<Collider>());
    }

    void CreateWallSegment(GameObject root, float w, float h, Color color)
    {
        // Tall narrow stone slab
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.transform.SetParent(root.transform);
        slab.transform.localPosition = new Vector3(0, 1.0f, 0);
        slab.transform.localScale    = new Vector3(w, 2.0f, 0.4f);
        slab.GetComponent<Renderer>().material.color = color;

        // Battlement (top crenels, two blocks)
        for (int i = -1; i <= 1; i += 2)
        {
            var crenel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crenel.transform.SetParent(root.transform);
            crenel.transform.localPosition = new Vector3(i * w * 0.3f, 2.2f, 0);
            crenel.transform.localScale    = new Vector3(w * 0.28f, 0.42f, 0.42f);
            crenel.GetComponent<Renderer>().material.color = color * 0.85f;
            Destroy(crenel.GetComponent<Collider>());
        }
    }

    void CreateBlacksmith(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.55f, 0);
        body.transform.localScale = new Vector3(w, 1.1f, h);
        body.GetComponent<Renderer>().material.color = color;

        var anvilBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anvilBase.transform.SetParent(root.transform);
        anvilBase.transform.localPosition = new Vector3(0, 1.2f, 0);
        anvilBase.transform.localScale = new Vector3(0.7f, 0.25f, 0.45f);
        anvilBase.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);
        Destroy(anvilBase.GetComponent<Collider>());

        var anvilTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anvilTop.transform.SetParent(root.transform);
        anvilTop.transform.localPosition = new Vector3(0.1f, 1.35f, 0);
        anvilTop.transform.localScale = new Vector3(0.45f, 0.12f, 0.55f);
        anvilTop.GetComponent<Renderer>().material.color = new Color(0.45f, 0.45f, 0.52f);
        Destroy(anvilTop.GetComponent<Collider>());
    }

    void CreateUniversity(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.75f, 0);
        body.transform.localScale = new Vector3(w, 1.5f, h);
        body.GetComponent<Renderer>().material.color = color;

        var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(root.transform);
        roof.transform.localPosition = new Vector3(0, 1.62f, 0);
        roof.transform.localScale = new Vector3(w * 0.92f, 0.24f, h * 0.92f);
        roof.GetComponent<Renderer>().material.color = color * 0.75f;
        Destroy(roof.GetComponent<Collider>());

        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.transform.SetParent(root.transform);
        dome.transform.localPosition = new Vector3(0, 2.15f, 0);
        dome.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        dome.GetComponent<Renderer>().material.color = new Color(0.75f, 0.75f, 0.82f);
        Destroy(dome.GetComponent<Collider>());
    }

    void CreateMonastery(GameObject root, float w, float h, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.65f, 0);
        body.transform.localScale = new Vector3(w, 1.3f, h);
        body.GetComponent<Renderer>().material.color = color;

        var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.transform.SetParent(root.transform);
        tower.transform.localPosition = new Vector3(0, 1.75f, 0);
        tower.transform.localScale = new Vector3(0.36f, 0.95f, 0.36f);
        tower.GetComponent<Renderer>().material.color = color * 0.9f;
        Destroy(tower.GetComponent<Collider>());

        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 2.65f, 0);
        top.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
        top.GetComponent<Renderer>().material.color = new Color(0.92f, 0.82f, 0.4f);
        Destroy(top.GetComponent<Collider>());
    }

    void CreateSiegeWorkshop(GameObject root, float w, float h, Color color)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = new Vector3(0, 0.3f, 0);
        floor.transform.localScale = new Vector3(w, 0.6f, h);
        floor.GetComponent<Renderer>().material.color = color;

        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.95f, 0);
        frame.transform.localScale = new Vector3(w * 0.85f, 0.35f, h * 0.85f);
        frame.GetComponent<Renderer>().material.color = new Color(0.48f, 0.34f, 0.2f);
        Destroy(frame.GetComponent<Collider>());

        var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.transform.SetParent(root.transform);
        beam.transform.localPosition = new Vector3(0, 1.4f, 0);
        beam.transform.localScale = new Vector3(0.1f, w * 0.35f, 0.1f);
        beam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        beam.GetComponent<Renderer>().material.color = new Color(0.56f, 0.42f, 0.24f);
        Destroy(beam.GetComponent<Collider>());
    }

    void CreateCastle(GameObject root, float w, float h, Color color)
    {
        var keep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        keep.transform.SetParent(root.transform);
        keep.transform.localPosition = new Vector3(0, 1.1f, 0);
        keep.transform.localScale = new Vector3(w * 0.62f, 2.2f, h * 0.62f);
        keep.GetComponent<Renderer>().material.color = color;

        float tx = w * 0.42f;
        float tz = h * 0.42f;
        for (int ix = -1; ix <= 1; ix += 2)
        {
            for (int iz = -1; iz <= 1; iz += 2)
            {
                var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.transform.SetParent(root.transform);
                tower.transform.localPosition = new Vector3(ix * tx, 1.35f, iz * tz);
                tower.transform.localScale = new Vector3(0.45f, 1.35f, 0.45f);
                tower.GetComponent<Renderer>().material.color = color * 0.9f;
                Destroy(tower.GetComponent<Collider>());
            }
        }
    }

    void CreateStable(GameObject root, float w, float h, Color color)
    {
        // Main shed — long low building
        var shed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shed.transform.SetParent(root.transform);
        shed.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        shed.transform.localScale    = new Vector3(w, 1.0f, h);
        shed.GetComponent<Renderer>().material.color = color;

        // Pitched roof (two wedge-shaped slabs)
        for (int side = -1; side <= 1; side += 2)
        {
            var roofSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofSlope.transform.SetParent(root.transform);
            roofSlope.transform.localPosition = new Vector3(side * w * 0.18f, 1.2f, 0f);
            roofSlope.transform.localRotation = Quaternion.Euler(0f, 0f, side * 22f);
            roofSlope.transform.localScale    = new Vector3(w * 0.5f, 0.18f, h * 1.02f);
            roofSlope.GetComponent<Renderer>().material.color = new Color(0.50f, 0.28f, 0.12f);
            Destroy(roofSlope.GetComponent<Collider>());
        }

        // Horse silhouette: two cylinder legs + sphere body
        var horseBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        horseBody.transform.SetParent(root.transform);
        horseBody.transform.localPosition = new Vector3(0f, 0.52f, -h * 0.15f);
        horseBody.transform.localScale    = new Vector3(0.38f, 0.22f, 0.58f);
        horseBody.GetComponent<Renderer>().material.color = new Color(0.32f, 0.22f, 0.12f);
        Destroy(horseBody.GetComponent<Collider>());

        var horseHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        horseHead.transform.SetParent(root.transform);
        horseHead.transform.localPosition = new Vector3(0f, 0.70f, h * 0.14f);
        horseHead.transform.localScale    = new Vector3(0.18f, 0.22f, 0.28f);
        horseHead.GetComponent<Renderer>().material.color = new Color(0.32f, 0.22f, 0.12f);
        Destroy(horseHead.GetComponent<Collider>());

        for (int i = -1; i <= 1; i += 2)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(i * 0.14f, 0.22f, -h * 0.15f);
            leg.transform.localScale    = new Vector3(0.06f, 0.22f, 0.06f);
            leg.GetComponent<Renderer>().material.color = new Color(0.28f, 0.18f, 0.10f);
            Destroy(leg.GetComponent<Collider>());
        }

        // Hay bale
        var hay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hay.transform.SetParent(root.transform);
        hay.transform.localPosition = new Vector3(w * 0.35f, 0.30f, -h * 0.3f);
        hay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hay.transform.localScale    = new Vector3(0.28f, 0.18f, 0.28f);
        hay.GetComponent<Renderer>().material.color = new Color(0.85f, 0.72f, 0.25f);
        Destroy(hay.GetComponent<Collider>());
    }

    void CreateLumberCamp(GameObject root, float w, float h, Color color)
    {
        // Log pile base
        var base_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
        base_.transform.SetParent(root.transform);
        base_.transform.localPosition = new Vector3(0, 0.18f, 0);
        base_.transform.localScale    = new Vector3(w, 0.36f, h);
        base_.GetComponent<Renderer>().material.color = new Color(0.38f, 0.22f, 0.10f);

        // Stacked logs (3 cylinders on their side)
        for (int i = 0; i < 3; i++)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.SetParent(root.transform);
            log.transform.localPosition = new Vector3(-w * 0.15f + i * w * 0.15f, 0.55f + i * 0.18f, 0);
            log.transform.localScale    = new Vector3(0.16f, h * 0.4f, 0.16f);
            log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            log.GetComponent<Renderer>().material.color = color;
            Destroy(log.GetComponent<Collider>());
        }
    }

    void CreateMiningCamp(GameObject root, float w, float h, Color color)
    {
        // Rock platform
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.transform.SetParent(root.transform);
        platform.transform.localPosition = new Vector3(0, 0.2f, 0);
        platform.transform.localScale    = new Vector3(w, 0.4f, h);
        platform.GetComponent<Renderer>().material.color = new Color(0.55f, 0.52f, 0.48f);

        // Ore cart (small box)
        var cart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cart.transform.SetParent(root.transform);
        cart.transform.localPosition = new Vector3(-w * 0.2f, 0.62f, 0);
        cart.transform.localScale    = new Vector3(w * 0.38f, 0.42f, h * 0.5f);
        cart.GetComponent<Renderer>().material.color = new Color(0.40f, 0.25f, 0.12f);
        Destroy(cart.GetComponent<Collider>());

        // Rock chunks (spheres)
        for (int i = 0; i < 2; i++)
        {
            var chunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chunk.transform.SetParent(root.transform);
            chunk.transform.localPosition = new Vector3(w * (0.15f + i * 0.18f), 0.55f, h * 0.2f);
            chunk.transform.localScale    = new Vector3(0.3f, 0.22f, 0.28f);
            chunk.GetComponent<Renderer>().material.color = color;
            Destroy(chunk.GetComponent<Collider>());
        }
    }

    void CreateMill(GameObject root, float w, float h, Color color)
    {
        // Mill house
        var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
        house.transform.SetParent(root.transform);
        house.transform.localPosition = new Vector3(0, 0.5f, 0);
        house.transform.localScale    = new Vector3(w * 0.65f, 1.0f, h);
        house.GetComponent<Renderer>().material.color = color;

        // Windmill pole
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(w * 0.3f, 0.9f, 0);
        pole.transform.localScale    = new Vector3(0.1f, 0.85f, 0.1f);
        pole.GetComponent<Renderer>().material.color = new Color(0.55f, 0.35f, 0.12f);
        Destroy(pole.GetComponent<Collider>());

        // Two windmill blades (crossed cubes)
        for (int i = 0; i < 2; i++)
        {
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(root.transform);
            blade.transform.localPosition = new Vector3(w * 0.3f, 1.8f, 0);
            blade.transform.localScale    = i == 0 ? new Vector3(0.06f, 0.75f, 0.12f)
                                                   : new Vector3(0.75f, 0.06f, 0.12f);
            blade.GetComponent<Renderer>().material.color = new Color(0.9f, 0.88f, 0.78f);
            Destroy(blade.GetComponent<Collider>());
        }
    }

    // ── Grid helpers ──────────────────────────────────────────────────────────

    bool CanPlaceAt(Vector2Int gridPos, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (_occupiedCells.Contains(new Vector2Int(gridPos.x + x, gridPos.y + z)))
                    return false;
        return true;
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        return Vector3.zero;
    }

    public Vector2Int WorldToGrid(Vector3 world) =>
        new Vector2Int(Mathf.RoundToInt(world.x / GridSize), Mathf.RoundToInt(world.z / GridSize));

    Vector3 GridToWorld(Vector2Int grid) =>
        new Vector3(grid.x * GridSize, 0f, grid.y * GridSize);

    bool IsPointerOverUI() =>
        UnityEngine.EventSystems.EventSystem.current != null &&
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

    // ── Queries ───────────────────────────────────────────────────────────────

    public int GetBuildingCount() => _placedBuildings.Count;

    public BuildingData GetBuildingDataByType(BuildingType type)
    {
        foreach (var bd in AvailableBuildings)
            if (bd.Type == type) return bd;
        return null;
    }
}
