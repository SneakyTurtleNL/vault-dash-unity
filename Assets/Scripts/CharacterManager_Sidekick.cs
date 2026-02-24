using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CharacterManager_Sidekick
/// Manages character + skin loading for Sidekick modular characters
/// 
/// Usage:
///   characterManager.LoadCharacter("agent_zero", "blue");
///   characterManager.LoadCharacter("blaze", "red");
/// </summary>

public class CharacterManager_Sidekick : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterSkinData
    {
        public string characterId;  // "agent_zero", "blaze", etc.
        public string skinId;       // "blue", "red", "gold", etc.
        public GameObject prefab;   // Reference to skin prefab
    }

    [SerializeField] private CharacterSkinData[] availableSkins;
    [SerializeField] private Transform spawnPoint;  // Where to instantiate character
    [SerializeField] private Material toonMaterial;  // ToonCelShaded material

    private GameObject currentCharacter;
    private Dictionary<string, CharacterSkinData> skinLookup = new Dictionary<string, CharacterSkinData>();

    private void Start()
    {
        // Build lookup table for fast access
        foreach (var skin in availableSkins)
        {
            string key = $"{skin.characterId}_{skin.skinId}";
            skinLookup[key] = skin;
        }

        // Spawn default character
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    /// <summary>
    /// Load a character with specific skin
    /// </summary>
    public void LoadCharacter(string characterId, string skinId)
    {
        string key = $"{characterId}_{skinId}";

        if (!skinLookup.ContainsKey(key))
        {
            Debug.LogError($"[CharacterManager] Skin not found: {key}");
            return;
        }

        // Destroy old character
        if (currentCharacter != null)
            Destroy(currentCharacter);

        // Instantiate new character
        var skinData = skinLookup[key];
        currentCharacter = Instantiate(skinData.prefab, spawnPoint);

        // Apply toon shader if available
        if (toonMaterial != null)
            ApplyToonShaderToCharacter(currentCharacter);

        Debug.Log($"[CharacterManager] Loaded character: {characterId} (skin: {skinId})");
    }

    /// <summary>
    /// Apply toon shader to all mesh renderers in character
    /// </summary>
    private void ApplyToonShaderToCharacter(GameObject character)
    {
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.material = new Material(toonMaterial);
        }

        MeshRenderer[] staticRenderers = character.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in staticRenderers)
        {
            renderer.material = new Material(toonMaterial);
        }
    }

    /// <summary>
    /// Get list of available skins for a character
    /// </summary>
    public string[] GetAvailableSkinsForCharacter(string characterId)
    {
        var skins = new List<string>();
        foreach (var skin in availableSkins)
        {
            if (skin.characterId == characterId)
                skins.Add(skin.skinId);
        }
        return skins.ToArray();
    }

    /// <summary>
    /// Get current character instance (for animation/interaction)
    /// </summary>
    public GameObject GetCurrentCharacter() => currentCharacter;
}
