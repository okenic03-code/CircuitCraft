using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Defines additional constraints for a stage, such as component limits or forbidden areas.
    /// </summary>
    [Serializable]
    public class StageConstraints
    {
        [SerializeField]
        [Tooltip("Maximum number of components allowed on the board. 0 means no limit.")]
        [FormerlySerializedAs("maxComponentCount")]
        private int _maxComponentCount;

        /// <summary>Maximum number of components allowed on the board. 0 means no limit.</summary>
        public int MaxComponentCount => _maxComponentCount;

        // Note: Future constraints like forbidden zones or specific component type limits 
        // can be added here as the system evolves.
    }
}
