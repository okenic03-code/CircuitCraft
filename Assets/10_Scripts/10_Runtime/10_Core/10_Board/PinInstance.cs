using System;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents a pin instance on a placed component.
    /// </summary>
    public class PinInstance
    {
        /// <summary>Gets the pin index on the component (0-based).</summary>
        public int PinIndex { get; }

        /// <summary>Gets the pin name (e.g., "anode", "cathode", "pin1").</summary>
        public string PinName { get; }

        /// <summary>Gets the local position of this pin relative to component origin.</summary>
        public GridPosition LocalPosition { get; }

        /// <summary>Gets or sets the ID of the net this pin is connected to (null if not connected).</summary>
        public int? ConnectedNetId { get; set; }

        /// <summary>
        /// Creates a new pin instance.
        /// </summary>
        /// <param name="pinIndex">Pin index (0-based).</param>
        /// <param name="pinName">Pin name.</param>
        /// <param name="localPosition">Local position relative to component.</param>
        public PinInstance(int pinIndex, string pinName, GridPosition localPosition)
        {
            if (pinIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pinIndex), "Pin index must be non-negative.");
            if (string.IsNullOrWhiteSpace(pinName))
                throw new ArgumentException("Pin name cannot be null or empty.", nameof(pinName));

            PinIndex = pinIndex;
            PinName = pinName;
            LocalPosition = localPosition;
        }

        /// <summary>
        /// Returns a string representation of this pin instance.
        /// </summary>
        public override string ToString()
        {
            var netInfo = ConnectedNetId.HasValue ? $" -> Net{ConnectedNetId.Value}" : " (unconnected)";
            return $"Pin[{PinIndex}:{PinName}@{LocalPosition}]{netInfo}";
        }
    }
}
