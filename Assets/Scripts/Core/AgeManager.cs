using UnityEngine;
using System.Collections.Generic;

public enum Age
{
    DarkAge = 1,
    FeudalAge = 2,
    CastleAge = 3,
    ImperialAge = 4
}

/// <summary>
/// Handles age progression and unlock gating.
/// Current implementation keeps requirements aligned with currently available content.
/// </summary>
public class AgeManager : MonoBehaviour
{
    public static AgeManager Instance { get; private set; }

    public Age CurrentAge { get; private set; } = Age.DarkAge;
    public bool IsAdvancing { get; private set; }
    public float AdvanceProgress { get; private set; }

    float _advanceTimer;
    float _advanceDuration;
    Age _targetAge;

    static readonly int[] FoodCost = { 0, 0, 500, 800, 1000 };
    static readonly int[] GoldCost = { 0, 0, 500, 800, 1000 };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsAdvancing) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        _advanceTimer += Time.deltaTime;
        AdvanceProgress = Mathf.Clamp01(_advanceTimer / _advanceDuration);
        if (_advanceTimer >= _advanceDuration)
            CompleteAdvance();
    }

    public bool CanBuild(BuildingData data)
    {
        if (data == null) return false;
        return (int)CurrentAge >= Mathf.Max(1, data.MinAge);
    }

    public bool CanTrain(UnitData data)
    {
        if (data == null) return false;
        return (int)CurrentAge >= Mathf.Max(1, data.MinAge);
    }

    public bool CanAdvance(out string reason)
    {
        reason = string.Empty;
        if (IsAdvancing) { reason = "Already advancing"; return false; }
        if (CurrentAge == Age.ImperialAge) { reason = "Already in Imperial Age"; return false; }

        int next = (int)CurrentAge + 1;
        var rm = ResourceManager.Instance;
        if (rm == null) { reason = "No resource manager"; return false; }

        if (!rm.HasResources(ResourceType.Food, FoodCost[next]) || !rm.HasResources(ResourceType.Gold, GoldCost[next]))
        {
            reason = $"Need {FoodCost[next]}F and {GoldCost[next]}G";
            return false;
        }

        if (!MeetsRequirements((Age)next, out reason))
            return false;

        return true;
    }

    public bool StartAdvance()
    {
        if (!CanAdvance(out var reason))
        {
            UIManager.Instance?.ShowMessage(reason);
            return false;
        }

        int next = (int)CurrentAge + 1;
        var rm = ResourceManager.Instance;
        rm?.RemoveResource(ResourceType.Food, FoodCost[next]);
        rm?.RemoveResource(ResourceType.Gold, GoldCost[next]);

        IsAdvancing = true;
        _targetAge = (Age)next;
        _advanceTimer = 0f;
        _advanceDuration = next == 2 ? 60f : next == 3 ? 80f : 100f;
        AdvanceProgress = 0f;
        UIManager.Instance?.ShowMessage("Advancing to " + GetAgeLabel(_targetAge) + "...");
        return true;
    }

    void CompleteAdvance()
    {
        CurrentAge = _targetAge;
        IsAdvancing = false;
        _advanceTimer = 0f;
        _advanceDuration = 0f;
        AdvanceProgress = 0f;

        UIManager.Instance?.ShowMessage("You have advanced to the " + GetAgeLabel(CurrentAge) + "!");
    }

    bool MeetsRequirements(Age targetAge, out string reason)
    {
        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        var types = new HashSet<BuildingType>();
        foreach (var b in buildings)
        {
            if (b == null || b.IsAI || !b.IsBuilt || b.Data == null) continue;
            types.Add(b.Data.Type);
        }

        reason = string.Empty;

        if (targetAge == Age.FeudalAge)
        {
            if (types.Count < 2)
            {
                reason = "Need 2 different buildings built";
                return false;
            }
            return true;
        }

        if (targetAge == Age.CastleAge)
        {
            if (!types.Contains(BuildingType.Barracks) || !types.Contains(BuildingType.ArcheryRange) || !types.Contains(BuildingType.Blacksmith))
            {
                reason = "Need Barracks, Archery Range and Blacksmith";
                return false;
            }
            return true;
        }

        if (targetAge == Age.ImperialAge)
        {
            if (!types.Contains(BuildingType.University) || !types.Contains(BuildingType.SiegeWorkshop) || !types.Contains(BuildingType.Monastery))
            {
                reason = "Need University, Siege Workshop and Monastery";
                return false;
            }
            return true;
        }

        return true;
    }

    public static string GetAgeLabel(Age age)
    {
        return age == Age.DarkAge ? "Dark Age" :
               age == Age.FeudalAge ? "Feudal Age" :
               age == Age.CastleAge ? "Castle Age" :
               "Imperial Age";
    }

    public static string GetAgeLabel(int age)
    {
        return GetAgeLabel((Age)Mathf.Clamp(age, 1, 4));
    }
}