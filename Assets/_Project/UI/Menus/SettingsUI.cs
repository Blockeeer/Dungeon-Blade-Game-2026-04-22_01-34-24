using DungeonBlade.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.UI.Menus
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Slider sensitivitySlider;

        [Header("Toggles")]
        [SerializeField] Toggle fullscreenToggle;

        [Header("Readouts (optional)")]
        [SerializeField] TMP_Text masterValueText;
        [SerializeField] TMP_Text musicValueText;
        [SerializeField] TMP_Text sfxValueText;
        [SerializeField] TMP_Text sensitivityValueText;

        [Header("Buttons")]
        [SerializeField] Button closeButton;
        [SerializeField] Button resetButton;

        bool _bound;

        void OnEnable()
        {
            if (SettingsManager.Instance == null) return;

            BindFromManager();
            if (_bound) return;
            _bound = true;

            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        }

        void BindFromManager()
        {
            var s = SettingsManager.Instance;
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(s.MasterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.SfxVolume);
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(s.MouseSensitivity);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(s.Fullscreen);
            UpdateReadouts();
        }

        void UpdateReadouts()
        {
            var s = SettingsManager.Instance;
            if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(s.MasterVolume * 100)}%";
            if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(s.MusicVolume * 100)}%";
            if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(s.SfxVolume * 100)}%";
            if (sensitivityValueText != null) sensitivityValueText.text = $"{s.MouseSensitivity:F2}";
        }

        void OnMasterChanged(float v) { SettingsManager.Instance.SetMaster(v); UpdateReadouts(); }
        void OnMusicChanged(float v) { SettingsManager.Instance.SetMusic(v); UpdateReadouts(); }
        void OnSfxChanged(float v) { SettingsManager.Instance.SetSfx(v); UpdateReadouts(); }
        void OnSensitivityChanged(float v) { SettingsManager.Instance.SetSensitivity(v); UpdateReadouts(); }
        void OnFullscreenChanged(bool v) { SettingsManager.Instance.SetFullscreen(v); }

        void OnClose() => gameObject.SetActive(false);

        void OnReset()
        {
            SettingsManager.Instance.ResetToDefaults();
            BindFromManager();
        }
    }
}
