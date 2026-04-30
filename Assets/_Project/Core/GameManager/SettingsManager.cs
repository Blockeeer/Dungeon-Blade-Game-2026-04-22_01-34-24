using System;
using UnityEngine;

namespace DungeonBlade.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        const string KeyMaster = "settings.master";
        const string KeyMusic = "settings.music";
        const string KeySfx = "settings.sfx";
        const string KeySensitivity = "settings.sensitivity";
        const string KeyFullscreen = "settings.fullscreen";

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float MouseSensitivity { get; private set; } = 0.12f;
        public bool Fullscreen { get; private set; } = true;

        public event Action OnSettingsChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            Apply();
        }

        public void SetMaster(float v) { MasterVolume = Mathf.Clamp01(v); Save(); Apply(); }
        public void SetMusic(float v) { MusicVolume = Mathf.Clamp01(v); Save(); Apply(); }
        public void SetSfx(float v) { SfxVolume = Mathf.Clamp01(v); Save(); Apply(); }
        public void SetSensitivity(float v) { MouseSensitivity = Mathf.Clamp(v, 0.01f, 1f); Save(); Apply(); }
        public void SetFullscreen(bool v) { Fullscreen = v; Save(); Apply(); }

        public void ResetToDefaults()
        {
            MasterVolume = 1f;
            MusicVolume = 1f;
            SfxVolume = 1f;
            MouseSensitivity = 0.12f;
            Fullscreen = true;
            Save();
            Apply();
        }

        void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyMaster, 1f);
            MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 1f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f);
            MouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 0.12f);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        }

        void Save()
        {
            PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            PlayerPrefs.SetFloat(KeySensitivity, MouseSensitivity);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        void Apply()
        {
            AudioListener.volume = MasterVolume;
            Screen.fullScreen = Fullscreen;
            OnSettingsChanged?.Invoke();
        }
    }
}
