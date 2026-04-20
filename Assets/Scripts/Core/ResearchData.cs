using System.Collections.Generic;

public enum ResearchEffectType
{
    AttackBonus,
    RangeBonus,
    MeleeArmorBonus,
    PierceArmorBonus,
    GatherRate,
    FarmFoodRate,
    HealthBonus,
    LineOfSightBonus
}

public enum ResearchTarget
{
    None,
    Villager,
    Archer,
    Infantry,
    Wood,
    Food,
    Building
}

[System.Serializable]
public class ResearchEffect
{
    public ResearchEffectType Type;
    public float Value;
    public ResearchTarget Target;

    public ResearchEffect(ResearchEffectType type, float value, ResearchTarget target = ResearchTarget.None)
    {
        Type = type;
        Value = value;
        Target = target;
    }
}

[System.Serializable]
public class ResearchData
{
    public string Id;
    public string Name;
    public string Description;
    public BuildingType RequiredBuilding;
    public int MinAge = 1;
    public int GoldCost;
    public int FoodCost;
    public int WoodCost;
    public float ResearchTime;
    public string PrerequisiteId;
    public List<ResearchEffect> Effects = new List<ResearchEffect>();

    public Dictionary<ResourceType, int> GetCostDict()
    {
        var dict = new Dictionary<ResourceType, int>();
        if (GoldCost > 0) dict[ResourceType.Gold] = GoldCost;
        if (FoodCost > 0) dict[ResourceType.Food] = FoodCost;
        if (WoodCost > 0) dict[ResourceType.Wood] = WoodCost;
        return dict;
    }
}

public static class ResearchCatalog
{
    public static List<ResearchData> CreateDefault()
    {
        return new List<ResearchData>
        {
            new ResearchData
            {
                Id = "loom",
                Name = "Loom",
                Description = "Villagers gain +15 HP and +1 armor.",
                RequiredBuilding = BuildingType.TownCenter,
                MinAge = 1,
                GoldCost = 50,
                ResearchTime = 25f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.HealthBonus, 15f, ResearchTarget.Villager),
                    new ResearchEffect(ResearchEffectType.MeleeArmorBonus, 1f, ResearchTarget.Villager),
                }
            },
            new ResearchData
            {
                Id = "double_bit_axe",
                Name = "Double Bit Axe",
                Description = "Wood gather rate +15%.",
                RequiredBuilding = BuildingType.LumberCamp,
                MinAge = 1,
                FoodCost = 100,
                ResearchTime = 25f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.GatherRate, 0.15f, ResearchTarget.Wood),
                }
            },
            new ResearchData
            {
                Id = "horse_collar",
                Name = "Horse Collar",
                Description = "Farm food production +15%.",
                RequiredBuilding = BuildingType.Mill,
                MinAge = 1,
                FoodCost = 75,
                ResearchTime = 20f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.FarmFoodRate, 0.15f, ResearchTarget.Food),
                }
            },
            new ResearchData
            {
                Id = "fletching",
                Name = "Fletching",
                Description = "Archers gain +1 attack and +1 range.",
                RequiredBuilding = BuildingType.Blacksmith,
                MinAge = 2,
                FoodCost = 100,
                GoldCost = 75,
                ResearchTime = 30f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.AttackBonus, 1f, ResearchTarget.Archer),
                    new ResearchEffect(ResearchEffectType.RangeBonus, 1f, ResearchTarget.Archer),
                }
            },
            new ResearchData
            {
                Id = "scale_mail",
                Name = "Scale Mail",
                Description = "Infantry gain +1 melee armor.",
                RequiredBuilding = BuildingType.Blacksmith,
                MinAge = 2,
                FoodCost = 100,
                ResearchTime = 40f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.MeleeArmorBonus, 1f, ResearchTarget.Infantry),
                }
            },
            new ResearchData
            {
                Id = "town_watch",
                Name = "Town Watch",
                Description = "Villagers and buildings gain +4 line of sight.",
                RequiredBuilding = BuildingType.TownCenter,
                MinAge = 1,
                FoodCost = 75,
                ResearchTime = 25f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.LineOfSightBonus, 4f, ResearchTarget.Villager),
                    new ResearchEffect(ResearchEffectType.LineOfSightBonus, 4f, ResearchTarget.Building),
                }
            },

            // ── University ────────────────────────────────────────────────────
            new ResearchData
            {
                Id = "masonry",
                Name = "Masonry",
                Description = "All buildings gain +10% HP. Retroactive on existing structures.",
                RequiredBuilding = BuildingType.University,
                MinAge = 3,
                FoodCost = 175,
                WoodCost = 175,
                ResearchTime = 50f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.HealthBonus, 0.1f, ResearchTarget.Building),
                }
            },
            new ResearchData
            {
                Id = "guard_tower",
                Name = "Guard Tower",
                Description = "Watch Towers gain +3 attack and +2 range.",
                RequiredBuilding = BuildingType.University,
                MinAge = 2,
                FoodCost = 150,
                GoldCost = 50,
                ResearchTime = 35f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.AttackBonus, 3f, ResearchTarget.Building),
                    new ResearchEffect(ResearchEffectType.RangeBonus, 2f, ResearchTarget.Building),
                }
            },
            new ResearchData
            {
                Id = "fortified_wall",
                Name = "Fortified Wall",
                Description = "Walls gain +500 HP. Harder to breach.",
                RequiredBuilding = BuildingType.University,
                MinAge = 3,
                GoldCost = 200,
                WoodCost = 100,
                ResearchTime = 50f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.HealthBonus, 500f, ResearchTarget.Building),
                }
            },
            new ResearchData
            {
                Id = "engineering",
                Name = "Engineering",
                Description = "Siege units gain +1 attack and move 10% faster.",
                RequiredBuilding = BuildingType.University,
                MinAge = 3,
                FoodCost = 200,
                WoodCost = 200,
                ResearchTime = 55f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.AttackBonus, 1f, ResearchTarget.None),
                }
            },
            new ResearchData
            {
                Id = "ballistics",
                Name = "Ballistics",
                Description = "Archers and Skirmishers attack 25% faster.",
                RequiredBuilding = BuildingType.University,
                MinAge = 2,
                FoodCost = 100,
                GoldCost = 75,
                ResearchTime = 35f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.AttackBonus, 0.25f, ResearchTarget.Archer),
                }
            },

            // ── Monastery ─────────────────────────────────────────────────────
            new ResearchData
            {
                Id = "sanctity",
                Name = "Sanctity",
                Description = "Monks gain +15 HP. More durable in the field.",
                RequiredBuilding = BuildingType.Monastery,
                MinAge = 3,
                GoldCost = 120,
                ResearchTime = 45f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.HealthBonus, 15f, ResearchTarget.None),
                }
            },
            new ResearchData
            {
                Id = "fervor",
                Name = "Fervor",
                Description = "Monks move 15% faster.",
                RequiredBuilding = BuildingType.Monastery,
                MinAge = 3,
                FoodCost = 140,
                ResearchTime = 50f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.LineOfSightBonus, 2f, ResearchTarget.None),
                }
            },

            // ── Blacksmith upgrades (Age 3) ───────────────────────────────────
            new ResearchData
            {
                Id = "chain_mail",
                Name = "Chain Mail",
                Description = "Infantry gain +1 melee armor and +1 pierce armor.",
                RequiredBuilding = BuildingType.Blacksmith,
                MinAge = 3,
                PrerequisiteId = "scale_mail",
                FoodCost = 200,
                GoldCost = 100,
                ResearchTime = 40f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.MeleeArmorBonus, 1f, ResearchTarget.Infantry),
                    new ResearchEffect(ResearchEffectType.PierceArmorBonus, 1f, ResearchTarget.Infantry),
                }
            },
            new ResearchData
            {
                Id = "bodkin_arrow",
                Name = "Bodkin Arrow",
                Description = "Archers gain +1 attack and +1 pierce armor.",
                RequiredBuilding = BuildingType.Blacksmith,
                MinAge = 3,
                PrerequisiteId = "fletching",
                FoodCost = 150,
                GoldCost = 100,
                ResearchTime = 35f,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect(ResearchEffectType.AttackBonus, 1f, ResearchTarget.Archer),
                    new ResearchEffect(ResearchEffectType.PierceArmorBonus, 1f, ResearchTarget.Archer),
                }
            }
        };
    }
}
