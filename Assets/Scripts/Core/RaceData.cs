using UnityEngine;

public enum RaceType { Humans, Elves, Dwarves, Orcs }

public class RaceData
{
    public RaceType Type;
    public string Name;
    public string Title;
    public string Description;
    public string[] Bonuses;
    public Color PrimaryColor;
    public Color AccentColor;

    // Starting resources (override defaults)
    public int StartGold;
    public int StartWood;
    public int StartStone;
    public int StartFood;

    // Flat passive production bonuses per second (added globally on game start)
    public int BonusGoldPerSec;
    public int BonusWoodPerSec;
    public int BonusStonePerSec;
    public int BonusFoodPerSec;

    // Building health multiplier applied when a building finishes construction
    public float BuildingHPMultiplier;

    // ── Factory ──────────────────────────────────────────────────────────────

    public static RaceData[] All() => new RaceData[]
    {
        new RaceData
        {
            Type              = RaceType.Humans,
            Name              = "Humans",
            Title             = "Terran Alliance",
            Description       = "Versatile builders and diplomats. Masters of trade, they adapt to any challenge through ingenuity and cooperation.",
            Bonuses           = new[] { "+2 Gold/s (passive)", "Markets: +2 Gold/s extra", "Build time -20%" },
            PrimaryColor      = new Color(0.90f, 0.78f, 0.35f),
            AccentColor       = new Color(0.95f, 0.95f, 0.75f),
            StartGold  = 300, StartWood = 200, StartStone = 100, StartFood = 100,
            BonusGoldPerSec   = 2,
            BuildingHPMultiplier = 1.0f,
        },
        new RaceData
        {
            Type              = RaceType.Elves,
            Name              = "Elves",
            Title             = "Sylvan Covenant",
            Description       = "Ancient guardians of the forest. Their bond with nature grants unparalleled mastery over wood and food production.",
            Bonuses           = new[] { "+3 Wood/s (passive)", "+2 Food/s (passive)", "Forest structures cost -25% wood" },
            PrimaryColor      = new Color(0.25f, 0.72f, 0.30f),
            AccentColor       = new Color(0.70f, 0.95f, 0.65f),
            StartGold  = 100, StartWood = 500, StartStone = 50, StartFood = 200,
            BonusWoodPerSec   = 3,
            BonusFoodPerSec   = 2,
            BuildingHPMultiplier = 0.85f,
        },
        new RaceData
        {
            Type              = RaceType.Dwarves,
            Name              = "Dwarves",
            Title             = "Iron Clans",
            Description       = "Legendary craftsmen of the deep mountains. Their structures stand as eternal monuments of stone and steel.",
            Bonuses           = new[] { "+3 Stone/s (passive)", "Buildings +50% HP", "Towers cost -30% stone" },
            PrimaryColor      = new Color(0.65f, 0.55f, 0.40f),
            AccentColor       = new Color(0.90f, 0.82f, 0.68f),
            StartGold  = 200, StartWood = 100, StartStone = 400, StartFood = 100,
            BonusStonePerSec  = 3,
            BuildingHPMultiplier = 1.5f,
        },
        new RaceData
        {
            Type              = RaceType.Orcs,
            Name              = "Orcs",
            Title             = "Bloodpeak Horde",
            Description       = "Fearsome warriors born of the untamed wilds. Their ferocity and hunger for conquest drives rapid expansion.",
            Bonuses           = new[] { "+3 Food/s (passive)", "+1 Wood/s (passive)", "Towers & walls -30% cost" },
            PrimaryColor      = new Color(0.55f, 0.78f, 0.25f),
            AccentColor       = new Color(0.75f, 0.95f, 0.45f),
            StartGold  = 100, StartWood = 300, StartStone = 100, StartFood = 400,
            BonusFoodPerSec   = 3,
            BonusWoodPerSec   = 1,
            BuildingHPMultiplier = 1.2f,
        },
    };
}
