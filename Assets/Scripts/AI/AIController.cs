using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy AI controller.
/// Manages its own resource pool, follows a scripted build order,
/// trains military units from its buildings, and launches attack waves.
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    // ── AI base position (far corner, visible on minimap) ─────────────────────
    public static readonly Vector3 BaseCenter = new Vector3(42f, 0f, 42f);

    // ── AI resources (separate from player's ResourceManager) ─────────────────
    int _gold  = 200;
    int _wood  = 300;
    int _stone = 100;
    int _food  = 300;

    // ── Buildings and units ───────────────────────────────────────────────────
    readonly List<Building>     _buildings = new List<Building>();
    readonly List<MilitaryUnit> _units     = new List<MilitaryUnit>();

    // ── Unit data ─────────────────────────────────────────────────────────────
    UnitData _militiaData;
    UnitData _archerData;

    // ── Production ────────────────────────────────────────────────────────────
    float _prodTimer;
    const float ProdInterval = 1f;
    const int   ProdGold     = 2;
    const int   ProdWood     = 2;
    const int   ProdStone    = 1;
    const int   ProdFood     = 4;

    // ── Build order: (delay in seconds, type, offset from BaseCenter) ─────────
    struct BuildTask { public float delay; public BuildingType type; public Vector3 offset; }

    static readonly BuildTask[] BuildOrder =
    {
        new BuildTask { delay =  0f,  type = BuildingType.TownCenter,   offset = new Vector3( 0f, 0f,  0f) },
        new BuildTask { delay =  5f,  type = BuildingType.House,        offset = new Vector3(-5f, 0f,  0f) },
        new BuildTask { delay = 20f,  type = BuildingType.House,        offset = new Vector3( 5f, 0f,  0f) },
        new BuildTask { delay = 30f,  type = BuildingType.Barracks,     offset = new Vector3( 0f, 0f, -7f) },
        new BuildTask { delay = 70f,  type = BuildingType.House,        offset = new Vector3(-5f, 0f,  5f) },
        new BuildTask { delay = 100f, type = BuildingType.ArcheryRange, offset = new Vector3( 7f, 0f, -5f) },
        new BuildTask { delay = 160f, type = BuildingType.House,        offset = new Vector3( 5f, 0f,  5f) },
        new BuildTask { delay = 200f, type = BuildingType.Barracks,     offset = new Vector3(-7f, 0f, -5f) },
    };

    int   _buildIndex;
    float _buildTimer;

    // ── Training ──────────────────────────────────────────────────────────────
    float _trainTimer;
    const float TrainInterval = 5f; // check every 5s if we should enqueue a unit

    // ── Attack waves ──────────────────────────────────────────────────────────
    float _attackTimer = 240f; // first attack at 4 min
    int   _waveNumber;

    // Idle units rally here between waves
    Vector3 _rallyPoint;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _rallyPoint = BaseCenter + new Vector3(0f, 0f, -10f);
    }

    public void Initialize(UnitData militia, UnitData archer)
    {
        _militiaData = militia;
        _archerData  = archer;
    }

    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Passive resource production
        _prodTimer += Time.deltaTime;
        if (_prodTimer >= ProdInterval)
        {
            _prodTimer -= ProdInterval;
            _gold  = Mathf.Min(_gold  + ProdGold,  9999);
            _wood  = Mathf.Min(_wood  + ProdWood,  9999);
            _stone = Mathf.Min(_stone + ProdStone, 9999);
            _food  = Mathf.Min(_food  + ProdFood,  9999);
        }

        // Build order
        _buildTimer += Time.deltaTime;
        ExecuteBuildOrder();

        // Training
        _trainTimer += Time.deltaTime;
        if (_trainTimer >= TrainInterval) { _trainTimer = 0f; TryEnqueueUnits(); }

        // Prune dead units
        _units.RemoveAll(u => u == null || !u.IsAlive);

        // Attack waves
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f) LaunchWave();
    }

    // ── Build order ───────────────────────────────────────────────────────────

    void ExecuteBuildOrder()
    {
        if (_buildIndex >= BuildOrder.Length) return;

        var task = BuildOrder[_buildIndex];
        if (_buildTimer < task.delay) return;

        Vector3 pos = BaseCenter + task.offset;
        var bm = BuildingManager.Instance;
        if (bm == null) { _buildIndex++; return; }

        var data = bm.GetBuildingDataByType(task.type);
        if (data == null) { _buildIndex++; return; }

        if (!CanAfford(data.GoldCost, data.WoodCost, data.StoneCost, data.FoodCost))
            return; // wait until we have resources

        Spend(data.GoldCost, data.WoodCost, data.StoneCost, data.FoodCost);
        var building = bm.SpawnAIBuilding(data, pos);
        if (building != null) _buildings.Add(building);
        _buildIndex++;
    }

    // ── Training ──────────────────────────────────────────────────────────────

    void TryEnqueueUnits()
    {
        if (_militiaData == null && _archerData == null) return;

        foreach (var b in _buildings)
        {
            if (b == null || !b.IsBuilt || b.IsTrainingQueueFull) continue;

            if (b.Data.Type == BuildingType.Barracks && _militiaData != null)
            {
                if (CanAfford(_militiaData.GoldCost, _militiaData.WoodCost, 0, _militiaData.FoodCost))
                {
                    Spend(_militiaData.GoldCost, _militiaData.WoodCost, 0, _militiaData.FoodCost);
                    b.EnqueueUnit(_militiaData);
                }
            }
            else if (b.Data.Type == BuildingType.ArcheryRange && _archerData != null)
            {
                if (CanAfford(_archerData.GoldCost, _archerData.WoodCost, 0, _archerData.FoodCost))
                {
                    Spend(_archerData.GoldCost, _archerData.WoodCost, 0, _archerData.FoodCost);
                    b.EnqueueUnit(_archerData);
                }
            }
        }
    }

    // Called by Building when an AI unit finishes training
    public void RegisterUnit(MilitaryUnit unit)
    {
        if (unit != null && !_units.Contains(unit))
            _units.Add(unit);
    }

    // Called when a Monk converts an AI unit to the player side
    public void UnregisterUnit(MilitaryUnit unit)
    {
        _units.Remove(unit);
    }

    // ── Attack waves ──────────────────────────────────────────────────────────

    void LaunchWave()
    {
        _waveNumber++;
        int waveSize = Mathf.Min(3 + _waveNumber * 2, 12);
        _attackTimer = 150f; // next wave in 2.5 min

        // Find idle units for the wave
        int sent = 0;
        foreach (var u in _units)
        {
            if (u == null || !u.IsAlive) continue;
            if (sent >= waveSize) break;
            u.CommandMoveTo(Vector3.zero); // attack player's center
            sent++;
        }

        if (sent > 0)
            Debug.Log($"[AI] Wave {_waveNumber}: sent {sent} units toward player base.");
    }

    // ── Resource helpers ──────────────────────────────────────────────────────

    bool CanAfford(int gold, int wood, int stone, int food) =>
        _gold >= gold && _wood >= wood && _stone >= stone && _food >= food;

    void Spend(int gold, int wood, int stone, int food)
    {
        _gold  -= gold;
        _wood  -= wood;
        _stone -= stone;
        _food  -= food;
    }

    // ── Queries used by GameManager ───────────────────────────────────────────

    public bool HasBuildings()
    {
        _buildings.RemoveAll(b => b == null || !b.gameObject.activeSelf);
        return _buildings.Count > 0;
    }

    public bool HasUnits()
    {
        _units.RemoveAll(u => u == null || !u.IsAlive);
        return _units.Count > 0;
    }

    // ── Minimap access ────────────────────────────────────────────────────────
    public IReadOnlyList<MilitaryUnit> Units => _units;
}
