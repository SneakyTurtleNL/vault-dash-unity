using UnityEngine;

namespace VaultDash.Audio
{
    /// <summary>
    /// FMOD MUSIC MANAGER
    /// Framework for dynamic music system with intensity ramp
    /// Integrates FMOD Studio for professional audio
    /// 
    /// Setup:
    /// 1. Import FMOD Studio Integration
    /// 2. Create FMOD project with:
    ///    - Event: "event:/Music/Ambient" (menu music)
    ///    - Event: "event:/Music/Gameplay" (in-game, with intensity parameter)
    ///    - Event: "event:/Music/Victory" (win fanfare)
    ///    - Event: "event:/Music/Defeat" (loss theme)
    /// 3. Attach this script to a persistent GameObject
    /// </summary>
    public class FMODMusicManager : MonoBehaviour
    {
        public static FMODMusicManager Instance { get; private set; }

        // FMOD event references (set in Inspector or load via strings)
        private const string EVENT_AMBIENT = "event:/Music/Ambient";
        private const string EVENT_GAMEPLAY = "event:/Music/Gameplay";
        private const string EVENT_VICTORY = "event:/Music/Victory";
        private const string EVENT_DEFEAT = "event:/Music/Defeat";

        // Runtime instance handles
        private FMOD.Studio.EventInstance currentMusicInstance;
        private FMOD.Studio.EventInstance gameplayMusicInstance;

        private float currentIntensity = 0f;
        private float targetIntensity = 0f;
        private float intensityLerpSpeed = 0.5f;

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

        private void Update()
        {
            // Smooth intensity transitions
            if (Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, intensityLerpSpeed * Time.deltaTime);
                
                if (gameplayMusicInstance.isValid())
                {
                    gameplayMusicInstance.setParameterByName("Intensity", currentIntensity);
                }
            }
        }

        public void PlayAmbientMusic()
        {
            StopCurrentMusic();
            try
            {
                currentMusicInstance = FMODUnity.RuntimeManager.CreateInstance(EVENT_AMBIENT);
                currentMusicInstance.start();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FMOD ambient music failed (not configured): {e.Message}");
            }
        }

        public void PlayGameplayMusic()
        {
            StopCurrentMusic();
            try
            {
                gameplayMusicInstance = FMODUnity.RuntimeManager.CreateInstance(EVENT_GAMEPLAY);
                gameplayMusicInstance.setParameterByName("Intensity", 0f);
                gameplayMusicInstance.start();
                currentMusicInstance = gameplayMusicInstance;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FMOD gameplay music failed (not configured): {e.Message}");
            }
        }

        public void SetIntensity(float intensity)
        {
            targetIntensity = Mathf.Clamp01(intensity);
        }

        public void PlayVictoryMusic()
        {
            StopCurrentMusic();
            try
            {
                currentMusicInstance = FMODUnity.RuntimeManager.CreateInstance(EVENT_VICTORY);
                currentMusicInstance.start();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FMOD victory music failed (not configured): {e.Message}");
            }
        }

        public void PlayDefeatMusic()
        {
            StopCurrentMusic();
            try
            {
                currentMusicInstance = FMODUnity.RuntimeManager.CreateInstance(EVENT_DEFEAT);
                currentMusicInstance.start();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FMOD defeat music failed (not configured): {e.Message}");
            }
        }

        public void StopCurrentMusic(float fadeOutDuration = 0.5f)
        {
            if (currentMusicInstance.isValid())
            {
                currentMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                currentMusicInstance.release();
            }
        }

        private void OnDestroy()
        {
            StopCurrentMusic();
        }
    }
}
