using UnityEngine;
using UnityEngine.UI;

namespace VaultDash.Gameplay
{
    /// <summary>
    /// PARALLAX BACKGROUND CONTROLLER
    /// 3-layer parallax scrolling system for arena environments
    /// Depth simulation via speed variation
    /// </summary>
    public class ParallaxBackgroundController : MonoBehaviour
    {
        [SerializeField] private Image backgroundLayer1;  // Far (slowest)
        [SerializeField] private Image backgroundLayer2;  // Mid
        [SerializeField] private Image backgroundLayer3;  // Near (fastest)

        [SerializeField] private float layer1Speed = 0.3f;
        [SerializeField] private float layer2Speed = 0.6f;
        [SerializeField] private float layer3Speed = 0.9f;

        [SerializeField] private Vector2 layer1Scale = Vector2.one;
        [SerializeField] private Vector2 layer2Scale = Vector2.one;
        [SerializeField] private Vector2 layer3Scale = Vector2.one;

        private float scrollPosition = 0f;
        private float totalDistance = 0f;

        private void Update()
        {
            UpdateScrollPosition();
        }

        private void UpdateScrollPosition()
        {
            // Calculate scroll based on game progress or camera position
            // For tunnel runner: based on game distance traveled
            
            if (backgroundLayer1 != null)
                ApplyLayerScroll(backgroundLayer1, layer1Speed, layer1Scale);
            if (backgroundLayer2 != null)
                ApplyLayerScroll(backgroundLayer2, layer2Speed, layer2Scale);
            if (backgroundLayer3 != null)
                ApplyLayerScroll(backgroundLayer3, layer3Speed, layer3Scale);
        }

        private void ApplyLayerScroll(Image layer, float speed, Vector2 scale)
        {
            // Update UV offset for infinite scrolling
            Material mat = layer.material;
            if (mat == null) return;

            float scrollAmount = scrollPosition * speed;
            Vector2 offset = new Vector2(
                scrollAmount % scale.x,
                scrollAmount % scale.y
            );

            mat.SetTextureOffset("_MainTex", offset);
        }

        public void UpdateTravelDistance(float distance)
        {
            totalDistance = distance;
            scrollPosition = distance / 100f;  // Normalize distance to scroll amount
        }

        public void SetArenaTheme(ArenaTheme theme)
        {
            // Apply theme-specific color grading / overlay
            ApplyThemeOverlay(theme);
        }

        private void ApplyThemeOverlay(ArenaTheme theme)
        {
            Color themeColor = GetThemeColor(theme);
            
            if (backgroundLayer1 != null)
                backgroundLayer1.color = Color.Lerp(Color.white, themeColor, 0.3f);
            if (backgroundLayer2 != null)
                backgroundLayer2.color = Color.Lerp(Color.white, themeColor, 0.5f);
            if (backgroundLayer3 != null)
                backgroundLayer3.color = Color.Lerp(Color.white, themeColor, 0.2f);
        }

        private Color GetThemeColor(ArenaTheme theme) =>
            theme switch
            {
                ArenaTheme.Rookie => new Color(0.8f, 0.7f, 0.6f),      // Bronze/gold tint
                ArenaTheme.Silver => new Color(0.6f, 0.8f, 0.9f),      // Cyan tint
                ArenaTheme.Gold => new Color(0.9f, 0.8f, 0.5f),        // Warm gold
                ArenaTheme.Diamond => new Color(0.4f, 0.6f, 0.9f),     // Cool blue
                ArenaTheme.Legend => new Color(0.7f, 0.5f, 1f),        // Purple cosmic
                _ => Color.white
            };
    }

    public enum ArenaTheme
    {
        Rookie = 0,
        Silver = 1,
        Gold = 2,
        Diamond = 3,
        Legend = 4
    }
}
