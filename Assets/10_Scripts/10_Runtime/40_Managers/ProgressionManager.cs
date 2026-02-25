using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Data;
using UnityEngine;

namespace CircuitCraft.Managers
{
    /// <summary>
    /// Tracks stage unlock state and best star ratings.
    /// Completing a stage automatically unlocks the next stage in the same world.
    /// Persistence (save/load) is handled separately by a future system.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField, Tooltip("Stage manager used to receive completion events for progression updates.")]
        private StageManager _stageManager;

        [Header("Stage Data")]
        [SerializeField]
        [Tooltip("All stages in the game, used to resolve next-stage lookups.")]
        private StageDefinition[] _allStages;

        private readonly Dictionary<string, bool> _unlockedStages = new();
        private readonly Dictionary<string, int> _bestStars = new();

        /// <summary>Raised when a stage becomes newly unlocked. Parameter is the stageId.</summary>
        public event Action<string> OnStageUnlocked;

        private void Awake()
        {
            InitializeDefaults();
            LoadProgress();
        }

        private void OnEnable()
        {
            if (_stageManager != null)
                _stageManager.OnStageCompleted += HandleStageCompleted;
        }

        private void OnDisable()
        {
            if (_stageManager != null)
                _stageManager.OnStageCompleted -= HandleStageCompleted;
        }

        /// <summary>
        /// Unlocks the first stage of each world by default.
        /// </summary>
        private void InitializeDefaults()
        {
            if (_allStages is null) return;

            foreach (var stage in _allStages)
            {
                if (stage == null) continue;
                if (stage.StageNumber == 1)
                    _unlockedStages[stage.StageId] = true;
            }
        }

        /// <summary>
        /// Handles the StageManager.OnStageCompleted event by recording
        /// the completion of the current stage with the earned stars.
        /// </summary>
        private void HandleStageCompleted(ScoreBreakdown breakdown)
        {
            if (breakdown is null) return;

            var currentStage = _stageManager.CurrentStage;
            if (currentStage == null)
            {
                Debug.LogWarning("ProgressionManager: OnStageCompleted fired but CurrentStage is null.");
                return;
            }

            // Only record progression if the player actually passed
            if (!breakdown.Passed) return;

            RecordStageCompletion(currentStage.StageId, breakdown.Stars);
        }

        /// <summary>
        /// Records a stage completion: updates the best star rating (if improved)
        /// and unlocks the next stage in the same world.
        /// </summary>
        /// <param name="stageId">The unique stage identifier.</param>
        /// <param name="stars">Star rating earned (0-3).</param>
        public void RecordStageCompletion(string stageId, int stars)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogWarning("ProgressionManager: Cannot record completion for null/empty stageId.");
                return;
            }

            // Update best stars if this attempt is better
            if (!_bestStars.TryGetValue(stageId, out int previousBest) || stars > previousBest)
            {
                _bestStars[stageId] = stars;
            }

            // Mark the completed stage as unlocked (in case it wasn't already)
            _unlockedStages[stageId] = true;

            // Find and unlock the next stage in the same world
            var nextStage = FindNextStage(stageId);
            if (nextStage != null)
            {
                UnlockStage(nextStage.StageId);
            }

            // Save progress after recording completion
            SaveProgress();
        }

        /// <summary>
        /// Returns whether the specified stage is unlocked.
        /// </summary>
        /// <param name="stageId">The unique stage identifier.</param>
        /// <returns>True if the stage is unlocked, false otherwise.</returns>
        public bool IsStageUnlocked(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return false;
            return _unlockedStages.TryGetValue(stageId, out bool unlocked) && unlocked;
        }

        /// <summary>
        /// Returns the best star rating achieved for the specified stage.
        /// </summary>
        /// <param name="stageId">The unique stage identifier.</param>
        /// <returns>Best star count (0 if never completed).</returns>
        public int GetBestStars(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return 0;
            return _bestStars.TryGetValue(stageId, out int stars) ? stars : 0;
        }

        /// <summary>
        /// Manually unlocks a stage. Fires OnStageUnlocked if the stage was not already unlocked.
        /// </summary>
        /// <param name="stageId">The unique stage identifier to unlock.</param>
        public void UnlockStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogWarning("ProgressionManager: Cannot unlock null/empty stageId.");
                return;
            }

            if (_unlockedStages.TryGetValue(stageId, out bool alreadyUnlocked) && alreadyUnlocked)
                return;

            _unlockedStages[stageId] = true;
            Debug.Log($"ProgressionManager: Stage '{stageId}' unlocked.");
            OnStageUnlocked?.Invoke(stageId);
        }

        /// <summary>
        /// Finds the next stage in the same world (stageNumber + 1).
        /// </summary>
        /// <param name="currentStageId">The stageId of the current stage.</param>
        /// <returns>The next StageDefinition, or null if there is no next stage.</returns>
        private StageDefinition FindNextStage(string currentStageId)
        {
            if (_allStages is null) return null;

            // First, find the current stage definition to get worldId and stageNumber
            StageDefinition current = null;
            foreach (var stage in _allStages)
            {
                if (stage != null && stage.StageId == currentStageId)
                {
                    current = stage;
                    break;
                }
            }

            if (current == null) return null;

            // Find the stage in the same world with stageNumber + 1
            int nextNumber = current.StageNumber + 1;
            string worldId = current.WorldId;

            foreach (var stage in _allStages)
            {
                if (stage != null && stage.WorldId == worldId && stage.StageNumber == nextNumber)
                    return stage;
            }

            return null;
        }

        /// <summary>
        /// Saves all progression data (unlock states and star ratings) to PlayerPrefs.
        /// </summary>
        public void SaveProgress()
        {
            if (_allStages is null) return;

            foreach (var stage in _allStages)
            {
                if (stage == null) continue;

                string stageId = stage.StageId;

                // Save unlock state
                bool isUnlocked = _unlockedStages.TryGetValue(stageId, out bool unlocked) && unlocked;
                PlayerPrefs.SetInt($"progress_unlock_{stageId}", isUnlocked ? 1 : 0);

                // Save best stars
                int bestStars = _bestStars.TryGetValue(stageId, out int stars) ? stars : 0;
                PlayerPrefs.SetInt($"progress_stars_{stageId}", bestStars);
            }

            PlayerPrefs.Save();
            Debug.Log("ProgressionManager: Progress saved.");
        }

        /// <summary>
        /// Loads all progression data from PlayerPrefs and restores dictionaries.
        /// </summary>
        public void LoadProgress()
        {
            if (_allStages is null) return;

            foreach (var stage in _allStages)
            {
                if (stage == null) continue;

                string stageId = stage.StageId;

                // Load unlock state
                string unlockKey = $"progress_unlock_{stageId}";
                if (PlayerPrefs.HasKey(unlockKey))
                {
                    bool isUnlocked = PlayerPrefs.GetInt(unlockKey) == 1;
                    _unlockedStages[stageId] = isUnlocked;
                }

                // Load best stars
                string starsKey = $"progress_stars_{stageId}";
                if (PlayerPrefs.HasKey(starsKey))
                {
                    int bestStars = PlayerPrefs.GetInt(starsKey);
                    _bestStars[stageId] = bestStars;
                }
            }

            Debug.Log("ProgressionManager: Progress loaded.");
        }

        /// <summary>
        /// Clears all saved progression data from PlayerPrefs (for testing/reset).
        /// </summary>
        public void ClearProgress()
        {
            if (_allStages is null) return;

            foreach (var stage in _allStages)
            {
                if (stage == null) continue;

                string stageId = stage.StageId;
                PlayerPrefs.DeleteKey($"progress_unlock_{stageId}");
                PlayerPrefs.DeleteKey($"progress_stars_{stageId}");
            }

            PlayerPrefs.Save();
            Debug.Log("ProgressionManager: Progress cleared.");
        }
    }
}
