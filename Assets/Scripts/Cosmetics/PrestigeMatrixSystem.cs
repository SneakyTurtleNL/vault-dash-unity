using UnityEngine;
using System.Collections.Generic;

namespace VaultDash.Cosmetics
{
    /// <summary>
    /// PRESTIGE MATRIX SYSTEM
    /// Manages 5 material variants per character (P0, P1, P5, P10, P20)
    /// No new geometry needed — just material parameter swaps
    /// 
    /// Instead of 50 GLB files, we have:
    /// - 10 base GLB models
    /// - 40 material variants (internal shader parameters)
    /// 
    /// Material parameters:
    /// - _Tint (color tint)
    /// - _EmissiveIntensity (glow intensity)
    /// - _OutlineThickness (cel-shaded outline width)
    /// - _GlowColor (for legendary tiers)
    /// </summary>
    public class PrestigeMatrixSystem : MonoBehaviour
    {
        [System.Serializable]
        public class PrestigeMaterial
        {
            public int prestigeLevel;           // 0, 1, 5, 10, 20
            public string name;                  // "Trainee", "Elite", "Legend", etc.
            public Color tintColor;
            public float emissiveIntensity;
            public float outlineThickness;
            public Color glowColor;
        }

        [System.Serializable]
        public class CharacterPrestigeTiers
        {
            public string characterId;
            public List<PrestigeMaterial> tiers = new();
        }

        private Dictionary<string, Dictionary<int, PrestigeMaterial>> prestigeMatrix = new();

        private void Awake()
        {
            InitializePrestigeMatrix();
        }

        private void InitializePrestigeMatrix()
        {
            // Agent Zero
            AddCharacterTiers("agent_zero", new[]
            {
                new PrestigeMaterial { prestigeLevel = 0, name = "Agent Zero", tintColor = new Color(0.2f, 0.4f, 0.8f), emissiveIntensity = 0f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 1, name = "Trainee Agent", tintColor = new Color(0.3f, 0.5f, 0.9f), emissiveIntensity = 0.2f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 5, name = "Elite Agent", tintColor = new Color(0.2f, 0.4f, 0.8f), emissiveIntensity = 0.5f, outlineThickness = 3f, glowColor = new Color(1f, 0.84f, 0f) },
                new PrestigeMaterial { prestigeLevel = 10, name = "Legendary Agent", tintColor = new Color(0.1f, 0.3f, 0.7f), emissiveIntensity = 1f, outlineThickness = 4f, glowColor = new Color(1f, 0.84f, 0f) },
                new PrestigeMaterial { prestigeLevel = 20, name = "Ultimate Agent", tintColor = new Color(0.5f, 0.8f, 1f), emissiveIntensity = 1.5f, outlineThickness = 5f, glowColor = new Color(1f, 0.2f, 0.8f) },
            });

            // Cipher (similar pattern for other characters)
            AddCharacterTiers("cipher", new[]
            {
                new PrestigeMaterial { prestigeLevel = 0, name = "Cipher", tintColor = new Color(0.6f, 0.2f, 0.8f), emissiveIntensity = 0f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 1, name = "Junior Hacker", tintColor = new Color(0.7f, 0.3f, 0.9f), emissiveIntensity = 0.2f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 5, name = "Master Hacker", tintColor = new Color(0.6f, 0.2f, 0.8f), emissiveIntensity = 0.5f, outlineThickness = 3f, glowColor = new Color(0f, 1f, 0f) },
                new PrestigeMaterial { prestigeLevel = 10, name = "Legendary Cipher", tintColor = new Color(0.5f, 0.1f, 0.7f), emissiveIntensity = 1f, outlineThickness = 4f, glowColor = new Color(0f, 1f, 0f) },
                new PrestigeMaterial { prestigeLevel = 20, name = "Ultimate Cipher", tintColor = new Color(0.8f, 0.5f, 1f), emissiveIntensity = 1.5f, outlineThickness = 5f, glowColor = new Color(0f, 1f, 1f) },
            });

            // Blaze
            AddCharacterTiers("blaze", new[]
            {
                new PrestigeMaterial { prestigeLevel = 0, name = "Blaze", tintColor = new Color(0.8f, 0.2f, 0.2f), emissiveIntensity = 0f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 1, name = "Fire Initiate", tintColor = new Color(0.9f, 0.3f, 0.1f), emissiveIntensity = 0.3f, outlineThickness = 2f, glowColor = Color.white },
                new PrestigeMaterial { prestigeLevel = 5, name = "Inferno Master", tintColor = new Color(0.8f, 0.2f, 0.2f), emissiveIntensity = 0.7f, outlineThickness = 3f, glowColor = new Color(1f, 0.5f, 0f) },
                new PrestigeMaterial { prestigeLevel = 10, name = "Legendary Phoenix", tintColor = new Color(1f, 0.1f, 0f), emissiveIntensity = 1.2f, outlineThickness = 4f, glowColor = new Color(1f, 0.5f, 0f) },
                new PrestigeMaterial { prestigeLevel = 20, name = "Ultimate Inferno", tintColor = new Color(1f, 0.8f, 0.2f), emissiveIntensity = 2f, outlineThickness = 5f, glowColor = new Color(1f, 1f, 0f) },
            });

            // Tank, Ghost, Viper, Nova, Pulse, Eclipse, Phoenix (same pattern)
            // For brevity, placeholder — full implementation would follow the same structure
        }

        private void AddCharacterTiers(string characterId, PrestigeMaterial[] tiers)
        {
            var tierDict = new Dictionary<int, PrestigeMaterial>();
            foreach (var tier in tiers)
            {
                tierDict[tier.prestigeLevel] = tier;
            }
            prestigeMatrix[characterId] = tierDict;
        }

        public PrestigeMaterial GetPrestigeMaterial(string characterId, int prestigeLevel)
        {
            if (!prestigeMatrix.ContainsKey(characterId))
                return null;

            var tiers = prestigeMatrix[characterId];
            
            // Find closest tier below or equal to prestigeLevel
            int closestTier = 0;
            foreach (var tier in tiers.Keys)
            {
                if (tier <= prestigeLevel && tier > closestTier)
                    closestTier = tier;
            }

            return tiers.ContainsKey(closestTier) ? tiers[closestTier] : null;
        }

        public void ApplyPrestigeMaterial(Renderer characterRenderer, string characterId, int prestigeLevel)
        {
            var material = GetPrestigeMaterial(characterId, prestigeLevel);
            if (material == null) return;

            var mat = characterRenderer.material;
            mat.SetColor("_Tint", material.tintColor);
            mat.SetFloat("_EmissiveIntensity", material.emissiveIntensity);
            mat.SetFloat("_OutlineThickness", material.outlineThickness);
            mat.SetColor("_GlowColor", material.glowColor);
        }

        public string GetPrestigeDisplayName(string characterId, int prestigeLevel)
        {
            var material = GetPrestigeMaterial(characterId, prestigeLevel);
            return material?.name ?? "Unknown";
        }
    }
}
