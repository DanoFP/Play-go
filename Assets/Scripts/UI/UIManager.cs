using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Runtime state ─────────────────────────────────────────────────────────
    bool      _buildMenuOpen;
    bool      _buildingInfoOpen;
    bool      _militaryInfoOpen;
    bool      _groupInfoOpen;
    int       _groupCount;
    Building  _selectedBuilding;
    MilitaryUnit _selectedMilitary;
    string    _message     = "";
    float     _messageTimer;
    bool      _paused;
    Vector2   _buildMenuScroll;

    // Race selection
    RaceData[] _races;
    int        _hoveredRace  = -1;
    int        _selectedRace = 0;

    // Minimap cache
    ResourceNode[] _minimapNodes     = new ResourceNode[0];
    Building[]     _minimapBuildings = new Building[0];
    Villager[]     _minimapVillagers = new Villager[0];
    float          _minimapCacheTimer;

    // ── Market trading ────────────────────────────────────────────────────────
    // Index 0=Food, 1=Wood, 2=Stone — "price in gold per 100 units"
    readonly float[] _marketRates = { 100f, 100f, 100f };
    float _marketNormTimer;
    const float MarketNormInterval = 30f;
    const int   MarketTradeAmt     = 100;
    static readonly ResourceType[] _marketResTypes = { ResourceType.Food, ResourceType.Wood, ResourceType.Stone };
    static readonly string[]       _marketResNames = { "Food", "Wood", "Stone" };

    // ── IMGUI styles ──────────────────────────────────────────────────────────
    GUIStyle _labelStyle;
    GUIStyle _titleStyle;
    GUIStyle _btnStyle;
    GUIStyle _resourceStyle;
    bool     _stylesReady;

    // ── Colours ───────────────────────────────────────────────────────────────
    static readonly Color[] ResColors  = { new Color(1f,0.85f,0.2f), new Color(0.5f,0.85f,0.3f), new Color(0.8f,0.8f,0.8f), new Color(0.95f,0.65f,0.4f) };
    static readonly string[] ResNames  = { "GOLD", "WOOD", "STONE", "FOOD" };
    static readonly ResourceType[] ResTypes = { ResourceType.Gold, ResourceType.Wood, ResourceType.Stone, ResourceType.Food };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _races = RaceData.All();
    }

    void Start()
    {
        Time.timeScale = 0f; // freeze until race is chosen
    }

    void Update()
    {
        if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;

        _minimapCacheTimer -= Time.unscaledDeltaTime;
        if (_minimapCacheTimer <= 0f)
        {
            _minimapCacheTimer = 0.5f;
            _minimapNodes     = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            _minimapBuildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            _minimapVillagers = Object.FindObjectsByType<Villager>(FindObjectsSortMode.None);
        }

        // Market: slowly normalize prices back toward 100 gold / 100 resources
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
        {
            _marketNormTimer -= Time.deltaTime;
            if (_marketNormTimer <= 0f)
            {
                _marketNormTimer = MarketNormInterval;
                for (int i = 0; i < _marketRates.Length; i++)
                    _marketRates[i] = Mathf.MoveTowards(_marketRates[i], 100f, 1f);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowBuildingInfo(Building b)  { _selectedBuilding = b; _buildingInfoOpen = true; _militaryInfoOpen = false; _groupInfoOpen = false; }
    public void HideBuildingInfo()            { _buildingInfoOpen = false; _selectedBuilding = null; }
    public void ShowMilitaryInfo(MilitaryUnit u) { _selectedMilitary = u; _militaryInfoOpen = true; _buildingInfoOpen = false; _groupInfoOpen = false; }
    public void HideMilitaryInfo()            { _militaryInfoOpen = false; _selectedMilitary = null; }
    public void ShowGroupInfo(int count)      { _groupCount = count; _groupInfoOpen = true; _militaryInfoOpen = false; _buildingInfoOpen = false; }
    public void HideGroupInfo()               { _groupInfoOpen = false; }
    public void ShowMessage(string msg, float dur = 2.5f) { _message = msg; _messageTimer = dur; }
    public void ToggleBuildMenu() => _buildMenuOpen = !_buildMenuOpen;

    public void TogglePause()
    {
        _paused = !_paused;
        GameManager.Instance?.TogglePause();
    }

    // ── Main render ───────────────────────────────────────────────────────────

    void OnGUI()
    {
        EnsureStyles();
        float sw = Screen.width, sh = Screen.height;

        var state = GameManager.Instance?.CurrentState ?? GameManager.GameState.RaceSelection;

        if (state == GameManager.GameState.RaceSelection)
        {
            DrawRaceSelection(sw, sh);
            return;
        }

        if (state == GameManager.GameState.Victory || state == GameManager.GameState.Defeat)
        {
            DrawGameOverlay(sw, sh, state == GameManager.GameState.Victory);
            return;
        }

        DrawTopHUD(sw, sh);
        DrawBottomBar(sw, sh);
        DrawMinimap(sw, sh);
        if (_buildMenuOpen)    DrawBuildMenu(sw, sh);
        if (_buildingInfoOpen) DrawBuildingInfo(sw, sh);
        if (_militaryInfoOpen) DrawMilitaryInfo(sw, sh);
        if (_groupInfoOpen)    DrawGroupInfo(sw, sh);
        if (_messageTimer > 0) DrawMessage(sw, sh);
        if (_paused)           DrawPauseOverlay(sw, sh);
        DrawDragSelectionRect();
    }

    // ── RACE SELECTION ────────────────────────────────────────────────────────

    void DrawRaceSelection(float sw, float sh)
    {
        DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.04f, 0.10f, 0.97f));

        _titleStyle.fontSize = 32;
        _titleStyle.normal.textColor = new Color(0.90f, 0.78f, 0.35f);
        GUI.Label(new Rect(0, 28, sw, 50), "CHOOSE YOUR RACE", _titleStyle);

        _labelStyle.fontSize = 13;
        _labelStyle.normal.textColor = new Color(0.55f, 0.55f, 0.65f);
        _labelStyle.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(0, 72, sw, 24), "Your choice shapes starting resources, passive production, and playstyle.", _labelStyle);
        _labelStyle.alignment = TextAnchor.UpperLeft;

        int   n     = _races.Length;
        float pad   = 20f;
        float cardW = (sw - pad * (n + 1)) / n;
        float cardH = sh * 0.62f;
        float cardY = 108f;

        _hoveredRace = -1;
        Vector2 mouse = Event.current.mousePosition;

        for (int i = 0; i < n; i++)
        {
            float cardX = pad + i * (cardW + pad);
            var   r     = _races[i];
            bool  hov   = mouse.x >= cardX && mouse.x <= cardX + cardW && mouse.y >= cardY && mouse.y <= cardY + cardH;
            bool  sel   = _selectedRace == i;
            if (hov) _hoveredRace = i;

            Color bg = sel ? new Color(0.14f,0.17f,0.24f) : hov ? new Color(0.11f,0.13f,0.18f) : new Color(0.08f,0.08f,0.13f);
            DrawRect(new Rect(cardX, cardY, cardW, cardH), bg);

            float barH = sel ? 8f : hov ? 6f : 4f;
            DrawRect(new Rect(cardX, cardY, cardW, barH), r.PrimaryColor);
            DrawRect(new Rect(cardX, cardY + barH, 4f, cardH - barH), new Color(r.PrimaryColor.r, r.PrimaryColor.g, r.PrimaryColor.b, 0.5f));

            _titleStyle.fontSize = 20;
            _titleStyle.normal.textColor = sel ? r.AccentColor : hov ? r.PrimaryColor : new Color(0.85f,0.85f,0.85f);
            GUI.Label(new Rect(cardX + 10, cardY + barH + 10, cardW - 20, 32), r.Name.ToUpper(), _titleStyle);

            _labelStyle.fontSize = 11;
            _labelStyle.normal.textColor = new Color(r.PrimaryColor.r*0.85f, r.PrimaryColor.g*0.85f, r.PrimaryColor.b*0.85f);
            GUI.Label(new Rect(cardX + 12, cardY + barH + 44, cardW - 20, 20), r.Title, _labelStyle);
            DrawRect(new Rect(cardX + 10, cardY + barH + 66, cardW - 20, 1f), new Color(r.PrimaryColor.r, r.PrimaryColor.g, r.PrimaryColor.b, 0.3f));

            var descStyle = new GUIStyle(_labelStyle) { fontSize = 12, wordWrap = true };
            descStyle.normal.textColor = new Color(0.72f,0.72f,0.72f);
            GUI.Label(new Rect(cardX + 12, cardY + barH + 74, cardW - 24, 80), r.Description, descStyle);

            _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.45f,0.50f,0.60f);
            GUI.Label(new Rect(cardX + 12, cardY + barH + 162, cardW - 24, 18), "BONUSES", _labelStyle);
            DrawRect(new Rect(cardX + 12, cardY + barH + 178, cardW - 24, 1f), new Color(0.3f,0.3f,0.4f,0.5f));

            for (int b = 0; b < r.Bonuses.Length; b++)
            {
                _labelStyle.fontSize = 12; _labelStyle.normal.textColor = r.PrimaryColor;
                GUI.Label(new Rect(cardX + 12, cardY + barH + 186 + b * 22, cardW - 24, 22), "• " + r.Bonuses[b], _labelStyle);
            }

            float resY = cardY + barH + 290;
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.42f,0.42f,0.52f);
            GUI.Label(new Rect(cardX + 12, resY, cardW - 24, 16), "STARTING RESOURCES", _labelStyle);
            resY += 18;
            string[] resLabels = { $"G:{r.StartGold}", $"W:{r.StartWood}", $"S:{r.StartStone}", $"F:{r.StartFood}" };
            for (int ri = 0; ri < 4; ri++)
            {
                _labelStyle.fontSize = 12; _labelStyle.normal.textColor = ResColors[ri];
                GUI.Label(new Rect(cardX + 12 + ri * (cardW - 24) / 4f, resY, (cardW - 24) / 4f, 20), resLabels[ri], _labelStyle);
            }

            GUI.color = new Color(1, 1, 1, 0.01f);
            if (GUI.Button(new Rect(cardX, cardY, cardW, cardH), GUIContent.none)) _selectedRace = i;
            GUI.color = Color.white;
            if (sel) DrawBorder(new Rect(cardX, cardY, cardW, cardH), r.PrimaryColor, 2f);
        }

        float btnW = 260, btnH = 56, btnX = sw / 2f - btnW / 2f, btnY = cardY + cardH + 28;
        var selRace = _races[_selectedRace];
        GUI.backgroundColor = new Color(selRace.PrimaryColor.r*0.7f, selRace.PrimaryColor.g*0.7f, selRace.PrimaryColor.b*0.7f);
        _btnStyle.fontSize = 20;
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "BEGIN  ▶", _btnStyle))
            GameManager.Instance?.StartGameWithRace(selRace);
        _btnStyle.fontSize = 14;
        GUI.backgroundColor = Color.white;

        _labelStyle.fontSize = 13; _labelStyle.normal.textColor = selRace.PrimaryColor;
        _labelStyle.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(0, btnY + btnH + 8, sw, 24), selRace.Name.ToUpper() + " — " + selRace.Title, _labelStyle);
        _labelStyle.alignment = TextAnchor.UpperLeft;
    }

    // ── TOP HUD ───────────────────────────────────────────────────────────────

    void DrawTopHUD(float sw, float sh)
    {
        DrawRect(new Rect(0, 0, sw, 66), new Color(0.06f, 0.06f, 0.1f, 0.92f));

        var rm = ResourceManager.Instance;
        if (rm == null) return;

        for (int i = 0; i < 4; i++)
        {
            float x = 12 + i * 140;
            DrawRect(new Rect(x, 8, 130, 50), new Color(0.12f, 0.12f, 0.18f, 0.85f));
            DrawRect(new Rect(x, 8, 5, 50), ResColors[i]);
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.55f, 0.65f);
            GUI.Label(new Rect(x + 10, 10, 120, 20), ResNames[i], _labelStyle);
            _resourceStyle.normal.textColor = ResColors[i];
            GUI.Label(new Rect(x + 10, 28, 120, 28), FormatNum(rm.GetAmount(ResTypes[i])), _resourceStyle);
        }

        // Population display
        float popX = 12 + 4 * 140;
        DrawRect(new Rect(popX, 8, 100, 50), new Color(0.12f, 0.12f, 0.18f, 0.85f));
        DrawRect(new Rect(popX, 8, 5, 50), new Color(0.7f, 0.85f, 1f));
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.55f, 0.65f);
        GUI.Label(new Rect(popX + 10, 10, 90, 20), "POP", _labelStyle);
        bool popFull = rm.CurrentPopulation >= rm.PopulationCap;
        _resourceStyle.normal.textColor = popFull ? new Color(1f, 0.4f, 0.4f) : new Color(0.7f, 0.85f, 1f);
        GUI.Label(new Rect(popX + 10, 28, 90, 28), $"{rm.CurrentPopulation}/{rm.PopulationCap}", _resourceStyle);

        // Race / title center
        var race = GameManager.Instance?.SelectedRace;
        if (race != null)
        {
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = race.PrimaryColor;
            _labelStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(sw / 2f - 150, 8, 300, 18), race.Name.ToUpper() + " — " + race.Title, _labelStyle);
            _labelStyle.alignment = TextAnchor.UpperLeft;
        }
        _titleStyle.fontSize = 20; _titleStyle.normal.textColor = new Color(0.9f, 0.78f, 0.35f);
        GUI.Label(new Rect(sw / 2f - 150, 22, 300, 44), "REALM FORGE", _titleStyle);

        // Score + time right
        var gm = GameManager.Instance;
        if (gm == null) return;
        var am = AgeManager.Instance;

        if (am != null)
        {
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.6f, 0.75f, 1f);
            GUI.Label(new Rect(sw - 360, 8, 100, 20), "AGE", _labelStyle);
            _labelStyle.fontSize = 13; _labelStyle.normal.textColor = new Color(0.78f, 0.9f, 1f);
            GUI.Label(new Rect(sw - 360, 24, 120, 24), AgeManager.GetAgeLabel(am.CurrentAge), _labelStyle);

            if (am.IsAdvancing)
            {
                DrawRect(new Rect(sw - 360, 50, 120, 8), new Color(0.12f, 0.12f, 0.2f));
                DrawRect(new Rect(sw - 360, 50, 120 * am.AdvanceProgress, 8), new Color(0.2f, 0.7f, 1f));
            }
        }

        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.5f, 0.55f, 0.7f);
        GUI.Label(new Rect(sw - 250, 8, 100, 20), "SCORE", _labelStyle);
        _resourceStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);
        GUI.Label(new Rect(sw - 250, 26, 100, 28), FormatNum(gm.PlayerScore), _resourceStyle);
        _labelStyle.fontSize = 18; _labelStyle.normal.textColor = new Color(0.75f, 0.8f, 0.85f);
        GUI.Label(new Rect(sw - 130, 18, 120, 30), gm.GetFormattedTime(), _labelStyle);
    }

    // ── BOTTOM BAR ────────────────────────────────────────────────────────────

    void DrawBottomBar(float sw, float sh)
    {
        DrawRect(new Rect(0, sh - 66, sw, 66), new Color(0.06f, 0.06f, 0.1f, 0.92f));

        GUI.backgroundColor = new Color(0.2f, 0.45f, 0.75f);
        if (GUI.Button(new Rect(12, sh - 58, 110, 50), "  BUILD", _btnStyle)) ToggleBuildMenu();

        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
        if (GUI.Button(new Rect(sw - 72, sh - 58, 60, 50), "||", _btnStyle)) TogglePause();

        // Worker + military unit count
        _labelStyle.fontSize = 13; _labelStyle.normal.textColor = new Color(0.85f, 0.72f, 0.55f);
        _labelStyle.alignment = TextAnchor.MiddleLeft;
        int totalUnits = Villager.Count;
        foreach (var u in MilitaryUnit.AllUnits) if (!u.IsAI && u.IsAlive) totalUnits++;
        GUI.Label(new Rect(132, sh - 58, 130, 50), $"Units: {totalUnits}", _labelStyle);

        // Group selection badge
        var bm2 = BuildingManager.Instance;
        if (bm2 != null && bm2.SelectedGroup.Count > 1)
        {
            _labelStyle.fontSize = 12; _labelStyle.normal.textColor = new Color(0.3f, 0.8f, 1f);
            GUI.Label(new Rect(270, sh - 58, 160, 50), $"[{bm2.SelectedGroup.Count} selected]", _labelStyle);
        }

        // Advance Age button
        var am = AgeManager.Instance;
        if (am != null)
        {
            GUI.backgroundColor = am.IsAdvancing ? new Color(0.25f, 0.25f, 0.3f) : new Color(0.2f, 0.42f, 0.25f);
            string label = am.IsAdvancing ? "ADVANCING..." : "AGE UP";
            if (GUI.Button(new Rect(sw - 200, sh - 58, 120, 50), label, _btnStyle))
            {
                if (!am.IsAdvancing)
                    am.StartAdvance();
            }
            GUI.backgroundColor = Color.white;
        }

        GUI.backgroundColor = Color.white;
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.4f, 0.4f, 0.5f);
        _labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(sw / 2f - 260, sh - 58, 520, 50),
            "WASD: Move  |  Scroll: Zoom  |  Q/E: Rotate  |  Left-click: Select  |  Right-click: Command",
            _labelStyle);
        _labelStyle.alignment = TextAnchor.UpperLeft;
    }

    // ── BUILD MENU ────────────────────────────────────────────────────────────

    void DrawBuildMenu(float sw, float sh)
    {
        float mw = 264, mh = Mathf.Min(sh - 160, 530), mx = 12, my = sh - mh - 78;
        DrawRect(new Rect(mx, my, mw, mh), new Color(0.07f, 0.07f, 0.12f, 0.97f));

        _titleStyle.fontSize = 16; _titleStyle.normal.textColor = new Color(0.85f, 0.75f, 0.4f);
        GUI.Label(new Rect(mx + 8, my + 6, mw - 40, 28), "BUILD", _titleStyle);

        GUI.backgroundColor = new Color(0.4f, 0.15f, 0.15f);
        if (GUI.Button(new Rect(mx + mw - 30, my + 5, 24, 24), "X", _btnStyle)) _buildMenuOpen = false;
        GUI.backgroundColor = Color.white;

        var bm = BuildingManager.Instance;
        if (bm == null) return;

        float y = my + 38, bh = 64;
        _buildMenuScroll = GUI.BeginScrollView(
            new Rect(mx + 2, y, mw - 4, mh - (y - my) - 4), _buildMenuScroll,
            new Rect(0, 0, mw - 20, bm.AvailableBuildings.Count * (bh + 3)));

        float iy = 0;
        foreach (var bd in bm.AvailableBuildings)
        {
            bool ageOk = AgeManager.Instance == null || AgeManager.Instance.CanBuild(bd);
            bool canAfford = ResourceManager.Instance?.CanAfford(bd.GetCostDict()) ?? false;
            bool canPlace = canAfford && ageOk;
            DrawRect(new Rect(2, iy, mw - 12, bh - 2), canPlace ? new Color(0.13f, 0.16f, 0.2f) : new Color(0.2f, 0.1f, 0.1f));
            DrawRect(new Rect(2, iy, 5, bh - 2), bd.BuildingColor);

            _labelStyle.fontSize = 13; _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.normal.textColor = canPlace ? Color.white : new Color(0.6f, 0.4f, 0.4f);
            GUI.Label(new Rect(12, iy + 4, mw - 20, 24), bd.BuildingName, _labelStyle);
            _labelStyle.fontStyle = FontStyle.Normal;

            string cost = "";
            if (bd.GoldCost  > 0) cost += $"G:{bd.GoldCost}  ";
            if (bd.WoodCost  > 0) cost += $"W:{bd.WoodCost}  ";
            if (bd.StoneCost > 0) cost += $"S:{bd.StoneCost}";
            if (!ageOk) cost += (cost.Length > 0 ? "  " : "") + $"Age {bd.MinAge}";
            _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.68f, 0.68f, 0.58f);
            GUI.Label(new Rect(12, iy + 30, mw - 20, 20), cost.Trim(), _labelStyle);

            GUI.color = new Color(1, 1, 1, 0.01f);
            if (GUI.Button(new Rect(2, iy, mw - 12, bh - 2), GUIContent.none))
            {
                if (!ageOk) ShowMessage("Requires " + AgeManager.GetAgeLabel(bd.MinAge));
                else if (canAfford) { bm.StartPlacement(bd); _buildMenuOpen = false; }
                else ShowMessage("Not enough resources!");
            }
            GUI.color = Color.white;
            iy += bh + 3;
        }
        GUI.EndScrollView();
    }

    // ── BUILDING INFO + TRAINING ──────────────────────────────────────────────

    void DrawBuildingInfo(float sw, float sh)
    {
        var bm = BuildingManager.Instance;
        var researchManager = ResearchManager.Instance;
        bool hasTraining = _selectedBuilding != null && _selectedBuilding.IsBuilt &&
                           bm != null && bm.TrainableUnits.ContainsKey(_selectedBuilding.Data.Type);
        bool hasResearch = _selectedBuilding != null && _selectedBuilding.IsBuilt &&
                           researchManager != null && researchManager.HasResearchForBuilding(_selectedBuilding.Data.Type);
        bool isMarket    = _selectedBuilding != null && _selectedBuilding.IsBuilt &&
                           _selectedBuilding.Data?.Type == BuildingType.Market;

        float ph = 210f;
        if (hasTraining) ph += 130f;
        if (hasResearch) ph += 170f;
        if (isMarket)    ph += 200f;
        float pw = 266f, px = sw - pw - 12, py = sh - ph - 78;
        DrawRect(new Rect(px, py, pw, ph), new Color(0.07f, 0.07f, 0.12f, 0.97f));

        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.5f, 0.55f, 0.75f);
        GUI.Label(new Rect(px + 8, py + 6, pw - 16, 20), "SELECTED", _labelStyle);

        GUI.backgroundColor = new Color(0.35f, 0.18f, 0.18f);
        if (GUI.Button(new Rect(px + pw - 30, py + 5, 24, 24), "X", _btnStyle))
            BuildingManager.Instance?.DeselectAll();
        GUI.backgroundColor = Color.white;

        if (_selectedBuilding?.Data == null) return;
        var d = _selectedBuilding.Data;

        _titleStyle.fontSize = 17; _titleStyle.normal.textColor = new Color(0.9f, 0.78f, 0.35f);
        GUI.Label(new Rect(px + 8, py + 26, pw - 16, 28), d.BuildingName, _titleStyle);

        _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.75f, 0.75f, 0.7f);
        GUI.Label(new Rect(px + 8, py + 55, pw - 16, 48), d.Description, _labelStyle);

        // Health bar
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.75f, 0.55f);
        GUI.Label(new Rect(px + 8, py + 108, 60, 20), "HEALTH", _labelStyle);
        float pct = d.MaxHealth > 0 ? _selectedBuilding.CurrentHealth / d.MaxHealth : 1f;
        DrawRect(new Rect(px + 8, py + 128, pw - 16, 14), new Color(0.18f, 0.18f, 0.2f));
        DrawRect(new Rect(px + 8, py + 128, (pw - 16) * pct, 14), Color.Lerp(Color.red, new Color(0.25f, 0.8f, 0.3f), pct));
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(px + 8, py + 146, pw - 16, 20), $"{(int)_selectedBuilding.CurrentHealth} / {d.MaxHealth}", _labelStyle);

        float sectionY = py + 172;

        if (hasTraining)
        {
            DrawRect(new Rect(px + 8, sectionY, pw - 16, 1f), new Color(0.3f, 0.3f, 0.5f, 0.5f));
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.75f, 0.55f);
            GUI.Label(new Rect(px + 8, sectionY + 6, pw - 16, 18), "TRAIN UNITS", _labelStyle);

            var trainable = bm.TrainableUnits[d.Type];
            float uy = sectionY + 26;
            float btnW = (pw - 20) / 2f;

            for (int i = 0; i < trainable.Count; i++)
            {
                var unit = trainable[i];
                float bx = px + 8 + (i % 2) * (btnW + 4);
                float by  = uy + (i / 2) * 36;

                bool canAfford = CanAffordUnit(unit);
                bool queueFull = _selectedBuilding.IsTrainingQueueFull;
                bool ageOk = AgeManager.Instance == null || AgeManager.Instance.CanTrain(unit);

                GUI.backgroundColor = canAfford && !queueFull && ageOk
                    ? new Color(0.18f, 0.35f, 0.22f)
                    : new Color(0.25f, 0.18f, 0.18f);

                _btnStyle.fontSize = 11;
                if (GUI.Button(new Rect(bx, by, btnW, 30), unit.UnitName, _btnStyle))
                {
                    if (queueFull)
                        ShowMessage("Training queue full!");
                    else if (!ageOk)
                        ShowMessage("Requires " + AgeManager.GetAgeLabel(unit.MinAge));
                    else if (!canAfford)
                        ShowMessage("Not enough resources!");
                    else
                        TrainUnit(unit);
                }
                GUI.backgroundColor = Color.white;
            }

            float progressY = uy + Mathf.Ceil(trainable.Count / 2f) * 36 + 6;
            if (_selectedBuilding.IsTraining)
            {
                _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
                var cur = _selectedBuilding.CurrentTraining;
                GUI.Label(new Rect(px + 8, progressY, pw - 16, 18),
                    $"Training: {cur?.UnitName ?? "?"} ({_selectedBuilding.TrainingQueueCount} in queue)", _labelStyle);
                DrawRect(new Rect(px + 8, progressY + 20, pw - 16, 10), new Color(0.12f, 0.12f, 0.18f));
                DrawRect(new Rect(px + 8, progressY + 20, (pw - 16) * _selectedBuilding.TrainingProgress, 10), new Color(0.2f, 0.7f, 1f));
            }

            sectionY += 130f;
        }

        if (hasResearch)
        {
            DrawRect(new Rect(px + 8, sectionY, pw - 16, 1f), new Color(0.3f, 0.3f, 0.5f, 0.5f));
            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.8f, 0.75f, 0.45f);
            GUI.Label(new Rect(px + 8, sectionY + 6, pw - 16, 18), "RESEARCH", _labelStyle);

            float ry = sectionY + 26;
            float rbtnW = (pw - 20) / 2f;
            var researchList = researchManager.GetAvailableResearch(_selectedBuilding);

            if (researchList.Count == 0)
            {
                _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.6f, 0.65f, 0.7f);
                GUI.Label(new Rect(px + 8, ry, pw - 16, 22), "No available research", _labelStyle);
            }
            else
            {
                for (int i = 0; i < researchList.Count; i++)
                {
                    var research = researchList[i];
                    float bx = px + 8 + (i % 2) * (rbtnW + 4);
                    float by = ry + (i / 2) * 36;

                    bool canStart = researchManager.CanStartResearch(research, _selectedBuilding, out var reason);
                    GUI.backgroundColor = canStart ? new Color(0.3f, 0.32f, 0.18f) : new Color(0.22f, 0.16f, 0.16f);
                    if (GUI.Button(new Rect(bx, by, rbtnW, 30), research.Name, _btnStyle))
                    {
                        if (!canStart) ShowMessage(reason);
                        else researchManager.StartResearch(research, _selectedBuilding);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            if (researchManager.IsResearching)
            {
                float py2 = sectionY + 132f;
                _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.85f, 0.85f, 1f);
                GUI.Label(new Rect(px + 8, py2, pw - 16, 18), "Researching: " + researchManager.CurrentResearch?.Name, _labelStyle);
                DrawRect(new Rect(px + 8, py2 + 18, pw - 16, 10), new Color(0.12f, 0.12f, 0.18f));
                DrawRect(new Rect(px + 8, py2 + 18, (pw - 16) * researchManager.CurrentProgress, 10), new Color(0.8f, 0.7f, 0.2f));
            }

            sectionY += 170f;
        }

        if (isMarket)
            DrawMarketPanel(px, sectionY, pw);
    }

    void DrawMarketPanel(float px, float sectionY, float pw)
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        DrawRect(new Rect(px + 8, sectionY, pw - 16, 1f), new Color(0.3f, 0.3f, 0.5f, 0.5f));
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(1f, 0.82f, 0.25f);
        GUI.Label(new Rect(px + 8, sectionY + 6, pw - 16, 18), "MARKET TRADE  (per 100 units)", _labelStyle);

        // One row per resource: [Label] [Buy Xg] [Sell +Xg] + thin price bar
        for (int i = 0; i < 3; i++)
        {
            float rowY   = sectionY + 28 + i * 56;
            int   buyRate  = Mathf.RoundToInt(_marketRates[i]);
            int   sellRate = Mathf.RoundToInt(_marketRates[i] * 0.65f);
            var   rt       = _marketResTypes[i];
            string rn      = _marketResNames[i];

            // Resource label
            Color labelColor = i == 0 ? new Color(0.95f, 0.65f, 0.4f) :
                               i == 1 ? new Color(0.5f, 0.85f, 0.3f) :
                                        new Color(0.8f, 0.8f, 0.8f);
            _labelStyle.fontSize = 11; _labelStyle.normal.textColor = labelColor;
            GUI.Label(new Rect(px + 8, rowY + 4, 44, 20), rn, _labelStyle);

            // Buy
            bool canBuy  = rm.HasResources(ResourceType.Gold, buyRate);
            GUI.backgroundColor = canBuy ? new Color(0.15f, 0.38f, 0.20f) : new Color(0.22f, 0.14f, 0.14f);
            _btnStyle.fontSize = 10;
            if (GUI.Button(new Rect(px + 56, rowY, (pw - 72) / 2f - 2, 26), $"Buy {buyRate}G", _btnStyle))
            {
                if (!canBuy) ShowMessage("Not enough gold!");
                else
                {
                    rm.RemoveResource(ResourceType.Gold, buyRate);
                    rm.AddResource(rt, MarketTradeAmt);
                    _marketRates[i] = Mathf.Min(300f, _marketRates[i] + 5f);
                    ShowMessage($"Bought 100 {rn}.");
                }
            }

            // Sell
            bool canSell = rm.HasResources(rt, MarketTradeAmt);
            GUI.backgroundColor = canSell ? new Color(0.38f, 0.22f, 0.12f) : new Color(0.22f, 0.14f, 0.14f);
            float sellBtnX = px + 56 + (pw - 72) / 2f + 2;
            if (GUI.Button(new Rect(sellBtnX, rowY, (pw - 72) / 2f - 2, 26), $"Sell +{sellRate}G", _btnStyle))
            {
                if (!canSell) ShowMessage($"Need 100 {rn} to sell!");
                else
                {
                    rm.RemoveResource(rt, MarketTradeAmt);
                    rm.AddResource(ResourceType.Gold, sellRate);
                    _marketRates[i] = Mathf.Max(50f, _marketRates[i] - 5f);
                    ShowMessage($"Sold 100 {rn} for {sellRate} gold.");
                }
            }
            GUI.backgroundColor = Color.white;
            _btnStyle.fontSize = 14;

            // Rate bar (visual price indicator)
            float rateNorm = (_marketRates[i] - 50f) / 250f;
            DrawRect(new Rect(px + 8, rowY + 32, pw - 16, 6), new Color(0.12f, 0.12f, 0.18f));
            DrawRect(new Rect(px + 8, rowY + 32, (pw - 16) * rateNorm, 6),
                     Color.Lerp(new Color(0.2f, 0.8f, 0.3f), new Color(0.9f, 0.2f, 0.2f), rateNorm));
        }

        _labelStyle.fontSize = 9; _labelStyle.normal.textColor = new Color(0.42f, 0.42f, 0.52f);
        GUI.Label(new Rect(px + 8, sectionY + 28 + 3 * 56, pw - 16, 18),
            "Prices fluctuate with each trade.", _labelStyle);
    }

    bool CanAffordUnit(UnitData unit)
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return false;
        return rm.HasResources(ResourceType.Gold, unit.GoldCost) &&
               rm.HasResources(ResourceType.Wood,  unit.WoodCost) &&
               rm.HasResources(ResourceType.Food,  unit.FoodCost) &&
               rm.CanTrainUnit(unit.PopulationCost);
    }

    void TrainUnit(UnitData unit)
    {
        var rm = ResourceManager.Instance;
        if (rm == null || _selectedBuilding == null) return;
        if (unit.GoldCost > 0) rm.RemoveResource(ResourceType.Gold, unit.GoldCost);
        if (unit.WoodCost > 0) rm.RemoveResource(ResourceType.Wood, unit.WoodCost);
        if (unit.FoodCost > 0) rm.RemoveResource(ResourceType.Food, unit.FoodCost);
        _selectedBuilding.EnqueueUnit(unit);
    }

    string BuildUnitCostString(UnitData unit)
    {
        string s = "";
        if (unit.GoldCost > 0) s += $"G:{unit.GoldCost} ";
        if (unit.WoodCost > 0) s += $"W:{unit.WoodCost} ";
        if (unit.FoodCost > 0) s += $"F:{unit.FoodCost}";
        return s.Trim();
    }

    // ── MILITARY UNIT INFO ────────────────────────────────────────────────────

    void DrawMilitaryInfo(float sw, float sh)
    {
        if (_selectedMilitary == null) return;
        bool isTrebuchet = _selectedMilitary.Data?.Type == UnitType.Trebuchet;
        float pw = 220, ph = isTrebuchet ? 210f : 160f, px = sw - pw - 12, py = sh - ph - 78;
        DrawRect(new Rect(px, py, pw, ph), new Color(0.07f, 0.07f, 0.12f, 0.97f));

        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.5f, 0.55f, 0.75f);
        GUI.Label(new Rect(px + 8, py + 6, pw - 16, 18), "UNIT SELECTED", _labelStyle);

        GUI.backgroundColor = new Color(0.35f, 0.18f, 0.18f);
        if (GUI.Button(new Rect(px + pw - 30, py + 5, 24, 24), "X", _btnStyle))
            BuildingManager.Instance?.DeselectAll();
        GUI.backgroundColor = Color.white;

        var d = _selectedMilitary.Data;
        if (d == null) return;

        _titleStyle.fontSize = 16; _titleStyle.normal.textColor = new Color(0.9f, 0.78f, 0.35f);
        GUI.Label(new Rect(px + 8, py + 26, pw - 16, 26), d.UnitName, _titleStyle);

        _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);
        GUI.Label(new Rect(px + 8, py + 52, pw - 16, 20), $"ATK: {d.Attack}  RNG: {d.AttackRange}", _labelStyle);
        GUI.Label(new Rect(px + 8, py + 70, pw - 16, 20), $"ARM: {d.MeleeArmor}/{d.PierceArmor}  SPD: {d.MoveSpeed}", _labelStyle);

        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.75f, 0.55f);
        GUI.Label(new Rect(px + 8, py + 94, pw - 16, 18), "HEALTH", _labelStyle);

        float pct = _selectedMilitary.MaxHP > 0 ? _selectedMilitary.HP / _selectedMilitary.MaxHP : 1f;
        DrawRect(new Rect(px + 8, py + 114, pw - 16, 14), new Color(0.18f, 0.18f, 0.2f));
        DrawRect(new Rect(px + 8, py + 114, (pw - 16) * pct, 14), Color.Lerp(Color.red, new Color(0.25f, 0.8f, 0.3f), pct));
        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(px + 8, py + 132, pw - 16, 20), $"{(int)_selectedMilitary.HP} / {(int)_selectedMilitary.MaxHP}", _labelStyle);

        // Trebuchet deploy / pack button
        if (isTrebuchet)
        {
            float btnY = py + 158;
            bool animating = _selectedMilitary.TrebuchetAnimating;
            bool deployed  = _selectedMilitary.TrebuchetDeployed;
            string label   = animating ? (deployed ? "Packing..." : "Deploying...") :
                             deployed  ? "  Pack Up" : "  Deploy";
            GUI.backgroundColor = animating ? new Color(0.3f, 0.3f, 0.3f)
                                : deployed   ? new Color(0.6f, 0.35f, 0.15f)
                                             : new Color(0.15f, 0.45f, 0.25f);
            _btnStyle.fontSize = 13;
            if (GUI.Button(new Rect(px + 8, btnY, pw - 16, 36), label, _btnStyle))
                _selectedMilitary.TrebuchetToggleDeploy();
            _btnStyle.fontSize = 14;
            GUI.backgroundColor = Color.white;

            _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.55f, 0.55f, 0.65f);
            string statusText = animating ? "Animating..." : deployed ? "Deployed — can fire" : "Packed — can move";
            GUI.Label(new Rect(px + 8, btnY + 38, pw - 16, 18), statusText, _labelStyle);
        }
    }

    // ── MINIMAP ───────────────────────────────────────────────────────────────

    void DrawMinimap(float sw, float sh)
    {
        float ms = 128f;
        float mx = sw - ms - 12, my = 78;
        DrawRect(new Rect(mx - 2, my - 2, ms + 4, ms + 4), new Color(0.05f, 0.05f, 0.1f));

        var fog = FogOfWar.Instance;
        if (fog?.MinimapTexture != null)
            GUI.DrawTexture(new Rect(mx, my, ms, ms), fog.MinimapTexture);

        float worldExtent = FogOfWar.WorldSize;
        float worldMin    = FogOfWar.WorldMin;

        System.Func<float, float, Vector2> toMM = (wx, wz) =>
            new Vector2(mx + (wx - worldMin) / worldExtent * ms,
                        my + ms - (wz - worldMin) / worldExtent * ms);

        // Resource nodes
        foreach (var node in _minimapNodes)
        {
            if (node == null) continue;
            Color nc = node.Type == ResourceNode.NodeType.Gold  ? new Color(1f,0.85f,0.1f) :
                       node.Type == ResourceNode.NodeType.Stone ? new Color(0.8f,0.8f,0.8f) :
                                                                  new Color(0.4f,0.25f,0.1f);
            var p = toMM(node.transform.position.x, node.transform.position.z);
            DrawRect(new Rect(p.x - 1.5f, p.y - 1.5f, 3, 3), nc);
        }

        // Player buildings (blue)
        foreach (var b in _minimapBuildings)
        {
            if (b == null || !b.IsBuilt) continue;
            Color bc = b.IsAI ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.6f, 1f);
            var p = toMM(b.transform.position.x, b.transform.position.z);
            DrawRect(new Rect(p.x - 2.5f, p.y - 2.5f, 5, 5), bc);
        }

        // Villagers (green)
        foreach (var v in _minimapVillagers)
        {
            if (v == null) continue;
            var p = toMM(v.transform.position.x, v.transform.position.z);
            DrawRect(new Rect(p.x - 1, p.y - 1, 2, 2), new Color(0.4f, 1f, 0.5f));
        }

        // Military units
        foreach (var u in MilitaryUnit.AllUnits)
        {
            if (u == null || !u.IsAlive) continue;
            Color uc = u.IsAI ? new Color(1f, 0.2f, 0.2f) : new Color(0.3f, 0.8f, 1f);
            var p = toMM(u.transform.position.x, u.transform.position.z);
            DrawRect(new Rect(p.x - 1.5f, p.y - 1.5f, 3, 3), uc);
        }

        // Camera indicator
        if (Camera.main != null)
        {
            var camPos = toMM(Camera.main.transform.position.x, Camera.main.transform.position.z);
            DrawRect(new Rect(camPos.x - 2, camPos.y - 2, 4, 4), Color.white);
        }
    }

    // ── MESSAGE ───────────────────────────────────────────────────────────────

    void DrawMessage(float sw, float sh)
    {
        float mw = 430, mh = 52, mx = sw / 2f - mw / 2f, my = sh * 0.12f;
        DrawRect(new Rect(mx, my, mw, mh), new Color(0.08f, 0.08f, 0.14f, 0.96f));
        _labelStyle.fontSize = 16; _labelStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);
        _labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(mx + 8, my, mw - 16, mh), _message, _labelStyle);
        _labelStyle.alignment = TextAnchor.UpperLeft;
    }

    // ── PAUSE ─────────────────────────────────────────────────────────────────

    void DrawPauseOverlay(float sw, float sh)
    {
        DrawRect(new Rect(0, 0, sw, sh), new Color(0, 0, 0, 0.62f));
        _titleStyle.fontSize = 40; _titleStyle.normal.textColor = new Color(0.9f, 0.78f, 0.35f);
        GUI.Label(new Rect(sw / 2f - 180, sh / 2f - 90, 360, 70), "PAUSED", _titleStyle);
        GUI.backgroundColor = new Color(0.2f, 0.45f, 0.75f); _btnStyle.fontSize = 18;
        if (GUI.Button(new Rect(sw / 2f - 90, sh / 2f, 180, 54), "RESUME", _btnStyle)) TogglePause();
        _btnStyle.fontSize = 14; GUI.backgroundColor = Color.white;
    }

    // ── VICTORY / DEFEAT ──────────────────────────────────────────────────────

    void DrawGameOverlay(float sw, float sh, bool victory)
    {
        DrawRect(new Rect(0, 0, sw, sh), new Color(0, 0, 0, 0.78f));

        Color titleColor = victory ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
        string titleText = victory ? "VICTORY!" : "DEFEAT";

        _titleStyle.fontSize = 52; _titleStyle.normal.textColor = titleColor;
        GUI.Label(new Rect(sw / 2f - 250, sh / 2f - 120, 500, 80), titleText, _titleStyle);

        _labelStyle.fontSize = 16; _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        _labelStyle.alignment = TextAnchor.UpperCenter;
        string subText = victory
            ? "The enemy civilization has been destroyed."
            : "Your civilization has fallen.";
        GUI.Label(new Rect(0, sh / 2f - 30, sw, 30), subText, _labelStyle);

        // Stats panel
        var gm = GameManager.Instance;
        if (gm != null)
        {
            float statW = 480f, statH = 120f;
            float statX = sw / 2f - statW / 2f, statY = sh / 2f + 10f;
            DrawRect(new Rect(statX, statY, statW, statH), new Color(0.06f, 0.06f, 0.12f, 0.85f));

            _labelStyle.fontSize = 13; _labelStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            _labelStyle.alignment = TextAnchor.UpperCenter;

            GUI.Label(new Rect(statX, statY + 8, statW, 22),
                $"Time: {gm.GetFormattedTime()}   |   Score: {gm.PlayerScore}", _labelStyle);

            _labelStyle.fontSize = 12; _labelStyle.normal.textColor = new Color(0.62f, 0.72f, 0.82f);

            string row1 = $"Buildings built: {gm.BuildingsBuilt}   |   Units lost: {gm.UnitsLost}";
            GUI.Label(new Rect(statX, statY + 36, statW, 20), row1, _labelStyle);

            string row2 = $"Enemies killed: {gm.EnemiesKilled}   |   Relics secured: {gm.RelicsDeposited}";
            GUI.Label(new Rect(statX, statY + 60, statW, 20), row2, _labelStyle);

            var race = gm.SelectedRace;
            if (race != null)
            {
                _labelStyle.fontSize = 11; _labelStyle.normal.textColor = race.PrimaryColor;
                GUI.Label(new Rect(statX, statY + 86, statW, 20), $"Civilization: {race.Name} — {race.Title}", _labelStyle);
            }
        }
        _labelStyle.alignment = TextAnchor.UpperLeft;

        GUI.backgroundColor = new Color(0.2f, 0.45f, 0.75f); _btnStyle.fontSize = 18;
        if (GUI.Button(new Rect(sw / 2f - 100, sh / 2f + 60, 200, 54), "PLAY AGAIN", _btnStyle))
            GameManager.Instance?.RestartGame();
        _btnStyle.fontSize = 14; GUI.backgroundColor = Color.white;
    }

    // ── GROUP INFO ────────────────────────────────────────────────────────────

    void DrawGroupInfo(float sw, float sh)
    {
        float pw = 220, ph = 80, px = sw - pw - 12, py = sh - ph - 78;
        DrawRect(new Rect(px, py, pw, ph), new Color(0.07f, 0.07f, 0.12f, 0.97f));

        _labelStyle.fontSize = 10; _labelStyle.normal.textColor = new Color(0.5f, 0.55f, 0.75f);
        GUI.Label(new Rect(px + 8, py + 6, pw - 16, 18), "GROUP SELECTED", _labelStyle);

        GUI.backgroundColor = new Color(0.35f, 0.18f, 0.18f);
        if (GUI.Button(new Rect(px + pw - 30, py + 5, 24, 24), "X", _btnStyle))
            BuildingManager.Instance?.DeselectAll();
        GUI.backgroundColor = Color.white;

        _titleStyle.fontSize = 17; _titleStyle.normal.textColor = new Color(0.3f, 0.8f, 1f);
        GUI.Label(new Rect(px + 8, py + 26, pw - 16, 28), $"{_groupCount} unit{(_groupCount != 1 ? "s" : "")} selected", _titleStyle);

        _labelStyle.fontSize = 11; _labelStyle.normal.textColor = new Color(0.6f, 0.7f, 0.6f);
        GUI.Label(new Rect(px + 8, py + 54, pw - 16, 20), "Right-click to command group", _labelStyle);
    }

    // ── DRAG SELECTION RECT ───────────────────────────────────────────────────

    void DrawDragSelectionRect()
    {
        var bm = BuildingManager.Instance;
        if (bm == null || !bm.IsDragging) return;

        // Convert Unity screen coords (y=0 bottom) to IMGUI coords (y=0 top)
        float sh = Screen.height;
        Vector2 start = bm.DragStart;
        Vector2 end   = bm.DragEnd;

        float x = Mathf.Min(start.x, end.x);
        float y = sh - Mathf.Max(start.y, end.y);
        float w = Mathf.Abs(end.x - start.x);
        float h = Mathf.Abs(end.y - start.y);

        Rect rect = new Rect(x, y, w, h);
        DrawRect(rect, new Color(0.2f, 0.8f, 1f, 0.12f));
        DrawBorder(rect, new Color(0.2f, 0.8f, 1f, 0.7f), 1.5f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void DrawRect(Rect r, Color c)
    {
        GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
    }

    void DrawBorder(Rect r, Color c, float thickness)
    {
        DrawRect(new Rect(r.x, r.y, r.width, thickness), c);
        DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
        DrawRect(new Rect(r.x, r.y, thickness, r.height), c);
        DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
    }

    void EnsureStyles()
    {
        if (_stylesReady) return; _stylesReady = true;
        _labelStyle    = new GUIStyle(GUI.skin.label) { richText = true };
        _titleStyle    = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 22, alignment = TextAnchor.MiddleCenter };
        _btnStyle      = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 14 };
        _btnStyle.normal.textColor = Color.white;
        _btnStyle.hover.textColor  = Color.white;
        _resourceStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 22 };
    }

    string FormatNum(int n) => n >= 1000 ? $"{n / 1000f:0.#}k" : n.ToString();
}
