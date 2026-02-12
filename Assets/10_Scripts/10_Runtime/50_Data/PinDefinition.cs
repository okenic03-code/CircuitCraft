using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("pinName")]
        private string _pinName;

        [SerializeField]
        [Tooltip("Grid-aligned local position of the pin relative to component origin.")]
        [FormerlySerializedAs("localPosition")]
        private Vector2Int _localPosition;

        /// <summary>Unique name for this pin within the component.</summary>
        public string PinName => _pinName;

        /// <summary>Grid-aligned local position of the pin relative to component origin.</summary>
        public Vector2Int LocalPosition => _localPosition;

        public PinDefinition(string pinName, Vector2Int localPosition)
        {
            _pinName = pinName;
            _localPosition = localPosition;
        }
    }
}
