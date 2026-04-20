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

            // Score
            GameManager.Instance?.AddScore(Data.GoldCost + Data.WoodCost + Data.StoneCost);

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

    // ── Visuals ───────────────────────────────────────────────────────────────

    void CreateVisuals()
    {
        // Selection ring
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _selectionIndicator.transform.SetParent(transform);
        _selectionIndicator.transform.localPosition = new Vector3(0, -0.45f, 0);
        float size = Data != null ? Mathf.Max(Data.Width, Data.Height) + 0.5f : 2.5f;
        _selectionIndicator.transform.localScale = new Vector3(size, 0.05f, size);
        _selectionIndicator.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Destroy(_selectionIndicator.GetComponent<Collider>());
        _selectionIndicator.SetActive(false);

        // Health bar
        _healthBar = new GameObject("HealthBar");
        _healthBar.transform.SetParent(transform);
        _healthBar.transform.localPosition = new Vector3(0, 2f, 0);

        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.transform.SetParent(_healthBar.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale    = new Vector3(1.2f, 0.15f, 0.05f);
        bg.GetComponent<Renderer>().material.color = Color.black;
        Destroy(bg.GetComponent<Collider>());

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.transform.SetParent(_healthBar.transform);
        fill.transform.localPosition = new Vector3(-0.001f, 0f, -0.01f);
        fill.transform.localScale    = new Vector3(1.18f, 0.12f, 0.05f);
        fill.GetComponent<Renderer>().material.color = Color.green;
        Destroy(fill.GetComponent<Collider>());
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
        float pct = CurrentHealth / Data.MaxHealth;
        _healthBarFill.localScale = new Vector3(1.18f * pct, 0.12f, 0.05f);
        var rend = _healthBarFill.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.Lerp(Color.red, Color.green, pct);
    }

    void LateUpdate()
    {
        if (_healthBar != null && Camera.main != null)
            _healthBar.transform.rotation = Camera.main.transform.rotation;
    }

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
