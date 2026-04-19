using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState CurrentState = GameState.Playing;
    public float GameTime { get; private set; }

    [Header("Player Settings")]
    public string PlayerName = "Dano";
    public int PlayerScore = 0;

    public UnityEvent OnGamePaused = new UnityEvent();
    public UnityEvent OnGameResumed = new UnityEvent();
    public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();

    public enum GameState { Playing, Paused, Victory, Defeat }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Single-scene game — no DontDestroyOnLoad needed
    }

    void Start()
    {
        SetupInitialScene();
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            GameTime += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void SetupInitialScene()
    {
        // Start with some initial resources
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(ResourceType.Gold, 200);
            ResourceManager.Instance.AddResource(ResourceType.Wood, 300);
            ResourceManager.Instance.AddResource(ResourceType.Stone, 150);
            ResourceManager.Instance.AddResource(ResourceType.Food, 100);
        }
    }

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

    public void AddScore(int amount)
    {
        PlayerScore += amount;
        OnScoreChanged?.Invoke(PlayerScore);
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(GameTime / 60f);
        int seconds = Mathf.FloorToInt(GameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
