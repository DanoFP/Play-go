using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bootstraps the entire RealmForge RTS scene at runtime.
/// Attach this MonoBehaviour to any GameObject and hit Play.
/// All UI is rendered via UIManager.OnGUI() (IMGUI) — no Canvas needed.
/// </summary>
public class SceneSetup : MonoBehaviour
{
    void Awake()
    {
        SetupManagers();
        try { SetupTerrain(); } catch (System.Exception e) { Debug.LogError("SetupTerrain: " + e); }
        GridMap.Instance?.RebuildStaticObstacles();
        SetupCamera();
        SetupLighting();
        SetupUIManager();
        SetupResourceNodes();
        SetupStartingBase();
        SetupStartingVillagers();
    }

    // ── Managers ─────────────────────────────────────────────────────────────

    void SetupManagers()
    {
        var managers = new GameObject("Managers");

        FindOrCreate("GameManager",       managers.transform).AddComponent<GameManager>();
        FindOrCreate("ResourceManager",   managers.transform).AddComponent<ResourceManager>();
        FindOrCreate("AgeManager",        managers.transform).AddComponent<AgeManager>();
        FindOrCreate("ResearchManager",   managers.transform).AddComponent<ResearchManager>();
        FindOrCreate("GridMap",           managers.transform).AddComponent<GridMap>();
        FindOrCreate("Pathfinder",        managers.transform).AddComponent<Pathfinder>();

        var bmGO = FindOrCreate("BuildingManager", managers.transform);
        var bm   = bmGO.AddComponent<BuildingManager>();
        bm.GroundLayer        = LayerMask.GetMask("Default");
        bm.GridSize           = 2f;
        bm.AvailableBuildings = CreateDefaultBuildingData();
        GridMap.Instance?.Configure(bm.GridSize, new Vector2Int(-96, -96), new Vector2Int(96, 96));

        FindOrCreate("TerritoryManager", managers.transform).AddComponent<TerritoryManager>();
        FindOrCreate("FogOfWar",         managers.transform).AddComponent<FogOfWar>();
        FindOrCreate("AIController",     managers.transform).AddComponent<AIController>();

        // Register trainable units in BuildingManager
        SetupUnitRegistry(bm);
    }

    void SetupUnitRegistry(BuildingManager bm)
    {
        // Militia — trained at Barracks
        var militia = UnitData.Create(
            name: "Militia",       type: UnitType.Militia,
            trainingBuilding: BuildingType.Barracks,
            goldCost: 20,          foodCost: 60,     woodCost: 0,   popCost: 1,
            trainTime: 21f,
            hp: 40f,               atk: 4f,          range: 1.5f,   speed: 1f,
            dmgType: DamageType.Melee,
            meleeArmor: 0f,        pierceArmor: 0f,  los: 4f,
            color: new Color(0.55f, 0.60f, 0.65f), minAge: 1);

        // Spearman — trained at Barracks
        var spearman = UnitData.Create(
            name: "Spearman",      type: UnitType.Spearman,
            trainingBuilding: BuildingType.Barracks,
            goldCost: 0,           foodCost: 35,     woodCost: 25,  popCost: 1,
            trainTime: 22f,
            hp: 45f,               atk: 3f,          range: 1.5f,   speed: 1f,
            dmgType: DamageType.Melee,
            meleeArmor: 0f,        pierceArmor: 0f,  los: 4f,
            color: new Color(0.45f, 0.55f, 0.80f), minAge: 1);

        // Archer — trained at Archery Range
        var archer = UnitData.Create(
            name: "Archer",        type: UnitType.Archer,
            trainingBuilding: BuildingType.ArcheryRange,
            goldCost: 45,          foodCost: 25,     woodCost: 0,   popCost: 1,
            trainTime: 35f,
            hp: 30f,               atk: 5f,          range: 8f,     speed: 0.9f,
            dmgType: DamageType.Pierce,
            meleeArmor: 0f,        pierceArmor: 0f,  los: 8f,
            color: new Color(0.60f, 0.78f, 0.40f), minAge: 2);

        // Skirmisher — trained at Archery Range
        var skirmisher = UnitData.Create(
            name: "Skirmisher",    type: UnitType.Skirmisher,
            trainingBuilding: BuildingType.ArcheryRange,
            goldCost: 0,           foodCost: 25,     woodCost: 35,  popCost: 1,
            trainTime: 22f,
            hp: 35f,               atk: 3f,          range: 7f,     speed: 1.0f,
            dmgType: DamageType.Pierce,
            meleeArmor: 0f,        pierceArmor: 3f,  los: 7f,
            color: new Color(0.35f, 0.65f, 0.35f), minAge: 2);

        // Scout — trained at Stable (Age 1, cheap, fast explorer)
        var scout = UnitData.Create(
            name: "Scout",         type: UnitType.Scout,
            trainingBuilding: BuildingType.Stable,
            goldCost: 0,           foodCost: 80,     woodCost: 0,   popCost: 1,
            trainTime: 30f,
            hp: 45f,               atk: 3f,          range: 1.5f,   speed: 1.8f,
            dmgType: DamageType.Melee,
            meleeArmor: 0f,        pierceArmor: 2f,  los: 12f,
            color: new Color(0.55f, 0.75f, 0.85f), minAge: 1);

        // Knight — trained at Stable (Age 3, heavy cavalry)
        var knight = UnitData.Create(
            name: "Knight",        type: UnitType.Knight,
            trainingBuilding: BuildingType.Stable,
            goldCost: 60,          foodCost: 75,     woodCost: 0,   popCost: 1,
            trainTime: 30f,
            hp: 100f,              atk: 10f,         range: 1.5f,   speed: 1.6f,
            dmgType: DamageType.Melee,
            meleeArmor: 2f,        pierceArmor: 2f,  los: 8f,
            color: new Color(0.75f, 0.65f, 0.85f), minAge: 3);

        // Monk — trained at Monastery (Age 3, healer)
        var monk = UnitData.Create(
            name: "Monk",          type: UnitType.Monk,
            trainingBuilding: BuildingType.Monastery,
            goldCost: 100,         foodCost: 0,      woodCost: 0,   popCost: 1,
            trainTime: 51f,
            hp: 30f,               atk: 0f,          range: 1.5f,   speed: 0.8f,
            dmgType: DamageType.Melee,
            meleeArmor: 0f,        pierceArmor: 0f,  los: 8f,
            color: new Color(0.84f, 0.82f, 0.64f), minAge: 3);

        // Battering Ram — trained at Siege Workshop (Age 3, anti-building siege)
        var ram = UnitData.Create(
            name: "Battering Ram", type: UnitType.BatteringRam,
            trainingBuilding: BuildingType.SiegeWorkshop,
            goldCost: 0,           foodCost: 160,    woodCost: 100, popCost: 2,
            trainTime: 56f,
            hp: 180f,              atk: 40f,         range: 1.5f,   speed: 0.5f,
            dmgType: DamageType.Siege,
            meleeArmor: 0f,        pierceArmor: 5f,  los: 4f,
            color: new Color(0.45f, 0.30f, 0.14f), minAge: 3);

        // Mangonel — trained at Siege Workshop (Age 3, area ranged)
        var mangonel = UnitData.Create(
            name: "Mangonel",      type: UnitType.Mangonel,
            trainingBuilding: BuildingType.SiegeWorkshop,
            goldCost: 160,         foodCost: 0,      woodCost: 100, popCost: 2,
            trainTime: 46f,
            hp: 120f,              atk: 35f,         range: 10f,    speed: 0.5f,
            dmgType: DamageType.Siege,
            meleeArmor: 0f,        pierceArmor: 6f,  los: 8f,
            color: new Color(0.45f, 0.30f, 0.14f), minAge: 3);

        bm.TrainableUnits[BuildingType.Barracks]       = new List<UnitData> { militia, spearman };
        bm.TrainableUnits[BuildingType.ArcheryRange]   = new List<UnitData> { archer, skirmisher };
        bm.TrainableUnits[BuildingType.Stable]         = new List<UnitData> { scout, knight };
        bm.TrainableUnits[BuildingType.Monastery]      = new List<UnitData> { monk };
        bm.TrainableUnits[BuildingType.SiegeWorkshop]  = new List<UnitData> { ram, mangonel };

        // Give the AI its unit data
        AIController.Instance?.Initialize(militia, archer);
    }

    GameObject FindOrCreate(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go;
    }

    // ── Building data ─────────────────────────────────────────────────────────

    List<BuildingData> CreateDefaultBuildingData()
    {
        return new List<BuildingData>
        {
            BD(BuildingType.TownCenter,   "Town Center",    "Heart of your civilization.\nGrants +5 pop cap.",
               2, 2,   0,   0,   0, 0,   0, 5, 0, 0,   400, 5,  new Color(0.85f,0.75f,0.40f), 5f),
            BD(BuildingType.House,        "House",          "Shelters your people.\nGrants +10 population cap.",
               1, 1,   0,  50,   0, 0,   0, 0, 3, 0,   100, 10, new Color(0.70f,0.50f,0.30f), 2f),
            BD(BuildingType.Farm,         "Farm",           "Produces food to sustain the population.",
               2, 2,  30,  80,   0, 0,   0, 0, 0, 5,    80, 0,  new Color(0.40f,0.75f,0.20f), 3f),
            BD(BuildingType.LumberMill,   "Lumber Mill",    "Processes wood for construction.",
               2, 1,  50, 100,   0, 0,   0, 3, 0, 0,   120, 0,  new Color(0.55f,0.35f,0.15f), 3f),
            BD(BuildingType.Quarry,       "Quarry",         "Extracts stone from the earth.",
               2, 2,  80,  60,  30, 0,   0, 0, 2, 0,   150, 0,  new Color(0.65f,0.65f,0.65f), 4f),
            BD(BuildingType.Market,       "Market",         "Generates gold through trade.",
               2, 2, 100, 100,  50, 0,   4, 0, 0, 0,   200, 0,  new Color(0.90f,0.80f,0.10f), 4f),
            BD(BuildingType.Barracks,     "Barracks",       "Trains infantry. Militia and Spearmen.",
               2, 2,   0, 175,   0, 0,   0, 0, 0, 0,   800, 0,  new Color(0.55f,0.40f,0.32f), 5f),
            BD(BuildingType.ArcheryRange, "Archery Range",  "Trains ranged units. Archers and Skirmishers.",
               2, 2,   0, 175,   0, 0,   0, 0, 0, 0,   600, 0,  new Color(0.40f,0.55f,0.32f), 5f),
                BD(BuildingType.Blacksmith,   "Blacksmith",     "Researches military upgrades.",
                    2, 2,   0, 150,   0, 0,   0, 0, 0, 0,   650, 0,  new Color(0.46f,0.46f,0.50f), 5f),
                BD(BuildingType.University,   "University",     "Researches defensive and engineering technologies.",
                    2, 2,   0, 200, 200, 0,   0, 0, 0, 0,   700, 0,  new Color(0.56f,0.56f,0.62f), 6f),
                BD(BuildingType.Monastery,    "Monastery",      "Spiritual center. Enables monk tech path.",
                    2, 2,   0, 175,   0, 0,   0, 0, 0, 0,   650, 0,  new Color(0.84f,0.82f,0.64f), 6f),
                BD(BuildingType.SiegeWorkshop,"Siege Workshop", "Produces siege machinery.",
                    2, 2,   0, 200,   0, 0,   0, 0, 0, 0,   750, 0,  new Color(0.44f,0.36f,0.28f), 6f),
                BD(BuildingType.Castle,       "Castle",         "Fortified stronghold for elite military production.",
                    3, 3,   0,   0, 650, 0,   0, 0, 0, 0,  1400, 0,  new Color(0.58f,0.58f,0.66f), 8f),
                BD(BuildingType.Stable,       "Stable",         "Trains cavalry. Scout (Age 1) and Knight (Age 3).",
                    2, 2,   0, 175,  50, 0,   0, 0, 0, 0,   600, 0,  new Color(0.55f,0.40f,0.20f), 5f),
            BD(BuildingType.Tower,        "Watch Tower",    "Defends territory. Auto-attacks nearby enemies.",
               1, 1,  60,  80, 100, 0,   0, 0, 0, 0,   300, 0,  new Color(0.50f,0.50f,0.60f), 5f),
            BD(BuildingType.Wall,         "Wall",           "Click two points to draw a wall line.",
                    1, 1,   0,   0,  30, 0,   0, 0, 0, 0,  1500, 0,  new Color(0.58f,0.58f,0.65f), 1f),
            BD(BuildingType.Temple,       "Temple",         "Cultural center — boosts score and morale.",
               3, 3, 200, 150, 200, 0,   2, 0, 0, 2,   500, 0,  new Color(0.90f,0.90f,0.60f), 8f),
            BD(BuildingType.LumberCamp,   "Lumber Camp",    "Wood deposit point. Villagers return wood here.",
               2, 1,   0, 100,   0, 0,   0, 0, 0, 0,   150, 0,  new Color(0.50f,0.32f,0.14f), 2f),
            BD(BuildingType.MiningCamp,   "Mining Camp",    "Stone & gold deposit point.",
               2, 2,   0, 100,   0, 0,   0, 0, 0, 0,   180, 0,  new Color(0.58f,0.55f,0.50f), 2f),
            BD(BuildingType.Mill,         "Mill",           "Food deposit point. Villagers return food here.",
               2, 1,   0, 100,   0, 0,   0, 0, 0, 2,   150, 0,  new Color(0.72f,0.68f,0.28f), 2f),
        };
    }

    BuildingData BD(BuildingType t, string n, string d,
        int w, int h, int gc, int wc, int sc, int fc,
        int gp, int wp, int sp, int fp, int hp, int pop, Color c, float bt)
    {
        var bd = ScriptableObject.CreateInstance<BuildingData>();
        bd.Type = t; bd.BuildingName = n; bd.Description = d;
        bd.MinAge = t == BuildingType.ArcheryRange || t == BuildingType.Blacksmith || t == BuildingType.Tower
                    || t == BuildingType.Wall || t == BuildingType.Stable ? 2 :
                t == BuildingType.University || t == BuildingType.Monastery || t == BuildingType.SiegeWorkshop
                    || t == BuildingType.Castle ? 3 : 1;
        bd.Width = w; bd.Height = h;
        bd.GoldCost = gc; bd.WoodCost = wc; bd.StoneCost = sc; bd.FoodCost = fc;
        bd.GoldProduction = gp; bd.WoodProduction = wp; bd.StoneProduction = sp; bd.FoodProduction = fp;
        bd.MaxHealth = hp; bd.PopulationCapacity = pop; bd.BuildingColor = c; bd.BuildTime = bt;
        return bd;
    }

    // ── Terrain ──────────────────────────────────────────────────────────────

    static Material Mat(Renderer r, Color color, float glossiness = 0.1f)
    {
        var m = r.material;
        m.color = color;
        m.SetFloat("_Glossiness", glossiness);
        return m;
    }

    void SetupTerrain()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground"; ground.transform.localScale = new Vector3(25f, 1f, 25f);
        Mat(ground.GetComponent<Renderer>(), new Color(0.35f, 0.52f, 0.28f), 0.05f);

        CreateWater(new Vector3(35f, -0.1f, 30f), new Vector3(25f, 0.12f, 20f));
        CreateMountainRange();
        PlantForest(new Vector3( 20, 0,  15), 7);
        PlantForest(new Vector3(-25, 0,  20), 6);
        PlantForest(new Vector3( 30, 0, -10), 5);
        PlantForest(new Vector3(-35, 0, -25), 5);
        ScatterRocks(new Vector3(-20, 0, -15), 5);
        ScatterRocks(new Vector3( 25, 0,  25), 4);
        ScatterBushes(22);
    }

    void CreateWater(Vector3 pos, Vector3 scale)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "Lake"; w.transform.position = pos; w.transform.localScale = scale;
        var m = w.GetComponent<Renderer>().material;
        m.color = new Color(0.2f, 0.45f, 0.7f, 0.8f);
        m.SetFloat("_Mode", 3); m.SetFloat("_Glossiness", 0.95f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0); m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000;
        Destroy(w.GetComponent<Collider>());
    }

    void CreateMountainRange()
    {
        for (int i = 0; i < 8; i++)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube); m.name = "Mountain";
            float mh = Random.Range(12f, 30f), mw = Random.Range(10f, 22f);
            m.transform.position   = new Vector3(Random.Range(-70f, 70f), mh / 2f, Random.Range(65f, 95f));
            m.transform.localScale = new Vector3(mw, mh, mw * 0.8f);
            Mat(m.GetComponent<Renderer>(), new Color(0.38f, 0.38f, 0.42f), 0.02f);
            Destroy(m.GetComponent<Collider>());
        }
    }

    void PlantForest(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
            PlantTree(center + new Vector3(Random.Range(-7f, 7f), 0, Random.Range(-7f, 7f)));
    }

    void PlantTree(Vector3 p)
    {
        var t = new GameObject("Tree"); t.transform.position = p;
        float h = Random.Range(2.5f, 5f);
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(t.transform);
        trunk.transform.localPosition = new Vector3(0, h * 0.3f, 0);
        trunk.transform.localScale    = new Vector3(0.22f, h * 0.32f, 0.22f);
        trunk.GetComponent<Renderer>().material.color = new Color(0.38f, 0.22f, 0.08f);
        Destroy(trunk.GetComponent<Collider>());
        for (int i = 0; i < 2; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.transform.SetParent(t.transform);
            float ly = h * 0.6f + i * h * 0.22f, ls = h * 0.42f - i * h * 0.12f;
            leaf.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), ly, Random.Range(-0.1f, 0.1f));
            leaf.transform.localScale    = new Vector3(ls, ls * 0.78f, ls);
            leaf.GetComponent<Renderer>().material.color = new Color(0.1f, Random.Range(0.38f, 0.68f), 0.08f);
            Destroy(leaf.GetComponent<Collider>());
        }
    }

    void ScatterRocks(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere); rock.name = "Rock";
            float rh = Random.Range(0.5f, 1.3f), rw = Random.Range(0.7f, 1.6f);
            rock.transform.position   = center + new Vector3(Random.Range(-5f, 5f), rh * 0.4f, Random.Range(-5f, 5f));
            rock.transform.localScale = new Vector3(rw, rh, rw * 0.88f);
            rock.transform.rotation   = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Mat(rock.GetComponent<Renderer>(), new Color(0.58f, 0.58f, 0.61f), 0.08f);
            Destroy(rock.GetComponent<Collider>());
        }
    }

    void ScatterBushes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-45f, 45f), z = Random.Range(-45f, 45f);
            if (Mathf.Abs(x) < 14f && Mathf.Abs(z) < 14f) continue;
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Bush";
            float s = Random.Range(0.3f, 0.75f);
            b.transform.position   = new Vector3(x, 0.28f, z);
            b.transform.localScale = new Vector3(s, s * 0.65f, s);
            b.GetComponent<Renderer>().material.color = new Color(0.12f, Random.Range(0.32f, 0.58f), 0.08f);
            Destroy(b.GetComponent<Collider>());
        }
    }

    // ── Camera ───────────────────────────────────────────────────────────────

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0f, 30f, -22f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        cam.backgroundColor    = new Color(0.53f, 0.81f, 0.98f);
        var ctrl = cam.gameObject.AddComponent<CameraController>();
        ctrl.MoveSpeed = 22f; ctrl.MinZoom = 18f; ctrl.MaxZoom = 80f;
        ctrl.BoundsMin = new Vector2(-85f, -85f); ctrl.BoundsMax = new Vector2(85f, 85f);
    }

    // ── Lighting ─────────────────────────────────────────────────────────────

    void SetupLighting()
    {
        var sun = FindAnyObjectByType<Light>();
        if (sun == null) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
        sun.color     = new Color(1f, 0.96f, 0.86f); sun.intensity = 1.25f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f); sun.shadows = LightShadows.Soft;
        RenderSettings.ambientLight      = new Color(0.4f, 0.45f, 0.5f);
        RenderSettings.fog               = true;
        RenderSettings.fogMode           = FogMode.Linear;
        RenderSettings.fogColor          = new Color(0.7f, 0.8f, 0.9f);
        RenderSettings.fogStartDistance  = 85f;
        RenderSettings.fogEndDistance    = 170f;
    }

    // ── Resource Nodes ────────────────────────────────────────────────────────

    void SetupResourceNodes()
    {
        SpawnNodeCluster(ResourceNode.NodeType.Wood,  new Vector3( 20, 0,  15), 4, 120);
        SpawnNodeCluster(ResourceNode.NodeType.Wood,  new Vector3(-25, 0,  20), 3, 100);
        SpawnNodeCluster(ResourceNode.NodeType.Wood,  new Vector3( 30, 0, -10), 3, 100);
        SpawnNodeCluster(ResourceNode.NodeType.Wood,  new Vector3(-35, 0, -25), 3,  80);
        SpawnNodeCluster(ResourceNode.NodeType.Stone, new Vector3(-20, 0, -15), 3, 160);
        SpawnNodeCluster(ResourceNode.NodeType.Stone, new Vector3( 25, 0,  25), 3, 160);
        SpawnNodeCluster(ResourceNode.NodeType.Gold,  new Vector3( 14, 0, -22), 2, 220);
        SpawnNodeCluster(ResourceNode.NodeType.Gold,  new Vector3(-12, 0,  32), 2, 220);
    }

    void SpawnNodeCluster(ResourceNode.NodeType type, Vector3 center, int count, int amountEach)
    {
        for (int i = 0; i < count; i++)
            ResourceNode.SpawnAt(center + new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f)), type, amountEach);
    }

    // ── Starting Villagers ────────────────────────────────────────────────────

    void SetupStartingBase()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;

        var tcData = bm.GetBuildingDataByType(BuildingType.TownCenter);
        if (tcData == null) return;

        var tc = bm.SpawnPlayerBuilding(tcData, Vector3.zero);
        if (tc == null) return;

        tc.CompleteInstantlyForBootstrap();
        FogOfWar.Instance?.Reveal(Vector3.zero, 24f);
    }

    void SetupStartingVillagers()
    {
        Villager.SpawnAt(new Vector3( 4, 0,  4));
        Villager.SpawnAt(new Vector3(-4, 0,  4));
        Villager.SpawnAt(new Vector3( 0, 0, -5));
        FogOfWar.Instance?.Reveal(new Vector3(0f, 0f, 2f), 18f);
    }

    // ── UI Manager ────────────────────────────────────────────────────────────

    void SetupUIManager()
    {
        var uiGO = new GameObject("UIManager");
        uiGO.AddComponent<UIManager>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
