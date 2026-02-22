using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Manages screen transitions in the gameplay scene.
    /// Navigates between StageSelect → GamePlay → Ending by
    /// enabling/disabling screen root GameObjects.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private GameObject _stageSelectScreen;
        [SerializeField] private GameObject _gamePlayScreen;
        [SerializeField] private GameObject _endingScreen;

        [Header("Controllers")]
        [SerializeField] private StageSelectController _stageSelectController;
        [SerializeField] private EndingController _endingController;
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private ProgressionManager _progressionManager;
        [SerializeField] private ComponentPaletteController _paletteController;
        [SerializeField] private ResultsPanelController _resultsPanelController;
        [SerializeField] private PauseMenuController _pauseMenuController;

        [Header("Stage Data")]
        [SerializeField]
        [Tooltip("Ordered stage definitions matching StageSelectController's stage list.")]
        private StageDefinition[] _stages;

        private enum GameScreen { StageSelect, GamePlay, Ending }

        private void Start()
        {
            ResolveSceneReferences();
            WireStageSelectEvents();
            WireEndingEvents();
            WireStageManagerEvents();
            WireResultsPanelEvents();
            WirePauseMenuEvents();

            SyncProgressionToStageSelect();
            ShowScreen(GameScreen.StageSelect);
        }

        private void OnDestroy()
        {
            UnwireStageSelectEvents();
            UnwireEndingEvents();
            UnwireStageManagerEvents();
            UnwireResultsPanelEvents();
            UnwirePauseMenuEvents();
        }

        // ─── Wiring ───────────────────────────────────────────────────────

        private void ResolveSceneReferences()
        {
            if (_stageSelectController == null)
                _stageSelectController = FindFirstObjectByType<StageSelectController>(FindObjectsInactive.Include);

            if (_endingController == null)
                _endingController = FindFirstObjectByType<EndingController>(FindObjectsInactive.Include);

            if (_stageManager == null)
                _stageManager = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);

            if (_progressionManager == null)
                _progressionManager = FindFirstObjectByType<ProgressionManager>(FindObjectsInactive.Include);

            if (_paletteController == null)
                _paletteController = FindFirstObjectByType<ComponentPaletteController>(FindObjectsInactive.Include);

            if (_resultsPanelController == null)
                _resultsPanelController = FindFirstObjectByType<ResultsPanelController>(FindObjectsInactive.Include);

            if (_pauseMenuController == null)
                _pauseMenuController = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);

            // Auto-discover screen GameObjects from controllers
            if (_stageSelectScreen == null && _stageSelectController != null)
                _stageSelectScreen = _stageSelectController.gameObject;

            if (_endingScreen == null && _endingController != null)
                _endingScreen = _endingController.gameObject;
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

        private void WireEndingEvents()
        {
            if (_endingController == null) return;
            _endingController.OnBackToMenu += HandleEndingBackToMenu;
        }

        private void UnwireEndingEvents()
        {
            if (_endingController == null) return;
            _endingController.OnBackToMenu -= HandleEndingBackToMenu;
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

        private void WireResultsPanelEvents()
        {
            if (_resultsPanelController == null) return;
            _resultsPanelController.OnRetryRequested += HandleRetry;
            _resultsPanelController.OnNextStageRequested += HandleNextStage;
        }

        private void UnwireResultsPanelEvents()
        {
            if (_resultsPanelController == null) return;
            _resultsPanelController.OnRetryRequested -= HandleRetry;
            _resultsPanelController.OnNextStageRequested -= HandleNextStage;
        }

        private void WirePauseMenuEvents()
        {
            if (_pauseMenuController == null) return;
            _pauseMenuController.OnRestartRequested += HandleRetry;
            _pauseMenuController.OnStageSelectRequested += HandlePauseStageSelect;
        }

        private void UnwirePauseMenuEvents()
        {
            if (_pauseMenuController == null) return;
            _pauseMenuController.OnRestartRequested -= HandleRetry;
            _pauseMenuController.OnStageSelectRequested -= HandlePauseStageSelect;
        }

        // ─── Event Handlers ───────────────────────────────────────────────

        private void HandleBackToMenu()
        {
            SceneManager.LoadScene(0);
        }

        private void HandleEndingBackToMenu()
        {
            SceneManager.LoadScene(0);
        }

        private void HandleStageSelected(StageDefinition stage)
        {
            if (_stageManager == null || stage == null)
                return;

            _stageManager.LoadStage(stage);
            ShowScreen(GameScreen.GamePlay);
        }

        private void HandleStageCompleted(ScoreBreakdown breakdown)
        {
            if (_progressionManager == null || _stageManager.CurrentStage == null || breakdown == null)
                return;

            if (breakdown.Stars <= 0)
                return;

            string stageId = _stageManager.CurrentStage.StageId;
            if (_progressionManager.GetBestStars(stageId) >= breakdown.Stars)
                return;

            _progressionManager.RecordStageCompletion(stageId, breakdown.Stars);
        }

        private void HandleStageLoaded()
        {
            if (_paletteController != null && _stageManager.CurrentStage != null)
            {
                _paletteController.SetAvailableComponents(_stageManager.CurrentStage.AllowedComponents);
            }
        }

        private void HandleRetry()
        {
            if (_stageManager.CurrentStage != null)
                _stageManager.LoadStage(_stageManager.CurrentStage);
        }

        private void HandlePauseStageSelect()
        {
            SyncProgressionToStageSelect();
            ShowScreen(GameScreen.StageSelect);
        }

        private void HandleNextStage()
        {
            if (_stages != null && _stageManager.CurrentStage != null)
            {
                int currentIndex = System.Array.IndexOf(_stages, _stageManager.CurrentStage);
                if (currentIndex >= 0 && currentIndex < _stages.Length - 1)
                {
                    // Not the last stage: advance to next stage
                    _stageManager.LoadStage(_stages[currentIndex + 1]);
                    return;
                }

                // Last stage completed → show Ending screen
                if (currentIndex == _stages.Length - 1)
                {
                    ShowEndingScreen();
                    return;
                }
            }

            SyncProgressionToStageSelect();
            ShowScreen(GameScreen.StageSelect);
        }

        private void ShowEndingScreen()
        {
            if (_endingController != null && _progressionManager != null && _stages != null)
            {
                int totalEarned = 0;
                int totalMax = _stages.Length * 3;
                foreach (var stage in _stages)
                {
                    if (stage != null)
                        totalEarned += _progressionManager.GetBestStars(stage.StageId);
                }
                _endingController.SetTotalStars(totalEarned, totalMax);
            }
            ShowScreen(GameScreen.Ending);
        }

        // ─── Screen Management ────────────────────────────────────────────

        private void ShowScreen(GameScreen screen)
        {
            if (_stageSelectScreen != null)
                _stageSelectScreen.SetActive(screen == GameScreen.StageSelect);

            if (_gamePlayScreen != null)
                _gamePlayScreen.SetActive(screen == GameScreen.GamePlay);

            if (_endingScreen != null)
                _endingScreen.SetActive(screen == GameScreen.Ending);

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
