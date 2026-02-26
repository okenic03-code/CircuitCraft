using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CircuitCraft.Controllers;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls pause overlay visibility and pause menu actions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        /// <summary>Raised after the pause menu requests resume.</summary>
        public event Action OnResumeRequested;
        /// <summary>Raised when the player requests restarting the stage.</summary>
        public event Action OnRestartRequested;
        /// <summary>Raised when the player requests opening stage select.</summary>
        public event Action OnStageSelectRequested;
        /// <summary>Raised when the player requests returning to the main menu.</summary>
        public event Action OnMainMenuRequested;

        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting pause menu elements.")]
        private UIDocument _uiDocument;
        [SerializeField]
        [Tooltip("Wire routing controller — ESC is suppressed while wiring is active.")]
        private WireRoutingController _wireRoutingController;
        [SerializeField]
        [Tooltip("Component interaction controller - ESC deselects component before opening pause menu.")]
        private ComponentInteraction _componentInteraction;
        [SerializeField]
        [Tooltip("Placement controller - ESC cancels placement before opening pause menu.")]
        private PlacementController _placementController;
        private VisualElement _root;
        private VisualElement _pauseOverlay;
        
        private Button _btnResume;
        private Button _btnRestart;
        private Button _btnStageSelect;
        private Button _btnMainMenu;

        private bool _isPaused = false;
        /// <summary>
        /// Gets whether the pause overlay is currently active.
        /// </summary>
        public bool IsPaused => _isPaused;

        private void OnEnable()
        {
            ResolveDependencies();

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
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_isPaused)
            {
                TogglePause();
                return;
            }

            if (TryConsumeGameplayEscape())
                return;

            TogglePause();
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

            StageSelectionContext.Clear();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void ResolveDependencies()
        {
            if (_wireRoutingController == null)
                _wireRoutingController = FindObjectOfType<WireRoutingController>();

            if (_componentInteraction == null)
                _componentInteraction = FindObjectOfType<ComponentInteraction>();

            if (_placementController == null)
                _placementController = FindObjectOfType<PlacementController>();
        }

        private bool TryConsumeGameplayEscape()
        {
            if (_wireRoutingController != null)
            {
                if (_wireRoutingController.ConsumedEscapeThisFrame)
                    return true;

                if (_wireRoutingController.TryConsumeEscape())
                    return true;
            }

            if (_componentInteraction != null)
            {
                if (_componentInteraction.ConsumedEscapeThisFrame)
                    return true;

                if (_componentInteraction.TryConsumeEscape())
                    return true;
            }

            if (_placementController != null)
            {
                if (_placementController.ConsumedEscapeThisFrame)
                    return true;

                if (_placementController.TryConsumeEscape())
                    return true;
            }

            return false;
        }
    }
}
