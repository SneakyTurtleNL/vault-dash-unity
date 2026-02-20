using UnityEngine;

/// <summary>
/// GameManager — Core game state controller for Vault Dash.
/// Handles game flow: menu, playing, paused, game over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    private const string HIGH_SCORE_KEY = "VaultDash_HighScore";

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    void Start()
    {
        Debug.Log("Vault Dash Unity — Ready!");
        SetState(GameState.Menu);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] State: {newState}");

        switch (newState)
        {
            case GameState.Playing:
                Score = 0;
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                if (Score > HighScore)
                {
                    HighScore = Score;
                    PlayerPrefs.SetInt(HIGH_SCORE_KEY, HighScore);
                }
                break;
            case GameState.Menu:
                Time.timeScale = 1f;
                break;
        }
    }

    public void AddScore(int points)
    {
        if (CurrentState != GameState.Playing) return;
        Score += points;
    }

    public void StartGame() => SetState(GameState.Playing);
    public void PauseGame() => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);
    public void GameOver() => SetState(GameState.GameOver);
}
