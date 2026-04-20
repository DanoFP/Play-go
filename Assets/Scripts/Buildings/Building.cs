using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Building : MonoBehaviour
{
    [Header("Configuration")]
    public BuildingData Data;

    [Header("Ownership")]
    public bool IsAI = false;

    [Header("State")]
    public bool IsBuilt       = false;
    public bool IsSelected    = false;
    public float CurrentHealth;
    public float BuildProgress = 0f;

    // ── Training queue ────────────────────────────────────────────────────────
    readonly Queue<UnitData> _trainingQueue = new Queue<UnitData>();
    float _trainingProgress;
    float _trainingTimeTotal;
    int _appliedFoodProduction;

    public bool  IsTraining       => _trainingTimeTotal > 0f;
    public bool  IsTrainingQueueFull => _trainingQueue.Count >= 5;
    public int   TrainingQueueCount  => _trainingQueue.Count;
    public float TrainingProgress    => _trainingTimeTotal > 0f ? _trainingProgress / _trainingTimeTotal : 0f;
    public UnitData CurrentTraining  => _trainingQueue.Count > 0 ? _trainingQueue.Peek() : null;

    // ── Visuals ───────────────────────────────────────────────────────────────
    Renderer[]  _renderers;
    Color       _originalColor;
    GameObject  _selectionIndicator;
    GameObject  _healthBar;
    Transform   _healthBarFill;

    public Vector2Int GridPosition { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _renderers    = GetComponentsInChildren<Renderer>();
        CurrentHealth = Data != null ? Data.MaxHealth : 100;
    }

    void Start()
    {
        CreateVisuals();
        if (Data != null)
            StartCoroutine(BuildRoutine());
    }

    void Update()
    {
        if (IsBuilt) ProcessTraining();
    }

    // ── Training ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Add a unit to the training queue. The caller must deduct resources first.
    /// </summary>
    public void EnqueueUnit(UnitData unit)
    {
        if (unit == null || _trainingQueue.Count >= 5) return;
        if (!IsAI && AgeManager.Instance != null && !AgeManager.Instance.CanTrain(unit))
        {
            UIManager.Instance?.ShowMessage("Requires " + AgeManager.GetAgeLabel(unit.MinAge));
            return;
        }
        _trainingQueue.Enqueue(unit);

        // Start timer if queue was empty
        if (_trainingQueue.Count == 1)
        {
            _trainingProgress  = 0f;
            _trainingTimeTotal = unit.TrainTime;
        }
    }

    void ProcessTraining()
    {
        if (_trainingQueue.Count == 0) return;

        _trainingProgress += Time.deltaTime;

        if (_trainingProgress >= _trainingTimeTotal)
        {
            var unit = _trainingQueue.Dequeue();
            SpawnTrainedUnit(unit);

            if (_trainingQueue.Count > 0)
            {
                var next       = _trainingQueue.Peek();
                _trainingProgress  = 0f;
                _trainingTimeTotal = next.TrainTime;
            }
            else
            {
                _trainingProgress  = 0f;
                _trainingTimeTotal = 0f;
            }
        }
    }

    void SpawnTrainedUnit(UnitData unit)
    {
        float gridSize = BuildingManager.Instance != null ? BuildingManager.Instance.GridSize : 2f;
        float offsetX  = (Data != null ? Data.Width * gridSize : 2f) * 0.5f + 1.5f;
        Vector3 spawnPos = transform.position
            + new Vector3(offsetX + Random.Range(-0.5f, 0.5f), 0f, Random.Range(-1f, 1f));

        var militaryUnit = MilitaryUnit.SpawnAt(spawnPos, unit, IsAI);
        if (militaryUnit == null) return;

        // Notify AI controller so it can track the new unit
        if (IsAI)
            AIController.Instance?.RegisterUnit(militaryUnit);
    }

    // ── Build routine ─────────────────────────────────────────────────────────

    IEnumerator BuildRoutine()
    {
        IsBuilt     = false;
        BuildProgress = 0f;
        SetColor(new Color(0.5f, 0.5f, 0.5f, 0.6f));

        float elapsed = 0f;
        while (elapsed < Data.BuildTime)
        {
            elapsed      += Time.deltaTime;
            BuildProgress = elapsed / Data.BuildTime;
            transform.localScale = Vector3.Lerp(new Vector3(1f, 0.1f, 1f), Vector3.one, BuildProgress);
            yield return null;
        }

        IsBuilt = true;
        transform.localScale = Vector3.one;
        SetColor(Data.BuildingColor);

        OnBuildComplete();
    }

    public void CompleteInstantlyForBootstrap()
    {
        StopAllCoroutines();
        IsBuilt = true;
        BuildProgress = 1f;
        transform.localScale = Vector3.one;
        if (Data != null) SetColor(Data.BuildingColor);
        OnBuildComplete();
    }

    void OnBuildComplete()
    {
        // Player-only side effects
        if (!IsAI)
        {
            // Production
            if (Data.GoldProduction  > 0) ResourceManager.Instance?.AddProduction(ResourceType.Gold,  Data.GoldProduction);
            if (Data.WoodProduction  > 0) ResourceManager.Instance?.AddProduction(ResourceType.Wood,  Data.WoodProduction);
            if (Data.StoneProduction > 0) ResourceManager.Instance?.AddProduction(ResourceType.Stone, Data.StoneProduction);
            if (Data.FoodProduction  > 0)
            {
                _appliedFoodProduction = GetEffectiveFoodProduction();
                ResourceManager.Instance?.AddProduction(ResourceType.Food, _appliedFoodProduction);
            }

            // Population cap
            if (Data.Type == BuildingType.House)
                ResourceManager.Instance?.AddPopCap(10);
            else if (Data.Type == BuildingType.TownCenter)
                ResourceManager.Instance?.AddPopCap(5);

            // Score + stats
            GameManager.Instance?.AddScore(Data.GoldCost + Data.WoodCost + Data.StoneCost);
            GameManager.Instance?.NotifyBuildingBuilt();

            // Territory
            TerritoryManager.Instance?.ExpandTerritory(GridPosition, Data.Width, Data.Height);

            // Town Center spawns a villager
            if (Data.Type == BuildingType.TownCenter)
                Villager.SpawnAt(transform.position + new Vector3(2.5f, 0f, 0f));
        }

        // Reveal fog around newly built structure (both player and AI)
        FogOfWar.Instance?.Reveal(transform.position, 14f);

        // Apply race HP multiplier for player buildings
        if (!IsAI && GameManager.Instance?.SelectedRace != null)
            CurrentHealth *= GameManager.Instance.SelectedRace.BuildingHPMultiplier;

        // Apply Masonry bonus retroactively if already researched when building completes
        if (!IsAI && ResearchManager.Instance != null && ResearchManager.Instance.HasMasonry() && Data != null)
        {
            int bonus = Mathf.Max(1, Mathf.RoundToInt(Data.MaxHealth * 0.1f));
            AddHealth(bonus);
        }

        // Tower auto-attack component (player towers only)
        if (!IsAI && Data.Type == BuildingType.Tower)
            gameObject.AddComponent<TowerDefense>();
    }

    // ── Damage / Death ────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateHealthBar();
        if (CurrentHealth <= 0) Die();
    }

    public void AddHealth(int amount)
    {
        if (Data == null) return;
        // Scale current HP proportionally so we don't instantly heal to full
        float pct = Data.MaxHealth > 0 ? CurrentHealth / Data.MaxHealth : 1f;
        Data.MaxHealth += amount;
        CurrentHealth = Data.MaxHealth * pct;
        UpdateHealthBar();
    }

    void Die()
    {
        if (!IsAI && ResourceManager.Instance != null)
        {
            if (Data.GoldProduction  > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Gold,  Data.GoldProduction);
            if (Data.WoodProduction  > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Wood,  Data.WoodProduction);
            if (Data.StoneProduction > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Stone, Data.StoneProduction);
            if (Data.FoodProduction  > 0)
            {
                int remove = _appliedFoodProduction > 0 ? _appliedFoodProduction : GetEffectiveFoodProduction();
                ResourceManager.Instance.RemoveProduction(ResourceType.Food, remove);
            }

            // Remove pop cap contribution
            if (Data.Type == BuildingType.House)
                ResourceManager.Instance.RemovePopCap(10);
            else if (Data.Type == BuildingType.TownCenter)
                ResourceManager.Instance.RemovePopCap(5);
        }

        BuildingManager.Instance?.RemoveBuilding(this);

        Destroy(gameObject, 0.2f);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    public void Select()
    {
        IsSelected = true;
        _selectionIndicator?.SetActive(true);
    }

    public void Deselect()
    {
        IsSelected = false;
        _selectionIndicator?.SetActive(false);
    }

    // ── Visuals (pixel-art top-down) ─────────────────────────────────────────

    void CreateVisuals()
    {
        float size = Data != null ? Mathf.Max(Data.Width, Data.Height) * BuildingManager.Instance?.GridSize ?? 2f : 2f;

        // Selection indicator: flat glowing ring on XZ plane
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _selectionIndicator.transform.SetParent(transform);
        _selectionIndicator.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        _selectionIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        float selSize = size * 1.15f;
        _selectionIndicator.transform.localScale = new Vector3(selSize, selSize, 1f);
        var selMat = _selectionIndicator.GetComponent<Renderer>().material;
        selMat.color = new Color(0.2f, 0.9f, 1f, 0.55f);
        Destroy(_selectionIndicator.GetComponent<Collider>());
        _selectionIndicator.SetActive(false);

        // Health bar: two flat sprite quads
        _healthBar = new GameObject("HealthBar");
        _healthBar.transform.SetParent(transform);
        _healthBar.transform.localPosition = Vector3.zero;

        float barW = Mathf.Clamp(size * 0.85f, 0.8f, 3.5f);
        float barH = 0.22f;
        float barY = 0.12f;

        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.9f));
        bgTex.Apply();

        var fillTex = new Texture2D(1, 1);
        fillTex.SetPixel(0, 0, Color.green);
        fillTex.Apply();

        var bg   = SpriteQuad.Create(bgTex,   barW,       barH,       barY,          _healthBar.transform);
        var fill = SpriteQuad.Create(fillTex, barW - 0.05f, barH * 0.6f, barY + 0.001f, _healthBar.transform);
        bg.name   = "HPBarBG";
        fill.name = "HPBarFill";
        _healthBarFill = fill.transform;
    }

    void SetColor(Color color)
    {
        foreach (var r in _renderers)
            if (r != null && r.gameObject != _selectionIndicator && r.gameObject.name != "HealthBar")
                r.material.color = color;
    }

    void UpdateHealthBar()
    {
        if (_healthBarFill == null || Data == null) return;
        float pct = Mathf.Clamp01(CurrentHealth / Data.MaxHealth);
        float barW = _healthBarFill.parent.GetComponent<MeshRenderer>() == null
            ? Mathf.Clamp(Mathf.Max(Data.Width, Data.Height) * (BuildingManager.Instance?.GridSize ?? 2f) * 0.85f, 0.8f, 3.5f)
            : 1f;
        float fullW = barW - 0.05f;
        // Scale on X axis (world X = sprite width since it's a flat quad)
        _healthBarFill.localScale = new Vector3(fullW * pct, _healthBarFill.localScale.y, 1f);
        // Shift so bar shrinks from right
        _healthBarFill.localPosition = new Vector3(-fullW * (1f - pct) * 0.5f, _healthBarFill.localPosition.y, _healthBarFill.localPosition.z);
        var rend = _healthBarFill.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.Lerp(Color.red, Color.green, pct);
    }

    // LateUpdate not needed for top-down orthographic camera
    void LateUpdate() { }

    int GetEffectiveFoodProduction()
    {
        if (Data == null) return 0;
        int baseProduction = Data.FoodProduction;
        if (baseProduction <= 0) return 0;
        if (Data.Type != BuildingType.Farm) return baseProduction;

        float mult = ResearchManager.Instance?.GetFarmFoodMultiplier() ?? 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseProduction * mult));
    }
}
