using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Lightweight main menu button controller that raises events for external consumers.
    /// Note: SceneFlowManager also handles main menu buttons directly.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private GameObject _settingsScreen;

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
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

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
