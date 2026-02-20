using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Defines the physical footprint of a component on the grid.
    /// </summary>
    [Serializable]
    public class FootprintDefinition
    {
        [SerializeField]
        [Tooltip("Size of the component in grid cells (width, height).")]
        [FormerlySerializedAs("size")]
        private Vector2Int _size;

        /// <summary>Size of the component in grid cells (width, height).</summary>
        public Vector2Int Size => _size;
    }
}
