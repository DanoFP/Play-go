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
        SetupTerrain();
        SetupCamera();
        SetupLighting();
        SetupUIManager();
    }

    // ── Managers ─────────────────────────────────────────────────────────────

    void SetupManagers()
    {
        var managers = new GameObject("Managers");

        new GameObject("GameManager").transform.SetParent(managers.transform);
        FindOrCreate("GameManager", managers.transform).AddComponent<GameManager>();

        FindOrCreate("ResourceManager", managers.transform).AddComponent<ResourceManager>();

        var bmGO = FindOrCreate("BuildingManager", managers.transform);
        var bm = bmGO.AddComponent<BuildingManager>();
        bm.GroundLayer = LayerMask.GetMask("Default");
        bm.GridSize = 2f;
        bm.AvailableBuildings = CreateDefaultBuildingData();

        FindOrCreate("TerritoryManager", managers.transform).AddComponent<TerritoryManager>();
    }

    GameObject FindOrCreate(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go;
    }

    List<BuildingData> CreateDefaultBuildingData()
    {
        return new List<BuildingData>
        {
            BD(BuildingType.TownCenter, "Town Center", "Heart of your civilization.",
               2, 2, 0, 0, 0, 0,   0,5,0,0,   400, 0, new Color(0.85f,0.75f,0.4f), 5f),
            BD(BuildingType.House, "House", "Shelter for your people.",
               1, 1, 0,50,0,0,     0,0,3,0,   100, 5, new Color(0.7f,0.5f,0.3f), 2f),
            BD(BuildingType.Farm, "Farm", "Produces food to sustain the population.",
               2, 2, 30,80,0,0,    0,0,0,5,   80,  0, new Color(0.4f,0.75f,0.2f), 3f),
            BD(BuildingType.LumberMill, "Lumber Mill", "Processes wood for construction.",
               2, 1, 50,100,0,0,   0,3,0,0,   120, 0, new Color(0.55f,0.35f,0.15f), 3f),
            BD(BuildingType.Quarry, "Quarry", "Extracts stone from the earth.",
               2, 2, 80,60,30,0,   0,0,2,0,   150, 0, new Color(0.65f,0.65f,0.65f), 4f),
            BD(BuildingType.Market, "Market", "Generates gold through trade.",
               2, 2, 100,100,50,0, 4,0,0,0,   200, 0, new Color(0.9f,0.8f,0.1f), 4f),
            BD(BuildingType.Tower, "Watch Tower", "Defends and expands territory.",
               1, 1, 60,80,100,0,  0,0,0,0,   300, 0, new Color(0.5f,0.5f,0.6f), 5f),
            BD(BuildingType.Temple, "Temple", "Cultural center — boosts score and morale.",
               3, 3, 200,150,200,0, 2,0,0,2,  500, 0, new Color(0.9f,0.9f,0.6f), 8f),
        };
    }

    BuildingData BD(BuildingType t, string n, string d,
        int w, int h, int gc, int wc, int sc, int fc,
        int gp, int wp, int sp, int fp, int hp, int pop, Color c, float bt)
    {
        var bd = ScriptableObject.CreateInstance<BuildingData>();
        bd.Type=t; bd.BuildingName=n; bd.Description=d;
        bd.Width=w; bd.Height=h;
        bd.GoldCost=gc; bd.WoodCost=wc; bd.StoneCost=sc; bd.FoodCost=fc;
        bd.GoldProduction=gp; bd.WoodProduction=wp; bd.StoneProduction=sp; bd.FoodProduction=fp;
        bd.MaxHealth=hp; bd.PopulationCapacity=pop; bd.BuildingColor=c; bd.BuildTime=bt;
        return bd;
    }

    // ── Terrain ──────────────────────────────────────────────────────────────

    void SetupTerrain()
    {
        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground"; ground.transform.localScale = new Vector3(25f,1f,25f);
        var gm = new Material(Shader.Find("Standard"));
        gm.color = new Color(0.35f,0.52f,0.28f); gm.SetFloat("_Glossiness",0.05f);
        ground.GetComponent<Renderer>().material = gm;

        CreateWater(new Vector3(35f,-0.1f,30f), new Vector3(25f,0.12f,20f));
        CreateMountainRange();
        PlantForest(new Vector3(20,0,15), 7);
        PlantForest(new Vector3(-25,0,20), 6);
        PlantForest(new Vector3(30,0,-10), 5);
        PlantForest(new Vector3(-35,0,-25), 5);
        ScatterRocks(new Vector3(-20,0,-15), 5);
        ScatterRocks(new Vector3(25,0,25), 4);
        ScatterBushes(22);
    }

    void CreateWater(Vector3 pos, Vector3 scale)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name="Lake"; w.transform.position=pos; w.transform.localScale=scale;
        var m = new Material(Shader.Find("Standard"));
        m.color = new Color(0.2f,0.45f,0.7f,0.8f);
        m.SetFloat("_Mode",3); m.SetFloat("_Glossiness",0.95f);
        m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite",0); m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue=3000;
        w.GetComponent<Renderer>().material=m; Destroy(w.GetComponent<Collider>());
    }

    void CreateMountainRange()
    {
        for (int i=0; i<8; i++)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube); m.name="Mountain";
            float h=Random.Range(12f,30f), w=Random.Range(10f,22f);
            m.transform.position = new Vector3(Random.Range(-70f,70f), h/2f, Random.Range(65f,95f));
            m.transform.localScale = new Vector3(w,h,w*0.8f);
            var mat=new Material(Shader.Find("Standard"));
            mat.color=new Color(0.38f,0.38f,0.42f); mat.SetFloat("_Glossiness",0.02f);
            m.GetComponent<Renderer>().material=mat; Destroy(m.GetComponent<Collider>());
        }
    }

    void PlantForest(Vector3 center, int count)
    {
        for (int i=0; i<count; i++)
            PlantTree(center + new Vector3(Random.Range(-7f,7f),0,Random.Range(-7f,7f)));
    }

    void PlantTree(Vector3 p)
    {
        var t=new GameObject("Tree"); t.transform.position=p;
        float h=Random.Range(2.5f,5f);
        var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(t.transform);
        trunk.transform.localPosition=new Vector3(0,h*0.3f,0);
        trunk.transform.localScale=new Vector3(0.22f,h*0.32f,0.22f);
        trunk.GetComponent<Renderer>().material.color=new Color(0.38f,0.22f,0.08f);
        Destroy(trunk.GetComponent<Collider>());
        for (int i=0; i<2; i++)
        {
            var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.transform.SetParent(t.transform);
            float ly=h*0.6f+i*h*0.22f, ls=h*0.42f-i*h*0.12f;
            leaf.transform.localPosition=new Vector3(Random.Range(-0.1f,0.1f),ly,Random.Range(-0.1f,0.1f));
            leaf.transform.localScale=new Vector3(ls,ls*0.78f,ls);
            leaf.GetComponent<Renderer>().material.color=new Color(0.1f,Random.Range(0.38f,0.68f),0.08f);
            Destroy(leaf.GetComponent<Collider>());
        }
    }

    void ScatterRocks(Vector3 center, int count)
    {
        for (int i=0; i<count; i++)
        {
            var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere); rock.name="Rock";
            float h=Random.Range(0.5f,1.3f), w=Random.Range(0.7f,1.6f);
            rock.transform.position=center+new Vector3(Random.Range(-5f,5f),h*0.4f,Random.Range(-5f,5f));
            rock.transform.localScale=new Vector3(w,h,w*0.88f);
            rock.transform.rotation=Quaternion.Euler(0,Random.Range(0f,360f),0);
            var m=new Material(Shader.Find("Standard"));
            m.color=new Color(0.58f,0.58f,0.61f); m.SetFloat("_Glossiness",0.08f);
            rock.GetComponent<Renderer>().material=m; Destroy(rock.GetComponent<Collider>());
        }
    }

    void ScatterBushes(int count)
    {
        for (int i=0; i<count; i++)
        {
            float x=Random.Range(-45f,45f), z=Random.Range(-45f,45f);
            if (Mathf.Abs(x)<14f && Mathf.Abs(z)<14f) continue;
            var b=GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name="Bush";
            float s=Random.Range(0.3f,0.75f);
            b.transform.position=new Vector3(x,0.28f,z);
            b.transform.localScale=new Vector3(s,s*0.65f,s);
            b.GetComponent<Renderer>().material.color=new Color(0.12f,Random.Range(0.32f,0.58f),0.08f);
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
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.98f);
        var ctrl = cam.gameObject.AddComponent<CameraController>();
        ctrl.MoveSpeed = 22f; ctrl.MinZoom = 18f; ctrl.MaxZoom = 80f;
        ctrl.BoundsMin = new Vector2(-85f,-85f); ctrl.BoundsMax = new Vector2(85f,85f);
    }

    // ── Lighting ─────────────────────────────────────────────────────────────

    void SetupLighting()
    {
        var sun = FindFirstObjectByType<Light>();
        if (sun == null) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
        sun.color = new Color(1f,0.96f,0.86f); sun.intensity = 1.25f;
        sun.transform.rotation = Quaternion.Euler(50f,-30f,0f); sun.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.4f,0.45f,0.5f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f,0.8f,0.9f);
        RenderSettings.fogStartDistance = 85f; RenderSettings.fogEndDistance = 170f;
    }

    // ── UI Manager ────────────────────────────────────────────────────────────

    void SetupUIManager()
    {
        var uiGO = new GameObject("UIManager");
        uiGO.AddComponent<UIManager>();

        // Event System (needed for raycasting buildings)
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
