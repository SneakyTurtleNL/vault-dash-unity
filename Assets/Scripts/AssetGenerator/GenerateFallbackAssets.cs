using UnityEngine;
using System.Collections.Generic;

namespace VaultDash.Assets
{
    /// <summary>
    /// FALLBACK ASSET GENERATOR
    /// Creates placeholder sprites programmatically until Scenario.gg assets arrive
    /// Saturday interim solution - replaced by real assets Week 5
    /// </summary>
    public class GenerateFallbackAssets : MonoBehaviour
    {
        public static void GenerateCharacterSprites()
        {
            // Characters with assigned colors (Clash Royale style)
            Dictionary<string, Color> characters = new()
            {
                { "agent_zero", new Color(0.2f, 0.4f, 0.8f) },    // Blue
                { "cipher", new Color(0.6f, 0.2f, 0.8f) },        // Purple
                { "blaze", new Color(0.8f, 0.2f, 0.2f) },         // Red
                { "tank", new Color(0.2f, 0.6f, 0.2f) },          // Green
                { "ghost", new Color(0.9f, 0.9f, 0.9f) },         // White
                { "viper", new Color(0.8f, 0.8f, 0.2f) },         // Yellow
                { "nova", new Color(0.2f, 0.8f, 0.8f) },          // Cyan
                { "pulse", new Color(0.8f, 0.2f, 0.6f) },         // Pink
                { "eclipse", new Color(0.2f, 0.2f, 0.4f) },       // Dark blue
                { "phoenix", new Color(1f, 0.5f, 0.2f) },         // Orange
            };

            foreach (var kvp in characters)
            {
                CreatePlaceholderSprite(
                    name: kvp.Key,
                    color: kvp.Value,
                    directory: "Assets/Resources/Characters"
                );
            }
            Debug.Log("✓ Generated 10 fallback character sprites");
        }

        public static void GenerateIconSprites()
        {
            // Icons with thematic colors
            Dictionary<string, Color> icons = new()
            {
                { "trophy", new Color(1f, 0.84f, 0f) },           // Gold
                { "gem", new Color(0.62f, 0.12f, 0.94f) },        // Purple
                { "coin", new Color(1f, 0.84f, 0f) },             // Gold
                { "sword", new Color(0.8f, 0.2f, 0.2f) },         // Red
                { "shield", new Color(0.2f, 0.4f, 0.8f) },        // Blue
                { "lightning", new Color(1f, 1f, 0.2f) },         // Yellow
                { "skull", new Color(0.7f, 0.7f, 0.7f) },         // Gray
                { "dice", new Color(1f, 1f, 1f) },                // White
                { "clover", new Color(0.2f, 0.8f, 0.2f) },        // Green
                { "card", new Color(1f, 1f, 1f) },                // White
                { "star", new Color(1f, 1f, 0.2f) },              // Yellow
                { "medal_gold", new Color(1f, 0.84f, 0f) },       // Gold
                { "medal_silver", new Color(0.75f, 0.75f, 0.75f) },  // Silver
                { "medal_bronze", new Color(0.8f, 0.5f, 0.2f) },  // Bronze
                { "clock", new Color(0.5f, 0.5f, 0.5f) },         // Gray
                { "crown", new Color(1f, 0.84f, 0f) },            // Gold
            };

            foreach (var kvp in icons)
            {
                CreatePlaceholderSprite(
                    name: kvp.Key,
                    color: kvp.Value,
                    size: 256,
                    directory: "Assets/Resources/Icons"
                );
            }
            Debug.Log("✓ Generated 16 fallback icon sprites");
        }

        public static void GenerateBackgroundSprites()
        {
            // Arena backgrounds with thematic overlays
            Dictionary<string, (Color, string)> backgrounds = new()
            {
                { "rookie", (new Color(0.5f, 0.4f, 0.3f), "Rookie") },        // Brown/gray
                { "silver", (new Color(0.3f, 0.3f, 0.4f), "Silver") },        // Dark gray
                { "gold", (new Color(0.6f, 0.5f, 0.2f), "Gold") },            // Warm gold
                { "diamond", (new Color(0.1f, 0.3f, 0.5f), "Diamond") },      // Dark blue
                { "legend", (new Color(0.3f, 0.1f, 0.5f), "Legend") },        // Purple
            };

            foreach (var kvp in backgrounds)
            {
                CreateParallaxBackground(
                    name: kvp.Key,
                    color: kvp.Value.Item1,
                    label: kvp.Value.Item2,
                    width: 1024,
                    height: 512
                );
            }
            Debug.Log("✓ Generated 5 fallback arena backgrounds");
        }

        private static void CreatePlaceholderSprite(string name, Color color, int size = 512, string directory = "Assets/Resources/Characters")
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Fill with rarity glow gradient
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (i % size) / (float)size;
                float y = (i / size) / (float)size;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                
                // Gradient from color center to transparent edge
                float alpha = Mathf.Max(0, 1 - dist * 2.5f);
                pixels[i] = new Color(color.r, color.g, color.b, alpha);
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = name;
            
            // Save to disk
            byte[] png = texture.EncodeToPNG();
            string path = $"{directory}/{name}.png";
            System.IO.File.WriteAllBytes(path, png);
            Debug.Log($"  → Saved: {path}");
            
            Object.Destroy(texture);
        }

        private static void CreateParallaxBackground(string name, Color color, string label, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            // Layered gradient background
            for (int i = 0; i < pixels.Length; i++)
            {
                float y = (i / width) / (float)height;
                // Gradient from top to bottom
                Color gradColor = Color.Lerp(
                    color,
                    color * 0.5f,
                    y
                );
                pixels[i] = gradColor;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            // Save
            byte[] png = texture.EncodeToPNG();
            string path = $"Assets/Resources/ArenaBackgrounds/{name}.png";
            System.IO.File.WriteAllBytes(path, png);
            Debug.Log($"  → Saved: {path} ({label})");
            
            Object.Destroy(texture);
        }
    }
}
