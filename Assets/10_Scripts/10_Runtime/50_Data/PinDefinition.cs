using System;
using UnityEngine;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Defines a connection point on a component, used for netlist generation and grid placement.
    /// </summary>
    [Serializable]
    public class PinDefinition
    {
        [SerializeField]
        [Tooltip("Unique name for this pin within the component.")]
        private string pinName;

        [SerializeField]
        [Tooltip("Grid-aligned local position of the pin relative to component origin.")]
        private Vector2Int localPosition;

        /// <summary>Unique name for this pin within the component.</summary>
        public string PinName => pinName;

        /// <summary>Grid-aligned local position of the pin relative to component origin.</summary>
        public Vector2Int LocalPosition => localPosition;
    }
}
