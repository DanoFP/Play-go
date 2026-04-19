using UnityEngine;
using System.Collections.Generic;

public enum BuildingType
{
    TownCenter,
    House,
    Farm,
    LumberMill,
    Quarry,
    Market,
    Barracks,
    Tower,
    Wall,
    Temple
}

[CreateAssetMenu(fileName = "BuildingData", menuName = "RealmForge/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public BuildingType Type;
    public string BuildingName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite Icon;

    [Header("Dimensions")]
    public int Width = 2;
    public int Height = 2;

    [Header("Cost")]
    public int GoldCost = 50;
    public int WoodCost = 100;
    public int StoneCost = 0;
    public int FoodCost = 0;

    [Header("Production (per second)")]
    public int GoldProduction = 0;
    public int WoodProduction = 0;
    public int StoneProduction = 0;
    public int FoodProduction = 0;

    [Header("Stats")]
    public int MaxHealth = 200;
    public int PopulationCapacity = 0;
    public int PopulationRequired = 0;

    [Header("Visual")]
    public Color BuildingColor = Color.white;
    public float BuildTime = 3f;

    public Dictionary<ResourceType, int> GetCostDict()
    {
        var dict = new Dictionary<ResourceType, int>();
        if (GoldCost > 0) dict[ResourceType.Gold] = GoldCost;
        if (WoodCost > 0) dict[ResourceType.Wood] = WoodCost;
        if (StoneCost > 0) dict[ResourceType.Stone] = StoneCost;
        if (FoodCost > 0) dict[ResourceType.Food] = FoodCost;
        return dict;
    }
}
