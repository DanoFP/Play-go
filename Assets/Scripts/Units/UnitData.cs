using UnityEngine;

public enum UnitType { Militia, Spearman, Archer, Skirmisher, Scout, Knight, Monk, BatteringRam, Mangonel }
public enum DamageType { Melee, Pierce, Siege }

[CreateAssetMenu(fileName = "UnitData", menuName = "RealmForge/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string UnitName;
    public UnitType Type;
    [Range(1, 4)]
    public int MinAge = 1;

    [Header("Cost")]
    public int GoldCost;
    public int FoodCost;
    public int WoodCost;
    public int PopulationCost = 1;
    public float TrainTime = 20f;

    [Header("Combat")]
    public float MaxHP       = 45f;
    public float Attack      = 4f;
    public float AttackRange = 1.5f;   // ≤2 = melee, >2 = ranged
    public float AttackSpeed = 1f;     // attacks per second
    public DamageType DamageType = DamageType.Melee;
    public float MeleeArmor  = 0f;
    public float PierceArmor = 0f;

    [Header("Movement")]
    public float MoveSpeed    = 4f;
    public float LineOfSight  = 8f;

    [Header("Visual")]
    public Color UnitColor = new Color(0.6f, 0.6f, 0.65f);

    [Header("Training")]
    public BuildingType TrainingBuilding;

    // ── Runtime factory (no asset files needed) ───────────────────────────────

    public static UnitData Create(string name, UnitType type, BuildingType trainingBuilding,
        int goldCost, int foodCost, int woodCost, int popCost, float trainTime,
        float hp, float atk, float range, float speed, DamageType dmgType,
        float meleeArmor, float pierceArmor, float los, Color color, int minAge = 1)
    {
        var d = ScriptableObject.CreateInstance<UnitData>();
        d.UnitName          = name;
        d.Type              = type;
        d.MinAge            = Mathf.Clamp(minAge, 1, 4);
        d.TrainingBuilding  = trainingBuilding;
        d.GoldCost          = goldCost;
        d.FoodCost          = foodCost;
        d.WoodCost          = woodCost;
        d.PopulationCost    = popCost;
        d.TrainTime         = trainTime;
        d.MaxHP             = hp;
        d.Attack            = atk;
        d.AttackRange       = range;
        d.AttackSpeed       = speed;
        d.DamageType        = dmgType;
        d.MeleeArmor        = meleeArmor;
        d.PierceArmor       = pierceArmor;
        d.LineOfSight       = los;
        d.UnitColor         = color;
        return d;
    }
}
