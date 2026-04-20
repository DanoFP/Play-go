using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public enum ResourceType { Gold, Wood, Stone, Food }

[System.Serializable]
public class ResourceData
{
    public ResourceType Type;
    public int Amount;
    public int MaxAmount = 9999;
    public int ProductionPerSecond = 0;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Resources")]
    public List<ResourceData> Resources = new List<ResourceData>();

    [Header("Population")]
    public int CurrentPopulation { get; private set; }
    public int PopulationCap     { get; private set; } = 5; // base cap before any buildings

    public UnityEvent<ResourceType, int> OnResourceChanged = new UnityEvent<ResourceType, int>();

    private Dictionary<ResourceType, ResourceData> _resourceMap;
    private float _productionTimer = 0f;
    private const float PRODUCTION_INTERVAL = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Resources = new List<ResourceData>
        {
            new ResourceData { Type = ResourceType.Gold,  Amount = 0 },
            new ResourceData { Type = ResourceType.Wood,  Amount = 0 },
            new ResourceData { Type = ResourceType.Stone, Amount = 0 },
            new ResourceData { Type = ResourceType.Food,  Amount = 0 }
        };

        BuildResourceMap();
    }

    void BuildResourceMap()
    {
        _resourceMap = new Dictionary<ResourceType, ResourceData>();
        foreach (var res in Resources)
            _resourceMap[res.Type] = res;
    }

    void Update()
    {
        _productionTimer += Time.deltaTime;
        if (_productionTimer >= PRODUCTION_INTERVAL)
        {
            _productionTimer = 0f;
            ProduceResources();
        }
    }

    void ProduceResources()
    {
        foreach (var res in Resources)
            if (res.ProductionPerSecond > 0)
                AddResource(res.Type, res.ProductionPerSecond);
    }

    // ── Population ────────────────────────────────────────────────────────────

    /// <summary>Returns true if there is room for <paramref name="cost"/> more population units.</summary>
    public bool CanTrainUnit(int cost = 1) => CurrentPopulation + cost <= PopulationCap;

    /// <summary>Reserve population slots when a unit is created.</summary>
    public void ReservePopulation(int amount)
    {
        CurrentPopulation += amount;
    }

    /// <summary>Free population slots when a unit dies.</summary>
    public void FreePopulation(int amount)
    {
        CurrentPopulation = Mathf.Max(0, CurrentPopulation - amount);
    }

    /// <summary>Increase the population cap (e.g., when a House is built).</summary>
    public void AddPopCap(int amount) => PopulationCap += amount;

    /// <summary>Decrease the population cap (e.g., when a House is destroyed).</summary>
    public void RemovePopCap(int amount) => PopulationCap = Mathf.Max(0, PopulationCap - amount);

    // ── Resources ─────────────────────────────────────────────────────────────

    public bool HasResources(ResourceType type, int amount)
    {
        if (!_resourceMap.ContainsKey(type)) return false;
        return _resourceMap[type].Amount >= amount;
    }

    public bool CanAfford(Dictionary<ResourceType, int> costs)
    {
        foreach (var cost in costs)
            if (!HasResources(cost.Key, cost.Value)) return false;
        return true;
    }

    public bool SpendResources(Dictionary<ResourceType, int> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var cost in costs)
            RemoveResource(cost.Key, cost.Value);
        return true;
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (!_resourceMap.ContainsKey(type)) return;
        var res = _resourceMap[type];
        res.Amount = Mathf.Min(res.Amount + amount, res.MaxAmount);
        OnResourceChanged?.Invoke(type, res.Amount);
    }

    public void RemoveResource(ResourceType type, int amount)
    {
        if (!_resourceMap.ContainsKey(type)) return;
        var res = _resourceMap[type];
        res.Amount = Mathf.Max(0, res.Amount - amount);
        OnResourceChanged?.Invoke(type, res.Amount);
    }

    public int GetAmount(ResourceType type)
    {
        if (!_resourceMap.ContainsKey(type)) return 0;
        return _resourceMap[type].Amount;
    }

    public void AddProduction(ResourceType type, int perSecond)
    {
        if (!_resourceMap.ContainsKey(type)) return;
        _resourceMap[type].ProductionPerSecond += perSecond;
    }

    public void RemoveProduction(ResourceType type, int perSecond)
    {
        if (!_resourceMap.ContainsKey(type)) return;
        _resourceMap[type].ProductionPerSecond = Mathf.Max(0, _resourceMap[type].ProductionPerSecond - perSecond);
    }
}
