using UnityEngine;
using System.Collections;

public class Building : MonoBehaviour
{
    [Header("Configuration")]
    public BuildingData Data;

    [Header("State")]
    public bool IsBuilt = false;
    public bool IsSelected = false;
    public float CurrentHealth;
    public float BuildProgress = 0f;

    private Renderer[] _renderers;
    private Color _originalColor;
    private GameObject _selectionIndicator;
    private GameObject _healthBar;
    private Transform _healthBarFill;

    public Vector2Int GridPosition { get; set; }

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        CurrentHealth = Data != null ? Data.MaxHealth : 100;
    }

    void Start()
    {
        CreateVisuals();
        if (Data != null)
            StartCoroutine(BuildRoutine());
    }

    void CreateVisuals()
    {
        // Selection ring
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _selectionIndicator.transform.SetParent(transform);
        _selectionIndicator.transform.localPosition = new Vector3(0, -0.45f, 0);
        float size = Data != null ? Mathf.Max(Data.Width, Data.Height) + 0.5f : 2.5f;
        _selectionIndicator.transform.localScale = new Vector3(size, 0.05f, size);
        var selRend = _selectionIndicator.GetComponent<Renderer>();
        selRend.material.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Destroy(_selectionIndicator.GetComponent<Collider>());
        _selectionIndicator.SetActive(false);

        // Health bar (world space)
        _healthBar = new GameObject("HealthBar");
        _healthBar.transform.SetParent(transform);
        _healthBar.transform.localPosition = new Vector3(0, 2f, 0);

        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.transform.SetParent(_healthBar.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(1.2f, 0.15f, 0.05f);
        bg.GetComponent<Renderer>().material.color = Color.black;
        Destroy(bg.GetComponent<Collider>());

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.transform.SetParent(_healthBar.transform);
        fill.transform.localPosition = new Vector3(-0.001f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(1.18f, 0.12f, 0.05f);
        fill.GetComponent<Renderer>().material.color = Color.green;
        Destroy(fill.GetComponent<Collider>());
        _healthBarFill = fill.transform;
    }

    IEnumerator BuildRoutine()
    {
        IsBuilt = false;
        BuildProgress = 0f;

        // Set under-construction color
        SetColor(new Color(0.5f, 0.5f, 0.5f, 0.6f));

        float buildTime = Data.BuildTime;
        float elapsed = 0f;

        while (elapsed < buildTime)
        {
            elapsed += Time.deltaTime;
            BuildProgress = elapsed / buildTime;
            transform.localScale = Vector3.Lerp(
                new Vector3(1f, 0.1f, 1f),
                Vector3.one,
                BuildProgress
            );
            yield return null;
        }

        IsBuilt = true;
        transform.localScale = Vector3.one;
        SetColor(Data.BuildingColor);

        // Register production
        if (Data.GoldProduction > 0) ResourceManager.Instance?.AddProduction(ResourceType.Gold, Data.GoldProduction);
        if (Data.WoodProduction > 0) ResourceManager.Instance?.AddProduction(ResourceType.Wood, Data.WoodProduction);
        if (Data.StoneProduction > 0) ResourceManager.Instance?.AddProduction(ResourceType.Stone, Data.StoneProduction);
        if (Data.FoodProduction > 0) ResourceManager.Instance?.AddProduction(ResourceType.Food, Data.FoodProduction);

        // Score
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(Data.GoldCost + Data.WoodCost + Data.StoneCost);

        // Territory expansion
        if (TerritoryManager.Instance != null)
            TerritoryManager.Instance.ExpandTerritory(GridPosition, Data.Width, Data.Height);
    }

    void SetColor(Color color)
    {
        foreach (var r in _renderers)
        {
            if (r.gameObject != _selectionIndicator && r.gameObject.name != "HealthBar")
                r.material.color = color;
        }
    }

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

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateHealthBar();
        if (CurrentHealth <= 0) Die();
    }

    void UpdateHealthBar()
    {
        if (_healthBarFill == null || Data == null) return;
        float pct = CurrentHealth / Data.MaxHealth;
        _healthBarFill.localScale = new Vector3(1.18f * pct, 0.12f, 0.05f);
        var rend = _healthBarFill.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = Color.Lerp(Color.red, Color.green, pct);
    }

    void Die()
    {
        // Unregister production
        if (Data != null && ResourceManager.Instance != null)
        {
            if (Data.GoldProduction > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Gold, Data.GoldProduction);
            if (Data.WoodProduction > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Wood, Data.WoodProduction);
            if (Data.StoneProduction > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Stone, Data.StoneProduction);
            if (Data.FoodProduction > 0) ResourceManager.Instance.RemoveProduction(ResourceType.Food, Data.FoodProduction);
        }

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.RemoveBuilding(this);

        Destroy(gameObject, 0.2f);
    }

    void LateUpdate()
    {
        // Health bar always faces camera
        if (_healthBar != null && Camera.main != null)
            _healthBar.transform.rotation = Camera.main.transform.rotation;
    }
}
