using UnityEngine;

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
        private string stageId;

        [SerializeField]
        [Tooltip("User-facing name of the stage.")]
        private string displayName;

        [SerializeField]
        [Tooltip("Identifier for the world this stage belongs to.")]
        private string worldId;

        [SerializeField]
        [Tooltip("The sequential number of the stage within its world.")]
        private int stageNumber;

        [Header("Grid Configuration")]
        [SerializeField]
        [Tooltip("The dimensions of the playable grid.")]
        private Vector2Int gridSize;

        [Header("Allowed Components")]
        [SerializeField]
        [Tooltip("The set of components the player is allowed to use in this stage.")]
        private ComponentDefinition[] allowedComponents;

        [Header("Win Conditions & Simulation")]
        [SerializeField]
        [Tooltip("Test cases to verify the player's circuit against.")]
        private StageTestCase[] testCases;

        [SerializeField]
        [Tooltip("Maximum budget allowed for the circuit. 0 means no limit.")]
        private float budgetLimit;

        [Header("Additional Constraints")]
        [SerializeField]
        [Tooltip("Extra constraints for this stage.")]
        private StageConstraints constraints;

        /// <summary>Unique internal identifier for the stage.</summary>
        public string StageId => stageId;

        /// <summary>User-facing name of the stage.</summary>
        public string DisplayName => displayName;

        /// <summary>Identifier for the world this stage belongs to.</summary>
        public string WorldId => worldId;

        /// <summary>The sequential number of the stage within its world.</summary>
        public int StageNumber => stageNumber;

        /// <summary>The dimensions of the playable grid.</summary>
        public Vector2Int GridSize => gridSize;

        /// <summary>The set of components the player is allowed to use in this stage.</summary>
        public ComponentDefinition[] AllowedComponents => allowedComponents;

        /// <summary>Test cases to verify the player's circuit against.</summary>
        public StageTestCase[] TestCases => testCases;

        /// <summary>Maximum budget allowed for the circuit. 0 means no limit.</summary>
        public float BudgetLimit => budgetLimit;

        /// <summary>Extra constraints for this stage.</summary>
        public StageConstraints Constraints => constraints;
    }
}
