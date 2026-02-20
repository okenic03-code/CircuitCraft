using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        // Events
        public event Action OnBackRequested;

        // UI Elements
        private Button _btnBack;
        
        // Tabs
        private Button _tabDisplay;
        private Button _tabAudio;
        private Button _tabGame;
        private VisualElement _contentDisplay;
        private VisualElement _contentAudio;
        private VisualElement _contentGame;

        // Display Controls
        private DropdownField _dropdownResolution;
        private Toggle _toggleFullscreen;
        private DropdownField _dropdownQuality;

        // Audio Controls
        private Slider _sliderMaster;
        private Slider _sliderBgm;
        private Slider _sliderSfx;

        // Game Controls
        private Toggle _toggleAutosave;
        private Toggle _toggleHints;

        // State
        private Resolution[] _resolutions;
        private List<string> _resolutionOptions;
        private List<string> _qualityNames;
        private bool _settingsDirty;

        // PlayerPrefs Keys
        private const string KEY_MASTER_VOL = "MasterVolume";
        private const string KEY_BGM_VOL = "BGMVolume";
        private const string KEY_SFX_VOL = "SFXVolume";
        private const string KEY_AUTOSAVE = "AutoSave";
        private const string KEY_HINTS = "TutorialHints";
        private const string KEY_QUALITY = "QualityLevel";

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Query Elements
            _btnBack = root.Q<Button>("btn-back");

            _tabDisplay = root.Q<Button>("tab-display");
            _tabAudio = root.Q<Button>("tab-audio");
            _tabGame = root.Q<Button>("tab-game");

            _contentDisplay = root.Q<VisualElement>("content-display");
            _contentAudio = root.Q<VisualElement>("content-audio");
            _contentGame = root.Q<VisualElement>("content-game");

            _dropdownResolution = root.Q<DropdownField>("dropdown-resolution");
            _toggleFullscreen = root.Q<Toggle>("toggle-fullscreen");
            _dropdownQuality = root.Q<DropdownField>("dropdown-quality");

            _sliderMaster = root.Q<Slider>("slider-master");
            _sliderBgm = root.Q<Slider>("slider-bgm");
            _sliderSfx = root.Q<Slider>("slider-sfx");

            _toggleAutosave = root.Q<Toggle>("toggle-autosave");
            _toggleHints = root.Q<Toggle>("toggle-hints");

            // Register Callbacks
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);

            _tabDisplay?.RegisterCallback<ClickEvent>(evt => SwitchTab("display"));
            _tabAudio?.RegisterCallback<ClickEvent>(evt => SwitchTab("audio"));
            _tabGame?.RegisterCallback<ClickEvent>(evt => SwitchTab("game"));

            _dropdownResolution?.RegisterValueChangedCallback(OnResolutionChanged);
            _toggleFullscreen?.RegisterValueChangedCallback(OnFullscreenChanged);
            _dropdownQuality?.RegisterValueChangedCallback(OnQualityChanged);

            _sliderMaster?.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _sliderBgm?.RegisterValueChangedCallback(OnBgmVolumeChanged);
            _sliderSfx?.RegisterValueChangedCallback(OnSfxVolumeChanged);

            _toggleAutosave?.RegisterValueChangedCallback(OnAutosaveChanged);
            _toggleHints?.RegisterValueChangedCallback(OnHintsChanged);

            InitializeSettings();
        }

        private void OnDisable()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);

            if (_settingsDirty)
            {
                PlayerPrefs.Save();
                _settingsDirty = false;
            }
            
            // Note: In a real production environment, we should unregister all callbacks
            // However, for brevity and since UI elements are often destroyed with the scene,
            // we primarily focus on the main interaction buttons.
            // If the controller persists or is pooled, unregistering everything is mandatory.
        }

        private void InitializeSettings()
        {
            // --- Display ---
            // Resolutions
            _resolutions = Screen.resolutions.Select(r => new Resolution { width = r.width, height = r.height }).Distinct().ToArray();
            _resolutionOptions = new List<string>();
            int currentResolutionIndex = 0;
            
            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
                _resolutionOptions.Add(option);
                
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            if (_dropdownResolution != null)
            {
                _dropdownResolution.choices = _resolutionOptions;
                _dropdownResolution.index = currentResolutionIndex; // If supported, otherwise value
                if (currentResolutionIndex < _resolutionOptions.Count)
                    _dropdownResolution.value = _resolutionOptions[currentResolutionIndex];
            }

            // Fullscreen
            if (_toggleFullscreen != null)
                _toggleFullscreen.value = Screen.fullScreen;

            // Quality
            _qualityNames = QualitySettings.names.ToList();
            if (_dropdownQuality != null)
            {
                _dropdownQuality.choices = _qualityNames;
                // Check if persisted quality exists
                int qualityLevel = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
                if (qualityLevel < _qualityNames.Count)
                    _dropdownQuality.index = qualityLevel;
            }

            // --- Audio ---
            if (_sliderMaster != null) _sliderMaster.value = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
            if (_sliderBgm != null) _sliderBgm.value = PlayerPrefs.GetFloat(KEY_BGM_VOL, 0.8f);
            if (_sliderSfx != null) _sliderSfx.value = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1.0f);

            // --- Game ---
            if (_toggleAutosave != null) _toggleAutosave.value = PlayerPrefs.GetInt(KEY_AUTOSAVE, 1) == 1;
            if (_toggleHints != null) _toggleHints.value = PlayerPrefs.GetInt(KEY_HINTS, 1) == 1;
        }

        private void SwitchTab(string tabName)
        {
            // Reset tabs
            _tabDisplay?.RemoveFromClassList("active");
            _tabAudio?.RemoveFromClassList("active");
            _tabGame?.RemoveFromClassList("active");

            _contentDisplay?.RemoveFromClassList("active");
            _contentAudio?.RemoveFromClassList("active");
            _contentGame?.RemoveFromClassList("active");

            // Activate target
            switch (tabName)
            {
                case "display":
                    _tabDisplay?.AddToClassList("active");
                    _contentDisplay?.AddToClassList("active");
                    break;
                case "audio":
                    _tabAudio?.AddToClassList("active");
                    _contentAudio?.AddToClassList("active");
                    break;
                case "game":
                    _tabGame?.AddToClassList("active");
                    _contentGame?.AddToClassList("active");
                    break;
            }
        }

        private void OnBackClicked(ClickEvent evt)
        {
            OnBackRequested?.Invoke();
        }

        // --- Settings Callbacks ---

        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            int index = _resolutionOptions.IndexOf(evt.newValue);
            if (index >= 0 && index < _resolutions.Length)
            {
                Resolution res = _resolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }
        }

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            Screen.fullScreen = evt.newValue;
        }

        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            int index = _qualityNames.IndexOf(evt.newValue);
            if (index >= 0)
            {
                QualitySettings.SetQualityLevel(index);
                PlayerPrefs.SetInt(KEY_QUALITY, index);
                _settingsDirty = true;
            }
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            AudioListener.volume = evt.newValue;
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, evt.newValue);
            _settingsDirty = true;
        }

        private void OnBgmVolumeChanged(ChangeEvent<float> evt)
        {
            PlayerPrefs.SetFloat(KEY_BGM_VOL, evt.newValue);
            _settingsDirty = true;
            // In a real game, we'd hook this to the AudioMixer
        }

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOL, evt.newValue);
            _settingsDirty = true;
            // In a real game, we'd hook this to the AudioMixer
        }

        private void OnAutosaveChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt(KEY_AUTOSAVE, evt.newValue ? 1 : 0);
            _settingsDirty = true;
        }

        private void OnHintsChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt(KEY_HINTS, evt.newValue ? 1 : 0);
            _settingsDirty = true;
        }
    }
}
