using System;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Manages runtime flow between StageSelect and GamePlay scenes.
    /// StageSelect scene: shows stage list and loads GamePlay on selection.
    /// GamePlay scene: loads selected stage and manages gameplay/ending flow.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField, Tooltip("Wire in Inspector: Stage Select screen root GameObject.")]
        private GameObject _stageSelectScreen;

        [SerializeField, Tooltip("Wire in Inspector: Gameplay screen root GameObject.")]
        private GameObject _gamePlayScreen;

        [SerializeField, Tooltip("Wire in Inspector: Ending screen root GameObject.")]
        private GameObject _endingScreen;

        [Header("Controllers")]
        [SerializeField, Tooltip("Wire in Inspector: Stage Select UI controller.")]
        private StageSelectController _stageSelectController;

        [SerializeField, Tooltip("Wire in Inspector: Ending screen controller.")]
        private EndingController _endingController;

        [SerializeField, Tooltip("Wire in Inspector: Stage manager instance in gameplay scene.")]
        private StageManager _stageManager;

        [SerializeField, Tooltip("Wire in Inspector: Progression manager for unlock and star data.")]
        private ProgressionManager _progressionManager;

        [SerializeField, Tooltip("Wire in Inspector: Component palette controller on gameplay HUD.")]
        private ComponentPaletteController _paletteController;

        [SerializeField, Tooltip("Wire in Inspector: Results panel controller for retry and next flow.")]
        private ResultsPanelController _resultsPanelController;

        [SerializeField, Tooltip("Wire in Inspector: Pause menu controller.")]
        private PauseMenuController _pauseMenuController;

        [Header("Stage Data")]
        [SerializeField, Tooltip("Ordered stage definitions matching StageSelectController's stage list.")]
        private StageDefinition[] _stages;

        private enum GameScreen
        {
            StageSelect,
            GamePlay,
            Ending
        }

        private bool _isStageSelectScene;

        private void Start()
        {
            _isStageSelectScene = string.Equals(
                SceneManager.GetActiveScene().name,
                SceneNames.StageSelect,
                StringComparison.Ordinal);

            if (_isStageSelectScene)
            {
                WireStageSelectEvents();
                SyncProgressionToStageSelect();
                ShowScreen(GameScreen.StageSelect);
                return;
            }

            WireEndingEvents();
            WireStageManagerEvents();
            WireResultsPanelEvents();
            WirePauseMenuEvents();

            LoadInitialGameplayStage();
            ShowScreen(GameScreen.GamePlay);
        }

        private void OnDestroy()
        {
            if (_isStageSelectScene)
            {
                UnwireStageSelectEvents();
                return;
            }

            UnwireEndingEvents();
            UnwireStageManagerEvents();
            UnwireResultsPanelEvents();
            UnwirePauseMenuEvents();
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

        private void HandleBackToMenu()
        {
            StageSelectionContext.Clear();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void HandleEndingBackToMenu()
        {
            StageSelectionContext.Clear();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private void HandleStageSelected(StageDefinition stage)
        {
            if (stage == null)
                return;

            StageSelectionContext.SetSelectedStage(stage);
            SceneManager.LoadScene(SceneNames.GamePlay);
        }

        private void HandleStageCompleted(ScoreBreakdown breakdown)
        {
            if (_progressionManager == null || _stageManager?.CurrentStage == null || breakdown == null)
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
            if (_paletteController != null && _stageManager?.CurrentStage != null)
            {
                _paletteController.SetAvailableComponents(_stageManager.CurrentStage.AllowedComponents);
            }
        }

        private void HandleRetry()
        {
            if (_stageManager?.CurrentStage != null)
            {
                _stageManager.LoadStage(_stageManager.CurrentStage);
            }
        }

        private void HandlePauseStageSelect()
        {
            SceneManager.LoadScene(SceneNames.StageSelect);
        }

        private void HandleNextStage()
        {
            if (_stages != null && _stageManager?.CurrentStage != null)
            {
                int currentIndex = Array.IndexOf(_stages, _stageManager.CurrentStage);
                if (currentIndex >= 0 && currentIndex < _stages.Length - 1)
                {
                    StageDefinition nextStage = _stages[currentIndex + 1];
                    _stageManager.LoadStage(nextStage);
                    StageSelectionContext.SetSelectedStage(nextStage);
                    return;
                }

                if (currentIndex == _stages.Length - 1)
                {
                    ShowEndingScreen();
                    return;
                }
            }

            SceneManager.LoadScene(SceneNames.StageSelect);
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
                    {
                        totalEarned += _progressionManager.GetBestStars(stage.StageId);
                    }
                }

                _endingController.SetTotalStars(totalEarned, totalMax);
            }

            ShowScreen(GameScreen.Ending);
        }

        private void ShowScreen(GameScreen screen)
        {
            if (_stageSelectScreen != null)
            {
                _stageSelectScreen.SetActive(screen == GameScreen.StageSelect);
            }

            if (_gamePlayScreen != null)
            {
                _gamePlayScreen.SetActive(screen == GameScreen.GamePlay);
            }

            if (_endingScreen != null)
            {
                _endingScreen.SetActive(screen == GameScreen.Ending);
            }

#if UNITY_EDITOR
            Debug.Log($"SceneFlowManager: Showing {screen}");
#endif
        }

        private void LoadInitialGameplayStage()
        {
            if (_stageManager == null)
                return;

            StageDefinition stageToLoad = StageSelectionContext.SelectedStage;

            if (stageToLoad == null && _stages != null && _stages.Length > 0)
            {
                stageToLoad = _stages[0];
            }

            if (stageToLoad == null)
            {
                Debug.LogWarning("SceneFlowManager: No stage available to load in GamePlay scene.");
                return;
            }

            _stageManager.LoadStage(stageToLoad);
            StageSelectionContext.SetSelectedStage(stageToLoad);
        }

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
                StageDefinition stage = _stages[i];
                if (stage == null) continue;

                if (_progressionManager.IsStageUnlocked(stage.StageId))
                {
                    _stageSelectController.UnlockStage(i);
                }

                int bestStars = _progressionManager.GetBestStars(stage.StageId);
                if (bestStars > 0)
                {
                    _stageSelectController.SetStageStars(i, bestStars);
                }
            }
        }
    }
}
