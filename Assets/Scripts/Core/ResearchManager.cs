using UnityEngine;
using System.Collections.Generic;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    public bool IsResearching => _current != null;
    public float CurrentProgress => _current != null && _current.Duration > 0f ? _current.Timer / _current.Duration : 0f;
    public ResearchData CurrentResearch => _current?.Data;
    public Building CurrentResearchBuilding => _current?.Building;

    class ActiveResearch
    {
        public ResearchData Data;
        public Building Building;
        public float Timer;
        public float Duration;
    }

    readonly Dictionary<string, ResearchData> _all = new Dictionary<string, ResearchData>();
    readonly HashSet<string> _completed = new HashSet<string>();
    ActiveResearch _current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var r in ResearchCatalog.CreateDefault())
            _all[r.Id] = r;
    }

    void Update()
    {
        if (_current == null) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        _current.Timer += Time.deltaTime;
        if (_current.Timer >= _current.Duration)
            CompleteResearch();
    }

    public bool HasResearchForBuilding(BuildingType type)
    {
        foreach (var r in _all.Values)
            if (r.RequiredBuilding == type)
                return true;
        return false;
    }

    public List<ResearchData> GetAvailableResearch(Building building)
    {
        var list = new List<ResearchData>();
        if (building == null || building.Data == null) return list;

        foreach (var r in _all.Values)
        {
            if (r.RequiredBuilding != building.Data.Type) continue;
            if (IsResearched(r.Id)) continue;
            if (AgeManager.Instance != null && (int)AgeManager.Instance.CurrentAge < Mathf.Max(1, r.MinAge)) continue;
            if (!string.IsNullOrEmpty(r.PrerequisiteId) && !IsResearched(r.PrerequisiteId)) continue;
            list.Add(r);
        }

        return list;
    }

    public bool IsResearched(string id) => !string.IsNullOrEmpty(id) && _completed.Contains(id);

    public bool CanStartResearch(ResearchData data, Building building, out string reason)
    {
        reason = string.Empty;

        if (data == null) { reason = "Invalid research"; return false; }
        if (building == null || building.Data == null || !building.IsBuilt) { reason = "Select a completed building"; return false; }
        if (building.IsAI) { reason = "Player research only"; return false; }
        if (building.Data.Type != data.RequiredBuilding) { reason = "Requires " + data.RequiredBuilding; return false; }
        if (IsResearching) { reason = "Another research is in progress"; return false; }
        if (IsResearched(data.Id)) { reason = "Already researched"; return false; }

        if (AgeManager.Instance != null && (int)AgeManager.Instance.CurrentAge < Mathf.Max(1, data.MinAge))
        {
            reason = "Requires " + AgeManager.GetAgeLabel(data.MinAge);
            return false;
        }

        if (!string.IsNullOrEmpty(data.PrerequisiteId) && !IsResearched(data.PrerequisiteId))
        {
            reason = "Missing prerequisite";
            return false;
        }

        var rm = ResourceManager.Instance;
        if (rm == null || !rm.CanAfford(data.GetCostDict()))
        {
            reason = "Not enough resources";
            return false;
        }

        return true;
    }

    public bool StartResearch(ResearchData data, Building building)
    {
        if (!CanStartResearch(data, building, out var reason))
        {
            UIManager.Instance?.ShowMessage(reason);
            return false;
        }

        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            if (data.GoldCost > 0) rm.RemoveResource(ResourceType.Gold, data.GoldCost);
            if (data.FoodCost > 0) rm.RemoveResource(ResourceType.Food, data.FoodCost);
            if (data.WoodCost > 0) rm.RemoveResource(ResourceType.Wood, data.WoodCost);
        }

        _current = new ActiveResearch
        {
            Data = data,
            Building = building,
            Timer = 0f,
            Duration = Mathf.Max(1f, data.ResearchTime)
        };

        UIManager.Instance?.ShowMessage("Researching " + data.Name + "...");
        return true;
    }

    void CompleteResearch()
    {
        if (_current == null) return;

        var data = _current.Data;
        _completed.Add(data.Id);

        if      (data.Id == "horse_collar")    ApplyHorseCollarToExistingFarms();
        else if (data.Id == "town_watch")      RevealAroundPlayerBuildings();
        else if (data.Id == "fortified_wall")  ApplyFortifiedWallToExistingWalls();
        else if (data.Id == "masonry")         ApplyMasonryToExistingBuildings();

        UIManager.Instance?.ShowMessage(data.Name + " completed!");
        _current = null;
    }

    void ApplyMasonryToExistingBuildings()
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b == null || b.IsAI || !b.IsBuilt || b.Data == null) continue;
            int bonus = Mathf.Max(1, Mathf.RoundToInt(b.Data.MaxHealth * 0.1f));
            b.AddHealth(bonus);
        }
    }

    void ApplyFortifiedWallToExistingWalls()
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b == null || b.IsAI || !b.IsBuilt || b.Data == null) continue;
            if (b.Data.Type != BuildingType.Wall) continue;
            b.AddHealth(500);
        }
    }

    void ApplyHorseCollarToExistingFarms()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b == null || b.IsAI || !b.IsBuilt || b.Data == null) continue;
            if (b.Data.Type != BuildingType.Farm || b.Data.FoodProduction <= 0) continue;

            int extra = Mathf.Max(1, Mathf.RoundToInt(b.Data.FoodProduction * 0.15f));
            rm.AddProduction(ResourceType.Food, extra);
        }
    }

    void RevealAroundPlayerBuildings()
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b == null || b.IsAI || !b.IsBuilt) continue;
            FogOfWar.Instance?.Reveal(b.transform.position, 18f);
        }
    }

    public float GetVillagerGatherMultiplier(ResourceType type)
    {
        float m = 1f;
        if (type == ResourceType.Wood && IsResearched("double_bit_axe")) m += 0.15f;
        return m;
    }

    public float GetFarmFoodMultiplier()
    {
        float m = 1f;
        if (IsResearched("horse_collar")) m += 0.15f;
        return m;
    }

    public float GetVillagerLoSBonus() => IsResearched("town_watch") ? 4f : 0f;
    public int GetVillagerHealthBonus() => IsResearched("loom") ? 15 : 0;
    public int GetVillagerArmorBonus() => IsResearched("loom") ? 1 : 0;
    public float GetBuildingLoSBonus() => IsResearched("town_watch") ? 4f : 0f;

    public float GetAttackBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        float bonus = 0f;
        if (IsResearched("fletching")    && IsArcher(unit.Type))  bonus += 1f;
        if (IsResearched("bodkin_arrow") && IsArcher(unit.Type))  bonus += 1f;
        if (IsResearched("engineering")  && IsSiege(unit.Type))   bonus += 1f;
        return bonus;
    }

    public float GetRangeBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        if (IsResearched("fletching") && IsArcher(unit.Type)) return 1f;
        return 0f;
    }

    public float GetMeleeArmorBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        float bonus = 0f;
        if (IsResearched("scale_mail") && IsInfantry(unit.Type)) bonus += 1f;
        if (IsResearched("chain_mail") && IsInfantry(unit.Type)) bonus += 1f;
        return bonus;
    }

    public float GetAttackSpeedBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        if (IsResearched("ballistics") && IsArcher(unit.Type)) return 0.25f;
        return 0f;
    }

    public float GetPierceArmorBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        float bonus = 0f;
        if (IsResearched("chain_mail")    && IsInfantry(unit.Type)) bonus += 1f;
        if (IsResearched("bodkin_arrow")  && IsArcher(unit.Type))   bonus += 1f;
        return bonus;
    }

    public float GetSiegeAttackBonus(UnitData unit)
    {
        if (unit == null) return 0f;
        if (IsResearched("engineering") && IsSiege(unit.Type)) return 1f;
        return 0f;
    }

    public bool  HasMasonry()          => IsResearched("masonry");
    public float GetTowerAttackBonus() => IsResearched("guard_tower") ? 3f : 0f;
    public float GetTowerRangeBonus()  => IsResearched("guard_tower") ? 2f : 0f;

    public bool HasFortifiedWall() => IsResearched("fortified_wall");
    public int  GetWallHealthBonus() => IsResearched("fortified_wall") ? 500 : 0;

    // Predicate helpers
    bool IsArcher(UnitType type) => type == UnitType.Archer || type == UnitType.Skirmisher;

    bool IsInfantry(UnitType type) =>
        type == UnitType.Militia || type == UnitType.Spearman;

    bool IsSiege(UnitType type) =>
        type == UnitType.BatteringRam || type == UnitType.Mangonel || type == UnitType.Trebuchet;

    bool IsCavalry(UnitType type) =>
        type == UnitType.Scout || type == UnitType.Knight;
}
