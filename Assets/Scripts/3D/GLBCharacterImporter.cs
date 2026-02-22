using UnityEngine;
using System.Collections.Generic;

namespace VaultDash.ThreeD
{
    /// <summary>
    /// GLB CHARACTER IMPORTER
    /// Pipeline for importing 3D GLB models from Scenario.gg
    /// Framework for full 3D character support (optional Week 5+ feature)
    /// 
    /// Usage:
    /// 1. Export GLB from Scenario.gg (3D generation)
    /// 2. Place in Assets/Models/Characters/{characterName}.glb
    /// 3. Configure material + skeleton
    /// 4. GLBCharacterImporter loads and displays in-game
    /// 
    /// Requirements:
    /// - UnityGLTF or GLTFUtility package (not included in built-in)
    /// - Import GLB files as Unity assets
    /// </summary>
    public class GLBCharacterImporter : MonoBehaviour
    {
        [SerializeField] private string characterName = "knox";
        [SerializeField] private Material characterMaterial;

        private GameObject characterModel;

        public void LoadCharacter()
        {
            // Load GLB from Resources
            var asset = Resources.Load<GameObject>($"Models/Characters/{characterName}");
            
            if (asset == null)
            {
                Debug.LogWarning($"GLB character not found: {characterName}");
                return;
            }

            // Instantiate
            characterModel = Instantiate(asset, transform);
            
            // Apply material if available
            if (characterMaterial != null)
            {
                var renderer = characterModel.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = characterMaterial;
                }
            }

            // Setup animator (if skeleton exists)
            var animator = characterModel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }
        }

        public void RotateCharacter(float speed)
        {
            if (characterModel != null)
            {
                characterModel.transform.Rotate(Vector3.up, speed * Time.deltaTime);
            }
        }

        public void PlayAnimation(string animationName)
        {
            if (characterModel != null)
            {
                var animator = characterModel.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(animationName);
                }
            }
        }

        public void ApplyToonShader()
        {
            if (characterModel != null)
            {
                var renderers = characterModel.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    // Assign ToonCelShaded shader to remove glossy PBR look
                    var shader = Shader.Find("VaultDash/ToonCelShaded");
                    if (shader != null)
                    {
                        var mat = new Material(shader);
                        // Copy main texture if present
                        if (renderer.material.mainTexture != null)
                        {
                            mat.mainTexture = renderer.material.mainTexture;
                        }
                        renderer.material = mat;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (characterModel != null)
            {
                Destroy(characterModel);
            }
        }
    }