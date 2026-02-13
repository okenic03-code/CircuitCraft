using System.Collections;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Manages screen transitions in the single-scene game.
    /// Navigates between MainMenu → StageSelect → GamePlay by
    /// enabling/disabling UIDocument GameObjects.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private GameObject _stageSelectScreen;
        [SerializeField] private GameObject _gamePlayScreen;

        [Header("Controllers")]
        [SerializeField] private StageSelectController _stageSelectController;
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private ProgressionManager _progressionManager;
        [SerializeField] private ComponentPaletteController _paletteController;

        [Header("Main Menu")]
        [SerializeField] private UIDocument _mainMenuDocument;

        [Header("Stage Data")]
        [SerializeField]
        [Tooltip("Ordered stage definitions matching StageSelectController's stage list.")]
        private StageDefinition[] _stages;

        [Header("Transition Settings")]
        [SerializeField]
        [Tooltip("Seconds to wait after stage completion before returning to stage select.")]
        private float _completionDelay = 2.0f;
        private WaitForSeconds _waitForCompletion;

        private enum GameScreen { MainMenu, StageSelect, GamePlay }

        private Button _playButton;
        private Button _quitButton;

        private void Start()
        {
            WireMainMenuButtons();
            WireStageSelectEvents();
            WireStageManagerEvents();
            _waitForCompletion = new WaitForSeconds(_completionDelay);

            ShowScreen(GameScreen.MainMenu);
        }

        private void OnDestroy()
        {
            UnwireMainMenuButtons();
            UnwireStageSelectEvents();
            UnwireStageManagerEvents();
        }

        // ─── Wiring ───────────────────────────────────────────────────────

        private void WireMainMenuButtons()
        {
            if (_mainMenuDocument == null) return;

            var root = _mainMenuDocument.rootVisualElement;
            if (root == null) return;

            _playButton = root.Q<Button>("btn-play");
            if (_playButton != null)
                _playButton.clicked += HandlePlayClicked;

            _quitButton = root.Q<Button>("btn-quit");
            if (_quitButton != null)
                _quitButton.clicked += HandleQuitClicked;
        }

        private void UnwireMainMenuButtons()
        {
            if (_playButton != null)
                _playButton.clicked -= HandlePlayClicked;

            if (_quitButton != null)
                _quitButton.clicked -= HandleQuitClicked;
        }

        private void WireStageSelectEvents()
        {
            if (_stageSelectController == null) return;
            _stageSelectController.OnStageSelected += HandleStageSelected;
            _stageSelectController.OnBackToMenu += HandleBackToMenu;
        }

        private void UnwireStageSelectEvents()
        {
            if (_stageSelectController == null) return;
            _stageSelectController.OnStageSelected -= HandleStageSelected;
            _stageSelectController.OnBackToMenu -= HandleBackToMenu;
        }

        private void WireStageManagerEvents()
        {
            if (_stageManager == null) return;
            _stageManager.OnStageLoaded += HandleStageLoaded;
            _stageManager.OnStageCompleted += HandleStageCompleted;
        }

        private void UnwireStageManagerEvents()
        {
            if (_stageManager == null) return;
            _stageManager.OnStageLoaded -= HandleStageLoaded;
            _stageManager.OnStageCompleted -= HandleStageCompleted;
        }

        // ─── Event Handlers ───────────────────────────────────────────────

        private void HandlePlayClicked()
        {
            SyncProgressionToStageSelect();
            ShowScreen(GameScreen.StageSelect);
        }

        private void HandleBackToMenu()
        {
            ShowScreen(GameScreen.MainMenu);
        }

        private void HandleStageSelected(StageDefinition stage)
        {
            _stageManager.LoadStage(stage);
            ShowScreen(GameScreen.GamePlay);
        }

        private void HandleStageCompleted(ScoreBreakdown breakdown)
        {
            // ProgressionManager auto-records completion via its own OnStageCompleted subscription
            // (it subscribes in OnEnable, which runs before Start, so it fires first).
            // We delay briefly so the player can see the results panel, then navigate back.
            StartCoroutine(TransitionAfterCompletion());
        }

        private void HandleStageLoaded()
        {
            if (_paletteController != null && _stageManager.CurrentStage != null)
            {
                _paletteController.SetAvailableComponents(_stageManager.CurrentStage.AllowedComponents);
            }
        }

        private IEnumerator TransitionAfterCompletion()
        {
            yield return _waitForCompletion;

            SyncProgressionToStageSelect();
            ShowScreen(GameScreen.StageSelect);
        }

        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─── Screen Management ────────────────────────────────────────────

        private void ShowScreen(GameScreen screen)
        {
            if (_mainMenuScreen != null)
                _mainMenuScreen.SetActive(screen == GameScreen.MainMenu);

            if (_stageSelectScreen != null)
                _stageSelectScreen.SetActive(screen == GameScreen.StageSelect);

            if (_gamePlayScreen != null)
                _gamePlayScreen.SetActive(screen == GameScreen.GamePlay);

#if UNITY_EDITOR
            Debug.Log($"SceneFlowManager: Showing {screen}");
#endif
        }

        // ─── Progression Sync ─────────────────────────────────────────────

        /// <summary>
        /// Pushes unlock/star state from ProgressionManager into StageSelectController.
        /// Data arrays are updated even while the StageSelect screen is inactive;
        /// the UI refreshes when the screen's OnEnable calls UpdateStageDisplay().
        /// </summary>
        private void SyncProgressionToStageSelect()
        {
            if (_progressionManager == null || _stageSelectController == null || _stages == null)
                return;

            for (int i = 0; i < _stages.Length; i++)
            {
                var stage = _stages[i];
                if (stage == null) continue;

                if (_progressionManager.IsStageUnlocked(stage.StageId))
                    _stageSelectController.UnlockStage(i);

                int bestStars = _progressionManager.GetBestStars(stage.StageId);
                if (bestStars > 0)
                    _stageSelectController.SetStageStars(i, bestStars);
            }
        }
    }
}
