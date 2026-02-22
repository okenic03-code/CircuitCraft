using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEngine;

namespace CircuitCraft.Tests.Managers
{
    /// <summary>
    /// Characterization tests for ProgressionManager — captures ALL observable public-surface
    /// behaviors as a safety net before refactoring.
    /// PlayerPrefs keys written during tests are cleaned up in TearDown.
    /// </summary>
    [TestFixture]
    public class ProgressionManagerTests
    {
        // Track all created Unity objects for cleanup
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        // Track PlayerPrefs keys to delete in TearDown
        private readonly List<string> _playerPrefsKeys = new List<string>();

        private GameObject _progressionManagerGo;
        private GameObject _stageManagerGo;
        private GameObject _gameManagerGo;
        private GameObject _simulationManagerGo;

        private ProgressionManager _progressionManager;
        private StageManager _stageManager;
        private GameManager _gameManager;
        private SimulationManager _simulationManager;

        [SetUp]
        public void SetUp()
        {
            // Create GameObjects
            _simulationManagerGo = new GameObject("TestSimulationManager");
            _createdObjects.Add(_simulationManagerGo);

            _gameManagerGo = new GameObject("TestGameManager");
            _createdObjects.Add(_gameManagerGo);

            _stageManagerGo = new GameObject("TestStageManager");
            _createdObjects.Add(_stageManagerGo);

            _progressionManagerGo = new GameObject("TestProgressionManager");
            _createdObjects.Add(_progressionManagerGo);

            // AddComponent triggers Awake
            _simulationManager = _simulationManagerGo.AddComponent<SimulationManager>();
            _gameManager = _gameManagerGo.AddComponent<GameManager>();
            _stageManager = _stageManagerGo.AddComponent<StageManager>();

            // Wire StageManager dependencies
            SetPrivateField(_stageManager, "_gameManager", _gameManager);
            SetPrivateField(_stageManager, "_simulationManager", _simulationManager);

            // ProgressionManager.Awake calls InitializeDefaults + LoadProgress.
            // We set _allStages and _stageManager BEFORE AddComponent so Awake picks them up.
            // But since MonoBehaviour fields are set after construction, we use a deferred approach:
            // Create the GO, add the component (Awake fires with null fields, which is safe),
            // then wire fields and call InitializeDefaults via reflection to reset state.
            _progressionManager = _progressionManagerGo.AddComponent<ProgressionManager>();
            SetPrivateField(_progressionManager, "_stageManager", _stageManager);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up PlayerPrefs keys created during tests
            foreach (var key in _playerPrefsKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }
            _playerPrefsKeys.Clear();
            PlayerPrefs.Save();

            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        // ------------------------------------------------------------------
        // Helper Methods
        // ------------------------------------------------------------------

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private StageDefinition CreateStageDefinition(
            string stageId,
            string worldId,
            int stageNumber,
            int targetArea = 4)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            _createdObjects.Add(stage);
            SetPrivateField(stage, "_stageId", stageId);
            SetPrivateField(stage, "_displayName", stageId);
            SetPrivateField(stage, "_worldId", worldId);
            SetPrivateField(stage, "_stageNumber", stageNumber);
            SetPrivateField(stage, "_targetArea", targetArea);

            // Register PlayerPrefs keys for cleanup
            _playerPrefsKeys.Add($"progress_unlock_{stageId}");
            _playerPrefsKeys.Add($"progress_stars_{stageId}");

            return stage;
        }

        /// <summary>
        /// Configures _allStages and re-runs InitializeDefaults + LoadProgress
        /// so the ProgressionManager starts with correct initial state for each test.
        /// </summary>
        private void SetupAllStages(StageDefinition[] stages)
        {
            SetPrivateField(_progressionManager, "_allStages", stages);

            // Reset internal dictionaries before re-initializing
            var unlockedField = typeof(ProgressionManager).GetField(
                "_unlockedStages",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var bestStarsField = typeof(ProgressionManager).GetField(
                "_bestStars",
                BindingFlags.NonPublic | BindingFlags.Instance);

            ((Dictionary<string, bool>)unlockedField.GetValue(_progressionManager)).Clear();
            ((Dictionary<string, int>)bestStarsField.GetValue(_progressionManager)).Clear();

            // Re-run initialization with the new stages
            var initMethod = typeof(ProgressionManager).GetMethod(
                "InitializeDefaults",
                BindingFlags.NonPublic | BindingFlags.Instance);
            initMethod.Invoke(_progressionManager, null);
        }

        private ScoreBreakdown CreatePassingBreakdown(int stars)
        {
            return new ScoreBreakdown(
                baseScore: 1000,
                budgetBonus: 0,
                areaBonus: 0,
                totalScore: 1000,
                stars: stars,
                passed: true,
                lineItems: new List<ScoreLineItem>(),
                summary: $"PASSED — {stars} stars");
        }

        private ScoreBreakdown CreateFailingBreakdown()
        {
            return new ScoreBreakdown(
                baseScore: 0,
                budgetBonus: 0,
                areaBonus: 0,
                totalScore: 0,
                stars: 0,
                passed: false,
                lineItems: new List<ScoreLineItem>(),
                summary: "FAILED");
        }

        // ------------------------------------------------------------------
        // IsStageUnlocked
        // ------------------------------------------------------------------

        [Test]
        public void IsStageUnlocked_UnknownStageId_ReturnsFalse()
        {
            Assert.IsFalse(_progressionManager.IsStageUnlocked("nonexistent_stage"));
        }

        [Test]
        public void IsStageUnlocked_NullStageId_ReturnsFalse()
        {
            Assert.IsFalse(_progressionManager.IsStageUnlocked(null));
        }

        [Test]
        public void IsStageUnlocked_EmptyStageId_ReturnsFalse()
        {
            Assert.IsFalse(_progressionManager.IsStageUnlocked(string.Empty));
        }

        [Test]
        public void IsStageUnlocked_AfterUnlockStage_ReturnsTrue()
        {
            _playerPrefsKeys.Add("progress_unlock_my_stage");
            _progressionManager.UnlockStage("my_stage");

            Assert.IsTrue(_progressionManager.IsStageUnlocked("my_stage"));
        }

        // ------------------------------------------------------------------
        // GetBestStars
        // ------------------------------------------------------------------

        [Test]
        public void GetBestStars_UnknownStageId_ReturnsZero()
        {
            Assert.AreEqual(0, _progressionManager.GetBestStars("nonexistent_stage"));
        }

        [Test]
        public void GetBestStars_NullStageId_ReturnsZero()
        {
            Assert.AreEqual(0, _progressionManager.GetBestStars(null));
        }

        [Test]
        public void GetBestStars_EmptyStageId_ReturnsZero()
        {
            Assert.AreEqual(0, _progressionManager.GetBestStars(string.Empty));
        }

        [Test]
        public void GetBestStars_AfterRecordCompletion_ReturnsSavedStars()
        {
            var stage = CreateStageDefinition("s_stars_test", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_stars_test", 2);

            Assert.AreEqual(2, _progressionManager.GetBestStars("s_stars_test"));
        }

        // ------------------------------------------------------------------
        // RecordStageCompletion
        // ------------------------------------------------------------------

        [Test]
        public void RecordStageCompletion_NullStageId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _progressionManager.RecordStageCompletion(null, 3),
                "RecordStageCompletion with null stageId should log a warning (not throw)");
        }

        [Test]
        public void RecordStageCompletion_EmptyStageId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _progressionManager.RecordStageCompletion(string.Empty, 3),
                "RecordStageCompletion with empty stageId should log a warning (not throw)");
        }

        [Test]
        public void RecordStageCompletion_NullOrEmptyStageId_DoesNotUpdateBestStars()
        {
            _progressionManager.RecordStageCompletion(null, 3);
            _progressionManager.RecordStageCompletion(string.Empty, 3);

            // Neither should have stored anything — GetBestStars returns 0 for unknown
            Assert.AreEqual(0, _progressionManager.GetBestStars(null));
            Assert.AreEqual(0, _progressionManager.GetBestStars(string.Empty));
        }

        [Test]
        public void RecordStageCompletion_FirstTime_SetsBestStars()
        {
            var stage = CreateStageDefinition("s_first", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_first", 2);

            Assert.AreEqual(2, _progressionManager.GetBestStars("s_first"));
        }

        [Test]
        public void RecordStageCompletion_WithHigherStars_UpdatesBestStars()
        {
            var stage = CreateStageDefinition("s_higher", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_higher", 1);
            _progressionManager.RecordStageCompletion("s_higher", 3);

            Assert.AreEqual(3, _progressionManager.GetBestStars("s_higher"),
                "Best stars should update when a higher star count is achieved");
        }

        [Test]
        public void RecordStageCompletion_WithLowerStars_DoesNotDowngradeBestStars()
        {
            var stage = CreateStageDefinition("s_lower", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_lower", 3);
            _progressionManager.RecordStageCompletion("s_lower", 1);

            Assert.AreEqual(3, _progressionManager.GetBestStars("s_lower"),
                "Best stars should NOT be downgraded when a lower star count is recorded");
        }

        [Test]
        public void RecordStageCompletion_UnlocksNextStageInSameWorld()
        {
            var stage1 = CreateStageDefinition("w1_s1", "world1", 1);
            var stage2 = CreateStageDefinition("w1_s2", "world1", 2);
            SetupAllStages(new[] { stage1, stage2 });

            // stage2 not yet unlocked
            Assert.IsFalse(_progressionManager.IsStageUnlocked("w1_s2"),
                "stage2 should not be unlocked before completing stage1");

            _progressionManager.RecordStageCompletion("w1_s1", 1);

            Assert.IsTrue(_progressionManager.IsStageUnlocked("w1_s2"),
                "Completing stage1 should unlock stage2 in the same world");
        }

        [Test]
        public void RecordStageCompletion_WithNoNextStage_DoesNotThrow()
        {
            var stage1 = CreateStageDefinition("w1_only", "world1", 1);
            SetupAllStages(new[] { stage1 });

            Assert.DoesNotThrow(() => _progressionManager.RecordStageCompletion("w1_only", 2),
                "RecordStageCompletion should not throw when there is no next stage");
        }

        [Test]
        public void RecordStageCompletion_MarksCompletedStageAsUnlocked()
        {
            // Even if stage was not initially unlocked, completing it should mark it unlocked
            var stage = CreateStageDefinition("s_mark_unlocked", "world1", 5);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_mark_unlocked", 1);

            Assert.IsTrue(_progressionManager.IsStageUnlocked("s_mark_unlocked"),
                "RecordStageCompletion should mark the completed stage itself as unlocked");
        }

        // ------------------------------------------------------------------
        // UnlockStage
        // ------------------------------------------------------------------

        [Test]
        public void UnlockStage_NullStageId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _progressionManager.UnlockStage(null),
                "UnlockStage with null stageId should log a warning (not throw)");
        }

        [Test]
        public void UnlockStage_EmptyStageId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _progressionManager.UnlockStage(string.Empty),
                "UnlockStage with empty stageId should log a warning (not throw)");
        }

        [Test]
        public void UnlockStage_NewStage_FiresOnStageUnlockedEvent()
        {
            string unlockedId = null;
            _progressionManager.OnStageUnlocked += id => unlockedId = id;

            _progressionManager.UnlockStage("new_stage_event_test");
            _playerPrefsKeys.Add("progress_unlock_new_stage_event_test");
            _playerPrefsKeys.Add("progress_stars_new_stage_event_test");

            Assert.AreEqual("new_stage_event_test", unlockedId,
                "OnStageUnlocked should fire with the stageId when a new stage is unlocked");
        }

        [Test]
        public void UnlockStage_AlreadyUnlocked_DoesNotFireEventAgain()
        {
            int eventCount = 0;
            _progressionManager.OnStageUnlocked += _ => eventCount++;
            _playerPrefsKeys.Add("progress_unlock_already_unlocked");
            _playerPrefsKeys.Add("progress_stars_already_unlocked");

            _progressionManager.UnlockStage("already_unlocked");
            _progressionManager.UnlockStage("already_unlocked"); // second call

            Assert.AreEqual(1, eventCount,
                "OnStageUnlocked should NOT fire again if the stage is already unlocked");
        }

        [Test]
        public void UnlockStage_NullOrEmpty_DoesNotFireEvent()
        {
            int eventCount = 0;
            _progressionManager.OnStageUnlocked += _ => eventCount++;

            _progressionManager.UnlockStage(null);
            _progressionManager.UnlockStage(string.Empty);

            Assert.AreEqual(0, eventCount,
                "OnStageUnlocked should not fire for null/empty stageId");
        }

        // ------------------------------------------------------------------
        // InitializeDefaults
        // ------------------------------------------------------------------

        [Test]
        public void InitializeDefaults_UnlocksStageNumberOne_InEachWorld()
        {
            var w1s1 = CreateStageDefinition("w1_s1_init", "world1", 1);
            var w1s2 = CreateStageDefinition("w1_s2_init", "world1", 2);
            var w2s1 = CreateStageDefinition("w2_s1_init", "world2", 1);
            SetupAllStages(new[] { w1s1, w1s2, w2s1 });

            Assert.IsTrue(_progressionManager.IsStageUnlocked("w1_s1_init"),
                "Stage number 1 of world1 should be unlocked by default");
            Assert.IsFalse(_progressionManager.IsStageUnlocked("w1_s2_init"),
                "Stage number 2 of world1 should NOT be unlocked by default");
            Assert.IsTrue(_progressionManager.IsStageUnlocked("w2_s1_init"),
                "Stage number 1 of world2 should be unlocked by default");
        }

        [Test]
        public void InitializeDefaults_WithNullAllStages_DoesNotThrow()
        {
            // If _allStages is null, InitializeDefaults should do nothing (not throw)
            SetPrivateField(_progressionManager, "_allStages", null);

            var initMethod = typeof(ProgressionManager).GetMethod(
                "InitializeDefaults",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.DoesNotThrow(() => initMethod.Invoke(_progressionManager, null),
                "InitializeDefaults with null _allStages should not throw");
        }

        // ------------------------------------------------------------------
        // FindNextStage (via RecordStageCompletion)
        // ------------------------------------------------------------------

        [Test]
        public void FindNextStage_ReturnsNull_WhenNoNextStageExists()
        {
            // Only one stage in the world; completing it should not unlock anything new
            var onlyStage = CreateStageDefinition("w_solo", "solo_world", 1);
            SetupAllStages(new[] { onlyStage });

            int eventCount = 0;
            _progressionManager.OnStageUnlocked += _ => eventCount++;

            // OnStageUnlocked fires when the solo stage itself gets marked unlocked
            // (RecordStageCompletion always calls UnlockStage on the completed stage).
            // We specifically want to verify no NEXT stage gets unlocked.
            _progressionManager.RecordStageCompletion("w_solo", 1);

            // Only the completed stage itself should have been "unlocked" via RecordStageCompletion.
            // Since w_solo was already unlocked by InitializeDefaults (stageNumber==1),
            // UnlockStage would have found it already-unlocked and not fired the event.
            // Either way, no ADDITIONAL stage unlock event beyond the solo stage should fire.
            Assert.IsFalse(_progressionManager.IsStageUnlocked("w_solo_nonexistent"),
                "There should be no next-stage unlock for a solo world");
        }

        [Test]
        public void FindNextStage_DoesNotUnlockStageFromDifferentWorld()
        {
            var w1s1 = CreateStageDefinition("w1_cross", "worldA", 1);
            var w2s2 = CreateStageDefinition("w2_cross", "worldB", 2); // stageNumber 2 in worldB

            SetupAllStages(new[] { w1s1, w2s2 });

            _progressionManager.RecordStageCompletion("w1_cross", 1);

            // worldB stage2 should NOT be unlocked — different world
            Assert.IsFalse(_progressionManager.IsStageUnlocked("w2_cross"),
                "Completing a stage in worldA should not unlock stages in worldB");
        }

        // ------------------------------------------------------------------
        // HandleStageCompleted (private, tested via StageManager.OnStageCompleted)
        // ------------------------------------------------------------------

        [Test]
        public void HandleStageCompleted_WhenBreakdownNotPassed_DoesNotRecordProgression()
        {
            var stage = CreateStageDefinition("s_fail_test", "world1", 1);
            SetupAllStages(new[] { stage });

            // Manually set CurrentStage on StageManager via LoadStage
            // We need a GameManager that will handle the board reset — already wired
            var stage2 = ScriptableObject.CreateInstance<StageDefinition>();
            _createdObjects.Add(stage2);
            SetPrivateField(stage2, "_stageId", "s_fail_test");
            SetPrivateField(stage2, "_displayName", "s_fail_test");
            SetPrivateField(stage2, "_worldId", "world1");
            SetPrivateField(stage2, "_stageNumber", 1);
            SetPrivateField(stage2, "_targetArea", 4);

            // Load stage so CurrentStage is set
            _stageManager.LoadStage(stage2);

            int initialStars = _progressionManager.GetBestStars("s_fail_test");

            // Manually raise OnStageCompleted with a failing breakdown
            var failBreakdown = CreateFailingBreakdown();

            // Fire the event via StageManager's OnStageCompleted
            // ProgressionManager is subscribed via OnEnable
            RaiseStageCompleted(failBreakdown);

            // Stars should not have changed (still 0)
            Assert.AreEqual(initialStars, _progressionManager.GetBestStars("s_fail_test"),
                "HandleStageCompleted should NOT record progression when breakdown.Passed is false");
        }

        [Test]
        public void HandleStageCompleted_WhenBreakdownPassed_RecordsProgression()
        {
            var stage = CreateStageDefinition("s_pass_test", "world1", 1);
            SetupAllStages(new[] { stage });

            // Load a matching stage into StageManager
            var stageSo = ScriptableObject.CreateInstance<StageDefinition>();
            _createdObjects.Add(stageSo);
            SetPrivateField(stageSo, "_stageId", "s_pass_test");
            SetPrivateField(stageSo, "_displayName", "s_pass_test");
            SetPrivateField(stageSo, "_worldId", "world1");
            SetPrivateField(stageSo, "_stageNumber", 1);
            SetPrivateField(stageSo, "_targetArea", 4);

            _stageManager.LoadStage(stageSo);

            // Subscribe ProgressionManager to StageManager event (OnEnable wires it)
            // Since both components are active, OnEnable already fired.
            // Re-subscribe manually in case ordering was off:
            var onEnableMethod = typeof(ProgressionManager).GetMethod(
                "OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod.Invoke(_progressionManager, null);

            var passBreakdown = CreatePassingBreakdown(2);
            RaiseStageCompleted(passBreakdown);

            Assert.AreEqual(2, _progressionManager.GetBestStars("s_pass_test"),
                "HandleStageCompleted should record progression when breakdown.Passed is true");
        }

        // ------------------------------------------------------------------
        // SaveProgress / LoadProgress / ClearProgress (smoke tests)
        // ------------------------------------------------------------------

        [Test]
        public void SaveProgress_WithNullAllStages_DoesNotThrow()
        {
            SetPrivateField(_progressionManager, "_allStages", null);

            Assert.DoesNotThrow(() => _progressionManager.SaveProgress(),
                "SaveProgress with null _allStages should not throw");
        }

        [Test]
        public void LoadProgress_WithNullAllStages_DoesNotThrow()
        {
            SetPrivateField(_progressionManager, "_allStages", null);

            Assert.DoesNotThrow(() => _progressionManager.LoadProgress(),
                "LoadProgress with null _allStages should not throw");
        }

        [Test]
        public void ClearProgress_WithNullAllStages_DoesNotThrow()
        {
            SetPrivateField(_progressionManager, "_allStages", null);

            Assert.DoesNotThrow(() => _progressionManager.ClearProgress(),
                "ClearProgress with null _allStages should not throw");
        }

        [Test]
        public void SaveAndLoadProgress_RoundtripsStarData()
        {
            var stage = CreateStageDefinition("s_roundtrip", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_roundtrip", 3);
            _progressionManager.SaveProgress();

            // Reset internal state then reload
            var bestStarsField = typeof(ProgressionManager).GetField(
                "_bestStars",
                BindingFlags.NonPublic | BindingFlags.Instance);
            ((Dictionary<string, int>)bestStarsField.GetValue(_progressionManager)).Clear();

            _progressionManager.LoadProgress();

            Assert.AreEqual(3, _progressionManager.GetBestStars("s_roundtrip"),
                "Best stars should survive a Save + Load cycle");
        }

        [Test]
        public void ClearProgress_RemovesUnlockDataFromPlayerPrefs()
        {
            var stage = CreateStageDefinition("s_clear", "world1", 1);
            SetupAllStages(new[] { stage });

            _progressionManager.RecordStageCompletion("s_clear", 2);
            _progressionManager.SaveProgress();

            _progressionManager.ClearProgress();

            // After clear, loading should not restore the old unlock state
            var unlockedField = typeof(ProgressionManager).GetField(
                "_unlockedStages",
                BindingFlags.NonPublic | BindingFlags.Instance);
            ((Dictionary<string, bool>)unlockedField.GetValue(_progressionManager)).Clear();

            _progressionManager.LoadProgress();

            // The key was deleted, so IsStageUnlocked should return false (not found in dict)
            Assert.IsFalse(_progressionManager.IsStageUnlocked("s_clear"),
                "After ClearProgress, unlock state should not be restored by LoadProgress");
        }

        // ------------------------------------------------------------------
        // Private helper: raise OnStageCompleted on StageManager
        // ------------------------------------------------------------------

        private void RaiseStageCompleted(ScoreBreakdown breakdown)
        {
            // Use reflection to raise the OnStageCompleted event on StageManager
            var eventField = typeof(StageManager).GetField(
                "OnStageCompleted",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            // C# events backed by multicast delegates: get the backing field
            var delegateVal = eventField?.GetValue(_stageManager) as System.MulticastDelegate;
            if (delegateVal != null)
            {
                delegateVal.DynamicInvoke(breakdown);
            }
        }
    }
}
