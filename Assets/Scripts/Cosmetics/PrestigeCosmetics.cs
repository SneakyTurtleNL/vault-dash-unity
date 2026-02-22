using UnityEngine;
using System.Collections.Generic;

namespace VaultDash.Cosmetics
{
    /// <summary>
    /// PRESTIGE COSMETICS SYSTEM
    /// Unlock skins, trails, effects at prestige milestones (P1, P5, P10, P20, P50)
    /// All cosmetics are visual-only (no gameplay advantage)
    /// </summary>
    public class PrestigeCosmetics : MonoBehaviour
    {
        public enum CosmeticType { Skin, Trail, Effect, Emote }

        [System.Serializable]
        public class PrestigeCosmetic
        {
            public string id;
            public CosmeticType type;
            public int prestigeLevelRequired;
            public string displayName;
            public Sprite icon;
            public Color rarity;
        }

        [SerializeField] private List<PrestigeCosmetic> cosmetics = new();

        private Dictionary<string, PrestigeCosmetic> cosmeticRegistry = new();

        private void Awake()
        {
            foreach (var cosmetic in cosmetics)
            {
                cosmeticRegistry[cosmetic.id] = cosmetic;
            }
        }

        public PrestigeCosmetic GetCosmetic(string cosmeticId)
        {
            return cosmeticRegistry.ContainsKey(cosmeticId) ? cosmeticRegistry[cosmeticId] : null;
        }

        public List<PrestigeCosmetic> GetUnlockedCosmeticsForPrestige(int prestigeLevel)
        {
            var unlocked = new List<PrestigeCosmetic>();
            foreach (var cosmetic in cosmetics)
            {
                if (cosmetic.prestigeLevelRequired <= prestigeLevel)
                    unlocked.Add(cosmetic);
            }
            return unlocked;
        }

        public bool IsCosmeticUnlocked(string cosmeticId, int playerPrestigeLevel)
        {
            var cosmetic = GetCosmetic(cosmeticId);
            if (cosmetic == null) return false;
            return playerPrestigeLevel >= cosmetic.prestigeLevelRequired;
        }

        public void ApplyCosmetic(string cosmeticId, CharacterController character)
        {
            var cosmetic = GetCosmetic(cosmeticId);
            if (cosmetic == null) return;

            switch (cosmetic.type)
            {
                case CosmeticType.Skin:
                    ApplySkin(cosmetic, character);
                    break;
                case CosmeticType.Trail:
                    ApplyTrail(cosmetic, character);
                    break;
                case CosmeticType.Effect:
                    ApplyEffect(cosmetic, character);
                    break;
                case CosmeticType.Emote:
                    ApplyEmote(cosmetic, character);
                    break;
            }
        }

        private void ApplySkin(PrestigeCosmetic cosmetic, CharacterController character)
        {
            // Load skin from Resources/Cosmetics/Skins/{id}
            var skinAsset = Resources.Load<Sprite>($"Cosmetics/Skins/{cosmetic.id}");
            if (skinAsset != null)
            {
                character.SetCharacterSprite(skinAsset);
            }
        }

        private void ApplyTrail(PrestigeCosmetic cosmetic, CharacterController character)
        {
            // Attach trail particle effect to character
            var trailPrefab = Resources.Load<GameObject>($"Cosmetics/Trails/{cosmetic.id}");
            if (trailPrefab != null)
            {
                var trail = Instantiate(trailPrefab);
                trail.transform.SetParent(character.transform);
                trail.transform.localPosition = Vector3.zero;
            }
        }

        private void ApplyEffect(PrestigeCosmetic cosmetic, CharacterController character)
        {
            // Apply visual effect (glow, aura, etc.)
            // Example: SetCharacterGlowColor(cosmetic.rarity)
            if (character.TryGetComponent<SpriteRenderer>(out var renderer))
            {
                var mat = renderer.material;
                mat.SetColor("_OutlineColor", cosmetic.rarity);
            }
        }

        private void ApplyEmote(PrestigeCosmetic cosmetic, CharacterController character)
        {
            // Play emote animation (celebration, taunt, etc.)
            // character.PlayEmote(cosmetic.id)
        }
    }

    public class CharacterController : MonoBehaviour
    {
        public void SetCharacterSprite(Sprite sprite)
        {
            GetComponent<SpriteRenderer>().sprite = sprite;
        }

        public void PlayEmote(string emoteId)
        {
            // Implement emote animation playback
        }
    }
}
