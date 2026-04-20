using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState CurrentState = GameState.RaceSelection;
    public float GameTime { get; private set; }

    [Header("Player Settings")]
    public string PlayerName = "Player";
    public int PlayerScore = 0;

    // ── End-game statistics ───────────────────────────────────────────────────
    public int BuildingsBuilt    { get; private set; }
    public int UnitsLost         { get; private set; }
    public int EnemiesKilled     { get; private set; }
    public int RelicsDeposited   { get; private set; }

    public void NotifyBuildingBuilt()   => BuildingsBuilt++;
    public void NotifyUnitLost()        => UnitsLost++;
    public void NotifyEnemyKilled()     { EnemiesKilled++; AddScore(10); }
    public void NotifyRelicDeposited()  { RelicsDeposited++; AddScore(200); }

    public RaceData SelectedRace { get; private set; }

    public UnityEvent OnGamePaused   = new UnityEvent();
    public UnityEvent OnGameResumed  = new UnityEvent();
    public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();

    public enum GameState { RaceSelection, Playing, Paused, Victory, Defeat }

    // ── Victory check ─────────────────────────────────────────────────────────
    float _victoryCheckTimer;
    const float VictoryCheckInterval = 3f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            GameTime += Time.deltaTime;

            _victoryCheckTimer -= Time.deltaTime;
            if (_victoryCheckTimer <= 0f)
            {
                _victoryCheckTimer = VictoryCheckInterval;
                CheckVictoryConditions();
            }
        }

        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }
    }

    // ── Game setup ────────────────────────────────────────────────────────────

    public void StartGameWithRace(RaceData race)
    {
        SelectedRace = race;

        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            rm.AddResource(ResourceType.Gold,  race.StartGold);
            rm.AddResource(ResourceType.Wood,  race.StartWood);
            rm.AddResource(ResourceType.Stone, race.StartStone);
            rm.AddResource(ResourceType.Food,  race.StartFood);

            if (race.BonusGoldPerSec  > 0) rm.AddProduction(ResourceType.Gold,  race.BonusGoldPerSec);
            if (race.BonusWoodPerSec  > 0) rm.AddProduction(ResourceType.Wood,  race.BonusWoodPerSec);
            if (race.BonusStonePerSec > 0) rm.AddProduction(ResourceType.Stone, race.BonusStonePerSec);
            if (race.BonusFoodPerSec  > 0) rm.AddProduction(ResourceType.Food,  race.BonusFoodPerSec);
        }

        // Register race-specific Castle unit
        var bm = BuildingManager.Instance;
        if (bm != null)
        {
            var uniqueUnit = bm.GetRaceUniqueUnit(race.Type);
            if (uniqueUnit != null)
                bm.TrainableUnits[BuildingType.Castle] = new System.Collections.Generic.List<UnitData> { uniqueUnit };
        }

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        _victoryCheckTimer = VictoryCheckInterval + 5f; // grace period before first check
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
        else if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    public void AddScore(int amount)
    {
        PlayerScore += amount;
        OnScoreChanged?.Invoke(PlayerScore);
    }

    // ── Victory / Defeat ──────────────────────────────────────────────────────

    void CheckVictoryConditions()
    {
        bool playerAlive = PlayerIsAlive();
        bool aiAlive     = AIController.Instance != null && AIController.Instance.HasBuildings();

        if (!playerAlive)
        {
            TriggerDefeat();
        }
        else if (!aiAlive && AIController.Instance != null)
        {
            // Also wait for all AI units to die before declaring victory
            if (!AIController.Instance.HasUnits())
                TriggerVictory();
        }
    }

    bool PlayerIsAlive()
    {
        // Player is alive if they have any built building OR any villager OR any military unit
        if (Villager.Count > 0) return true;

        foreach (var u in MilitaryUnit.AllUnits)
            if (!u.IsAI && u.IsAlive) return true;

        var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (var b in buildings)
            if (!b.IsAI && b.IsBuilt) return true;

        return false;
    }

    public void TriggerVictory()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Victory;
        Time.timeScale = 0f;
        Debug.Log("[GameManager] VICTORY!");
    }

    public void TriggerDefeat()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Defeat;
        Time.timeScale = 0f;
        Debug.Log("[GameManager] DEFEAT!");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(GameTime / 60f);
        int seconds  = Mathf.FloorToInt(GameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
