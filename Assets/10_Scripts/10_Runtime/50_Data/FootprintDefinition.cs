using System;
using UnityEngine;

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
        private Vector2Int size;

        /// <summary>Size of the component in grid cells (width, height).</summary>
        public Vector2Int Size => size;
    }
}
