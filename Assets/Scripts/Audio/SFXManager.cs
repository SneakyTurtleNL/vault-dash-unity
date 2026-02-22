using UnityEngine;
using System.Collections.Generic;

namespace VaultDash.Audio
{
    /// <summary>
    /// SFX MANAGER
    /// Standardized sound effects for UI and gameplay
    /// Supercell-grade audio feedback
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [SerializeField] private AudioSource uiSFXSource;
        [SerializeField] private AudioSource gameplaySFXSource;

        private Dictionary<string, AudioClip> sfxLibrary = new();

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

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (uiSFXSource == null)
            {
                var obj = new GameObject("UI SFX Source");
                obj.transform.SetParent(transform);
                uiSFXSource = obj.AddComponent<AudioSource>();
                uiSFXSource.spatialBlend = 0f;
                uiSFXSource.volume = 0.8f;
            }

            if (gameplaySFXSource == null)
            {
                var obj = new GameObject("Gameplay SFX Source");
                obj.transform.SetParent(transform);
                gameplaySFXSource = obj.AddComponent<AudioSource>();
                gameplaySFXSource.spatialBlend = 0.5f;
                gameplaySFXSource.volume = 0.7f;
            }
        }

        public void PlayUISound(string soundName, float volumeScale = 1f)
        {
            if (uiSFXSource == null) return;

            AudioClip clip = GetSoundClip(soundName);
            if (clip != null)
                uiSFXSource.PlayOneShot(clip, volumeScale * 0.8f);
        }

        public void PlayGameplaySound(string soundName, Vector3 position, float volumeScale = 1f)
        {
            if (gameplaySFXSource == null) return;

            AudioClip clip = GetSoundClip(soundName);
            if (clip != null)
            {
                gameplaySFXSource.transform.position = position;
                gameplaySFXSource.PlayOneShot(clip, volumeScale * 0.7f);
            }
        }

        public void PlayClickSound()
        {
            PlayUISound("click", 0.9f);
        }

        public void PlayPopSound()
        {
            PlayUISound("pop", 1f);
        }

        public void PlayUpgradeSound()
        {
            PlayUISound("upgrade", 1.1f);
        }

        public void PlayVictorySound()
        {
            PlayUISound("victory", 1.2f);
        }

        public void PlayDefeatSound()
        {
            PlayUISound("defeat", 0.9f);
        }

        private AudioClip GetSoundClip(string soundName)
        {
            // Try to load from resources
            if (!sfxLibrary.ContainsKey(soundName))
            {
                AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
                if (clip != null)
                    sfxLibrary[soundName] = clip;
                else
                    Debug.LogWarning($"SFX not found: {soundName}");
            }

            sfxLibrary.TryGetValue(soundName, out var result);
            return result;
        }

        public void StopAllSounds()
        {
            if (uiSFXSource != null)
                uiSFXSource.Stop();
            if (gameplaySFXSource != null)
                gameplaySFXSource.Stop();
        }
    }
}
