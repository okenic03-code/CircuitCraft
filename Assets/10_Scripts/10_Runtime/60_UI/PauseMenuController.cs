using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls pause overlay visibility and pause menu actions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        // Events
        public event Action OnResumeRequested;
        public event Action OnRestartRequested;
        public event Action OnStageSelectRequested;
        public event Action OnMainMenuRequested;

        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting pause menu elements.")]
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _pauseOverlay;
        
        private Button _btnResume;
        private Button _btnRestart;
        private Button _btnStageSelect;
        private Button _btnMainMenu;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            
            _root = _uiDocument.rootVisualElement;
            if (_root is null) return;

            _pauseOverlay = _root.Q<VisualElement>("pause-overlay");
            
            _btnResume = _root.Q<Button>("btn-resume");
            _btnRestart = _root.Q<Button>("btn-restart");
            _btnStageSelect = _root.Q<Button>("btn-stage-select");
            _btnMainMenu = _root.Q<Button>("btn-main-menu");

            if (_btnResume is not null) _btnResume.clicked += OnResumeClicked;
            if (_btnRestart is not null) _btnRestart.clicked += OnRestartClicked;
            if (_btnStageSelect is not null) _btnStageSelect.clicked += OnStageSelectClicked;
            if (_btnMainMenu is not null) _btnMainMenu.clicked += OnMainMenuClicked;
            
            // Ensure we start hidden
            if (_pauseOverlay is not null)
            {
                _pauseOverlay.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            if (_btnResume is not null) _btnResume.clicked -= OnResumeClicked;
            if (_btnRestart is not null) _btnRestart.clicked -= OnRestartClicked;
            if (_btnStageSelect is not null) _btnStageSelect.clicked -= OnStageSelectClicked;
            if (_btnMainMenu is not null) _btnMainMenu.clicked -= OnMainMenuClicked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Toggles between paused and resumed game states.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            
            if (_pauseOverlay is not null)
            {
                _pauseOverlay.style.display = DisplayStyle.Flex;
            }
        }

        private void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            
            if (_pauseOverlay is not null)
            {
                _pauseOverlay.style.display = DisplayStyle.None;
            }
            
            OnResumeRequested?.Invoke();
        }

        private void OnResumeClicked()
        {
            Resume();
        }

        private void OnRestartClicked()
        {
            Resume(); // Unpause first
            OnRestartRequested?.Invoke();
        }

        private void OnStageSelectClicked()
        {
            Resume(); // Unpause first
            OnStageSelectRequested?.Invoke();
        }

        private void OnMainMenuClicked()
        {
            Resume(); // Unpause first
            OnMainMenuRequested?.Invoke();
            
            // Load Main Menu scene (Build Index 0)
            SceneManager.LoadScene(0);
        }
    }
}
