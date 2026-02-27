using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Lightweight main menu button controller that raises events for external consumers.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField, Tooltip("UI document that contains main menu button elements.")] private UIDocument _uiDocument;
        [SerializeField, Tooltip("Settings screen GameObject toggled from the main menu.")] private GameObject _settingsScreen;

        private Button _playButton;
        private Button _settingsButton;
        private Button _quitButton;

        /// <summary>
        /// Fired when the Play button is clicked.
        /// </summary>
        public event Action OnPlayRequested;

        /// <summary>
        /// Fired when the Settings button is clicked.
        /// </summary>
        public event Action OnSettingsRequested;

        /// <summary>
        /// Fired when the Quit button is clicked.
        /// </summary>
        public event Action OnQuitRequested;

        private void OnEnable()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root is null) return;

            _playButton = root.Q<Button>("btn-play");
            _settingsButton = root.Q<Button>("btn-settings");
            _quitButton = root.Q<Button>("btn-quit");

            _playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);

            if (_settingsScreen != null)
            {
                var settingsCtrl = _settingsScreen.GetComponent<SettingsController>();
                if (settingsCtrl != null)
                {
                    settingsCtrl.OnBackRequested += OnSettingsBackRequested;
                }
            }
        }

        private void OnDisable()
        {
            _playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
            _settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
            _quitButton?.UnregisterCallback<ClickEvent>(OnQuitClicked);

            if (_settingsScreen != null)
            {
                var settingsCtrl = _settingsScreen.GetComponent<SettingsController>();
                if (settingsCtrl != null)
                {
                    settingsCtrl.OnBackRequested -= OnSettingsBackRequested;
                }
            }
        }

        private void OnPlayClicked(ClickEvent evt)
        {
            OnPlayRequested?.Invoke();
            StageSelectionContext.Clear();
            SceneManager.LoadScene(SceneNames.StageSelect);
        }

        private void OnSettingsClicked(ClickEvent evt)
        {
            OnSettingsRequested?.Invoke();
            if (_settingsScreen != null)
            {
                _settingsScreen.SetActive(true);
            }
        }

        private void OnSettingsBackRequested()
        {
            if (_settingsScreen != null)
            {
                _settingsScreen.SetActive(false);
            }
        }

        private void OnQuitClicked(ClickEvent evt)
        {
            OnQuitRequested?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
