using UnityEngine;
using TMPro;

/// <summary>
/// ScoreManager — Displays and tracks score UI.
/// Attach to a UI Canvas object.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    void Update()
    {
        if (GameManager.Instance == null) return;
        
        if (scoreText != null)
            scoreText.text = $"Score: {GameManager.Instance.Score}";
        
        if (highScoreText != null)
            highScoreText.text = $"Best: {GameManager.Instance.HighScore}";
    }
}
