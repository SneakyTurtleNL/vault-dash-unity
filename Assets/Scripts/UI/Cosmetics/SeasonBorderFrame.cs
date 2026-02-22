using UnityEngine;
using UnityEngine.UI;

namespace VaultDash.UI.Cosmetics
{
    /// <summary>
    /// SEASON BORDER FRAME
    /// Decorative frame around UI elements (cards, profiles, leaderboard)
    /// Changes per season for visual theming
    /// </summary>
    public class SeasonBorderFrame : MonoBehaviour
    {
        public enum SeasonTier { Bronze, Silver, Gold, Diamond, Legend }

        [SerializeField] private Image borderImage;
        [SerializeField] private Image glowRing;
        [SerializeField] private ParticleSystem borderParticles;

        private SeasonTier currentTier;
        private float glowPulseSpeed = 1.5f;

        private void Start()
        {
            if (borderImage == null)
                borderImage = GetComponent<Image>();
        }

        public void SetSeasonTier(SeasonTier tier)
        {
            currentTier = tier;
            ApplyTierStyling(tier);
        }

        private void ApplyTierStyling(SeasonTier tier)
        {
            switch (tier)
            {
                case SeasonTier.Bronze:
                    borderImage.color = new Color(0.8f, 0.5f, 0.2f);  // Bronze
                    glowRing.color = new Color(0.8f, 0.5f, 0.2f, 0.3f);
                    break;
                case SeasonTier.Silver:
                    borderImage.color = new Color(0.75f, 0.75f, 0.75f);  // Silver
                    glowRing.color = new Color(0.75f, 0.75f, 0.75f, 0.3f);
                    break;
                case SeasonTier.Gold:
                    borderImage.color = new Color(1f, 0.84f, 0f);  // Gold
                    glowRing.color = new Color(1f, 0.84f, 0f, 0.4f);
                    break;
                case SeasonTier.Diamond:
                    borderImage.color = new Color(0.2f, 0.9f, 1f);  // Diamond
                    glowRing.color = new Color(0.2f, 0.9f, 1f, 0.5f);
                    break;
                case SeasonTier.Legend:
                    borderImage.color = new Color(1f, 0.2f, 0.5f);  // Legendary Pink
                    glowRing.color = new Color(1f, 0.2f, 0.5f, 0.6f);
                    break;
            }

            // Play border particles
            if (borderParticles != null)
                borderParticles.Play();
        }

        private void Update()
        {
            // Pulse glow effect
            if (glowRing != null)
            {
                float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
                var color = glowRing.color;
                color.a = pulse * 0.6f;
                glowRing.color = color;
            }
        }
    }
}
