using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool _buildMenuOpen = false;
    private bool _buildingInfoOpen = false;
    private Building _selectedBuilding;
    private string _message = "";
    private float _messageTimer = 0f;
    private bool _paused = false;
    private Vector2 _buildMenuScroll;

    // ── Cached styles ─────────────────────────────────────────────────────────
    private GUIStyle _labelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _resourceStyle;
    private bool _stylesReady;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ResourceManager.Instance?.OnResourceChanged?.AddListener((t, v) => { });
        GameManager.Instance?.OnScoreChanged?.AddListener((s) => { });
    }

    void Update()
    {
        if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void ShowBuildingInfo(Building b)  { _selectedBuilding = b; _buildingInfoOpen = true; }
    public void HideBuildingInfo()            { _buildingInfoOpen = false; _selectedBuilding = null; }
    public void ShowMessage(string msg, float dur = 2.5f) { _message = msg; _messageTimer = dur; }
    public void ToggleBuildMenu() => _buildMenuOpen = !_buildMenuOpen;

    public void TogglePause()
    {
        _paused = !_paused;
        GameManager.Instance?.TogglePause();
    }

    // ── IMGUI render ─────────────────────────────────────────────────────────
    void OnGUI()
    {
        EnsureStyles();
        float sw = Screen.width, sh = Screen.height;

        DrawTopHUD(sw, sh);
        DrawBottomBar(sw, sh);
        if (_buildMenuOpen)    DrawBuildMenu(sw, sh);
        if (_buildingInfoOpen) DrawBuildingInfo(sw, sh);
        if (_messageTimer > 0) DrawMessage(sw, sh);
        if (_paused)           DrawPauseOverlay(sw, sh);
    }

    // ── TOP HUD ───────────────────────────────────────────────────────────────

    void DrawTopHUD(float sw, float sh)
    {
        DrawRect(new Rect(0,0,sw,66), new Color(0.06f,0.06f,0.1f,0.92f));

        var rm = ResourceManager.Instance;
        if (rm == null) return;

        string[] names  = { "GOLD","WOOD","STONE","FOOD" };
        Color[]  colors = {
            new Color(1f,0.85f,0.2f), new Color(0.5f,0.85f,0.3f),
            new Color(0.8f,0.8f,0.8f), new Color(0.95f,0.65f,0.4f)
        };
        ResourceType[] types = { ResourceType.Gold, ResourceType.Wood, ResourceType.Stone, ResourceType.Food };

        for (int i = 0; i < 4; i++)
        {
            float x = 12 + i * 158;
            DrawRect(new Rect(x,8,148,50), new Color(0.12f,0.12f,0.18f,0.85f));
            DrawRect(new Rect(x,8,5,50), colors[i]);

            _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.55f,0.55f,0.65f);
            GUI.Label(new Rect(x+10, 10, 130, 20), names[i], _labelStyle);
            _resourceStyle.normal.textColor = colors[i];
            GUI.Label(new Rect(x+10, 28, 130, 28), FormatNum(rm.GetAmount(types[i])), _resourceStyle);
        }

        // Title
        _titleStyle.fontSize = 22; _titleStyle.normal.textColor = new Color(0.9f,0.78f,0.35f);
        GUI.Label(new Rect(sw/2f-160, 10, 320, 50), "REALM FORGE", _titleStyle);

        var gm = GameManager.Instance;
        if (gm == null) return;
        // Score
        _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.5f,0.55f,0.7f);
        GUI.Label(new Rect(sw-280, 8, 120, 20), "SCORE", _labelStyle);
        _resourceStyle.normal.textColor = new Color(1f,0.9f,0.3f);
        GUI.Label(new Rect(sw-280, 26, 120, 28), FormatNum(gm.PlayerScore), _resourceStyle);
        // Timer
        _labelStyle.fontSize = 18; _labelStyle.normal.textColor = new Color(0.75f,0.8f,0.85f);
        GUI.Label(new Rect(sw-145, 18, 130, 30), gm.GetFormattedTime(), _labelStyle);
    }

    // ── BOTTOM BAR ────────────────────────────────────────────────────────────

    void DrawBottomBar(float sw, float sh)
    {
        DrawRect(new Rect(0, sh-66, sw, 66), new Color(0.06f,0.06f,0.1f,0.92f));

        GUI.backgroundColor = new Color(0.2f,0.45f,0.75f);
        if (GUI.Button(new Rect(12, sh-58, 130, 50), "  BUILD", _btnStyle)) ToggleBuildMenu();

        GUI.backgroundColor = new Color(0.3f,0.3f,0.35f);
        if (GUI.Button(new Rect(sw-72, sh-58, 60, 50), "||", _btnStyle)) TogglePause();

        GUI.backgroundColor = Color.white;
        _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.4f,0.4f,0.5f);
        _labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(sw/2f-350, sh-58, 700, 50),
            "WASD: Move  |  Scroll: Zoom  |  Q/E: Rotate  |  Left-click: Select  |  ESC: Pause",
            _labelStyle);
        _labelStyle.alignment = TextAnchor.UpperLeft;
    }

    // ── BUILD MENU ────────────────────────────────────────────────────────────

    void DrawBuildMenu(float sw, float sh)
    {
        float mw=264, mh=Mathf.Min(sh-160,530), mx=12, my=sh-mh-78;
        DrawRect(new Rect(mx,my,mw,mh), new Color(0.07f,0.07f,0.12f,0.97f));

        _titleStyle.fontSize=16; _titleStyle.normal.textColor=new Color(0.85f,0.75f,0.4f);
        GUI.Label(new Rect(mx+8,my+6,mw-40,28), "BUILD", _titleStyle);

        GUI.backgroundColor=new Color(0.4f,0.15f,0.15f);
        if (GUI.Button(new Rect(mx+mw-30,my+5,24,24),"X",_btnStyle)) _buildMenuOpen=false;
        GUI.backgroundColor=Color.white;

        var bm = BuildingManager.Instance;
        if (bm==null) return;

        float y=my+38; float bh=64;
        _buildMenuScroll = GUI.BeginScrollView(
            new Rect(mx+2,y,mw-4,mh-(y-my)-4), _buildMenuScroll,
            new Rect(0,0,mw-20, bm.AvailableBuildings.Count*(bh+3)));

        float iy = 0;
        foreach (var bd in bm.AvailableBuildings)
        {
            bool canAfford = ResourceManager.Instance?.CanAfford(bd.GetCostDict()) ?? false;
            DrawRect(new Rect(2,iy,mw-12,bh-2), canAfford ? new Color(0.13f,0.16f,0.2f) : new Color(0.2f,0.1f,0.1f));
            DrawRect(new Rect(2,iy,5,bh-2), bd.BuildingColor);

            _labelStyle.fontSize=14; _labelStyle.fontStyle=FontStyle.Bold;
            _labelStyle.normal.textColor = canAfford ? Color.white : new Color(0.6f,0.4f,0.4f);
            GUI.Label(new Rect(12,iy+4,mw-20,24), bd.BuildingName, _labelStyle);
            _labelStyle.fontStyle=FontStyle.Normal;

            string cost="";
            if (bd.GoldCost>0) cost+=$"G:{bd.GoldCost}  ";
            if (bd.WoodCost>0) cost+=$"W:{bd.WoodCost}  ";
            if (bd.StoneCost>0) cost+=$"S:{bd.StoneCost}";
            _labelStyle.fontSize=11; _labelStyle.normal.textColor=new Color(0.68f,0.68f,0.58f);
            GUI.Label(new Rect(12,iy+30,mw-20,20), cost.Trim(), _labelStyle);

            GUI.color=new Color(1,1,1,0.01f);
            if (GUI.Button(new Rect(2,iy,mw-12,bh-2),GUIContent.none))
            {
                if (canAfford) { bm.StartPlacement(bd); _buildMenuOpen=false; }
                else ShowMessage("Not enough resources!");
            }
            GUI.color=Color.white;

            iy+=bh+3;
        }
        GUI.EndScrollView();
    }

    // ── BUILDING INFO ─────────────────────────────────────────────────────────

    void DrawBuildingInfo(float sw, float sh)
    {
        float pw=262,ph=210,px=sw-pw-12,py=sh-ph-78;
        DrawRect(new Rect(px,py,pw,ph),new Color(0.07f,0.07f,0.12f,0.97f));

        _labelStyle.fontSize=11; _labelStyle.normal.textColor=new Color(0.5f,0.55f,0.75f);
        GUI.Label(new Rect(px+8,py+6,pw-16,20),"SELECTED",_labelStyle);

        GUI.backgroundColor=new Color(0.35f,0.18f,0.18f);
        if (GUI.Button(new Rect(px+pw-30,py+5,24,24),"X",_btnStyle)) BuildingManager.Instance?.DeselectAll();
        GUI.backgroundColor=Color.white;

        if (_selectedBuilding?.Data==null) return;
        var d=_selectedBuilding.Data;

        _titleStyle.fontSize=18; _titleStyle.normal.textColor=new Color(0.9f,0.78f,0.35f);
        GUI.Label(new Rect(px+8,py+26,pw-16,30),d.BuildingName,_titleStyle);

        _labelStyle.fontSize=12; _labelStyle.normal.textColor=new Color(0.75f,0.75f,0.7f);
        GUI.Label(new Rect(px+8,py+58,pw-16,50),d.Description,_labelStyle);

        _labelStyle.fontSize=11; _labelStyle.normal.textColor=new Color(0.55f,0.75f,0.55f);
        GUI.Label(new Rect(px+8,py+112,60,20),"HEALTH",_labelStyle);

        float pct = d.MaxHealth>0 ? _selectedBuilding.CurrentHealth/d.MaxHealth : 1f;
        DrawRect(new Rect(px+8,py+134,pw-16,16),new Color(0.18f,0.18f,0.2f));
        DrawRect(new Rect(px+8,py+134,(pw-16)*pct,16),Color.Lerp(Color.red,new Color(0.25f,0.8f,0.3f),pct));

        _labelStyle.fontSize=11; _labelStyle.normal.textColor=Color.white;
        GUI.Label(new Rect(px+8,py+154,pw-16,20),
            $"{(int)_selectedBuilding.CurrentHealth} / {d.MaxHealth}",_labelStyle);
    }

    // ── MESSAGE ───────────────────────────────────────────────────────────────

    void DrawMessage(float sw, float sh)
    {
        float mw=430,mh=52,mx=sw/2f-mw/2f,my=sh*0.12f;
        DrawRect(new Rect(mx,my,mw,mh),new Color(0.08f,0.08f,0.14f,0.96f));
        _labelStyle.fontSize=16; _labelStyle.normal.textColor=new Color(1f,0.9f,0.4f);
        _labelStyle.alignment=TextAnchor.MiddleCenter;
        GUI.Label(new Rect(mx+8,my,mw-16,mh),_message,_labelStyle);
        _labelStyle.alignment=TextAnchor.UpperLeft;
    }

    // ── PAUSE ─────────────────────────────────────────────────────────────────

    void DrawPauseOverlay(float sw, float sh)
    {
        DrawRect(new Rect(0,0,sw,sh),new Color(0,0,0,0.62f));
        _titleStyle.fontSize=40; _titleStyle.normal.textColor=new Color(0.9f,0.78f,0.35f);
        GUI.Label(new Rect(sw/2f-180,sh/2f-90,360,70),"PAUSED",_titleStyle);
        GUI.backgroundColor=new Color(0.2f,0.45f,0.75f); _btnStyle.fontSize=18;
        if (GUI.Button(new Rect(sw/2f-90,sh/2f,180,54),"RESUME",_btnStyle)) TogglePause();
        _btnStyle.fontSize=14; GUI.backgroundColor=Color.white;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void DrawRect(Rect r, Color c)
    {
        GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white;
    }

    void EnsureStyles()
    {
        if (_stylesReady) return; _stylesReady=true;
        _labelStyle=new GUIStyle(GUI.skin.label){ richText=true };
        _titleStyle=new GUIStyle(GUI.skin.label){ fontStyle=FontStyle.Bold, fontSize=22, alignment=TextAnchor.MiddleCenter };
        _btnStyle=new GUIStyle(GUI.skin.button){ fontStyle=FontStyle.Bold, fontSize=14 };
        _btnStyle.normal.textColor=Color.white; _btnStyle.hover.textColor=Color.white;
        _resourceStyle=new GUIStyle(GUI.skin.label){ fontStyle=FontStyle.Bold, fontSize=22 };
    }

    string FormatNum(int n) => n>=1000 ? $"{n/1000f:0.#}k" : n.ToString();
}
