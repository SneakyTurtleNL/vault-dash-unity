using UnityEngine;

namespace VaultDash.UI.Cosmetics
{
    /// <summary>
    /// UI COSMETICS SYSTEM
    /// Central orchestrator for all UI cosmetic effects
    /// Manages season frames, particle effects, glow effects
    /// </summary>
    public class UICosmeticsSystem : MonoBehaviour
    {
        public static UICosmeticsSystem Instance { get; private set; }

        [SerializeField] private SeasonBorderFrame seasonBorderFrame;
        [SerializeField] private TrophySparkleEffect trophySparkle;
        [SerializeField] private GemShineEffect gemShine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetSeasonTheme(SeasonBorderFrame.SeasonTier tier)
        {
            if (seasonBorderFrame != null)
                seasonBorderFrame.SetSeasonTier(tier);
        }

        public void TriggerTrophyGain(int amount)
        {
            if (trophySparkle != null)
                trophySparkle.PlaySparkle(amount);
        }

        public void TriggerGemEarned(int amount)
        {
            if (gemShine != null)
                gemShine.PlayGemEarned(amount);
        }

        public void TriggerGemSpent(int amount)
        {
            if (gemShine != null)
                gemShine.PlayGemSpent(amount);
        }

        public void ApplyButtonHoverGlowToAll(Color glowColor)
        {
            var buttons = FindObjectsOfType<ButtonHoverGlow>();
            foreach (var btn in buttons)
            {
                btn.SetGlowColor(glowColor);
            }
        }
    }
}
