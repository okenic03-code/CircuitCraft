using UnityEngine;
using UnityEngine.Serialization;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Defines the data for a playable stage in the game.
    /// This ScriptableObject stores configuration such as grid size, allowed components, and win conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStage", menuName = "CircuitCraft/Stage Definition")]
    public class StageDefinition : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField]
        [Tooltip("Unique internal identifier for the stage (e.g., 'w1-s1').")]
        [FormerlySerializedAs("stageId")]
        private string _stageId;

        [SerializeField]
        [Tooltip("User-facing name of the stage.")]
        [FormerlySerializedAs("displayName")]
        private string _displayName;

        [SerializeField]
        [Tooltip("Identifier for the world this stage belongs to.")]
        [FormerlySerializedAs("worldId")]
        private string _worldId;

        [SerializeField]
        [Tooltip("The sequential number of the stage within its world.")]
        [FormerlySerializedAs("stageNumber")]
        private int _stageNumber;

        [Header("Grid Configuration")]
        [SerializeField]
        [Tooltip("The dimensions of the playable grid.")]
        [HideInInspector]
        [FormerlySerializedAs("gridSize")]
        private Vector2Int _gridSize;

        [Header("Footprint Target")]
        [SerializeField]
        [Tooltip("Target footprint area for scoring. 0 falls back to legacy grid area.")]
        private int _targetArea;

        [Header("Allowed Components")]
        [SerializeField]
        [Tooltip("The set of components the player is allowed to use in this stage.")]
        [FormerlySerializedAs("allowedComponents")]
        private ComponentDefinition[] _allowedComponents;

        [Header("Win Conditions & Simulation")]
        [SerializeField]
        [Tooltip("Test cases to verify the player's circuit against.")]
        [FormerlySerializedAs("testCases")]
        private StageTestCase[] _testCases;

        [SerializeField]
        [Tooltip("Maximum budget allowed for the circuit. 0 means no limit.")]
        [FormerlySerializedAs("budgetLimit")]
        private float _budgetLimit;

        /// <summary>Unique internal identifier for the stage.</summary>
        public string StageId => _stageId;

        /// <summary>User-facing name of the stage.</summary>
        public string DisplayName => _displayName;

        /// <summary>Identifier for the world this stage belongs to.</summary>
        public string WorldId => _worldId;

        /// <summary>The sequential number of the stage within its world.</summary>
        public int StageNumber => _stageNumber;

        /// <summary>Target footprint area used in scoring.</summary>
        public int TargetArea => _targetArea > 0 ? _targetArea : Mathf.Max(1, _gridSize.x * _gridSize.y);

        /// <summary>The set of components the player is allowed to use in this stage.</summary>
        public ComponentDefinition[] AllowedComponents => _allowedComponents;

        /// <summary>Test cases to verify the player's circuit against.</summary>
        public StageTestCase[] TestCases => _testCases;

        /// <summary>Maximum budget allowed for the circuit. 0 means no limit.</summary>
        public float BudgetLimit => _budgetLimit;

    }
}
