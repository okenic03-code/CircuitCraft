using System;
using System.Collections.Generic;

namespace CircuitCraft.Core
{
    public static class RotationConstants
    {
        public const int None = 0;
        public const int Quarter = 90;
        public const int Half = 180;
        public const int ThreeQuarter = 270;
        public const int Full = 360;
        public static readonly int[] ValidRotations = { None, Quarter, Half, ThreeQuarter };
    }

    /// <summary>
    /// Represents a component instance placed on the circuit board.
    /// </summary>
    public class PlacedComponent
    {
        private readonly List<PinInstance> _pins = new();
        private readonly IReadOnlyList<PinInstance> _readOnlyPins;

        /// <summary>Gets the unique instance ID for this component.</summary>
        public int InstanceId { get; }

        /// <summary>Gets the component definition ID (references ComponentDefinition.Id).</summary>
        public string ComponentDefinitionId { get; }

        /// <summary>Gets the position of this component on the board grid.</summary>
        public GridPosition Position { get; }

        /// <summary>Gets the rotation of this component in degrees (0, 90, 180, 270).</summary>
        public int Rotation { get; }

        /// <summary>Gets the user-specified custom electrical value (null if using definition default).</summary>
        public float? CustomValue { get; }

        /// <summary>Gets whether this component is fixed and cannot be moved/removed by the player.</summary>
        public bool IsFixed { get; }

        /// <summary>Gets the read-only list of pins on this component.</summary>
        public IReadOnlyList<PinInstance> Pins => _readOnlyPins;

        /// <summary>
        /// Creates a new placed component.
        /// </summary>
        /// <param name="instanceId">Unique instance ID.</param>
        /// <param name="componentDefId">Component definition ID.</param>
        /// <param name="position">Position on the board grid.</param>
        /// <param name="rotation">Rotation in degrees (0, 90, 180, 270).</param>
        /// <param name="pins">Pin instances for this component.</param>
        /// <param name="customValue">User-specified custom electrical value (null to use definition default).</param>
        /// <param name="isFixed">True when this component is pre-placed and non-removable.</param>
        public PlacedComponent(int instanceId, string componentDefId, GridPosition position, 
                               int rotation, IEnumerable<PinInstance> pins, float? customValue = null, bool isFixed = false)
        {
            if (instanceId < 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId), "Instance ID must be non-negative.");
            if (string.IsNullOrWhiteSpace(componentDefId))
                throw new ArgumentException("Component definition ID cannot be null or empty.", nameof(componentDefId));
            if (Array.IndexOf(RotationConstants.ValidRotations, rotation) < 0)
                throw new ArgumentOutOfRangeException(nameof(rotation), "Rotation must be 0, 90, 180, or 270 degrees.");
            if (pins is null)
                throw new ArgumentNullException(nameof(pins));

            InstanceId = instanceId;
            ComponentDefinitionId = componentDefId;
            Position = position;
            Rotation = rotation;
            CustomValue = customValue;
            IsFixed = isFixed;
            _pins.AddRange(pins);
            _readOnlyPins = _pins.AsReadOnly();
        }

        /// <summary>
        /// Gets the world position of a pin by applying component position and rotation.
        /// </summary>
        /// <param name="pinIndex">Pin index (0-based).</param>
        /// <returns>World grid position of the pin.</returns>
        public GridPosition GetPinWorldPosition(int pinIndex)
        {
            if (pinIndex is < 0 or >= _pins.Count)
                throw new ArgumentException($"Pin index {pinIndex} not found on component {InstanceId}.", nameof(pinIndex));

            var pin = _pins[pinIndex];

            // Apply rotation transformation to local position
            var localPos = pin.LocalPosition;
            var rotatedPos = RotatePosition(localPos, Rotation);

            // Add component position to get world position
            return new GridPosition(
                Position.X + rotatedPos.X,
                Position.Y + rotatedPos.Y
            );
        }

        /// <summary>
        /// Rotates a local position by the specified angle.
        /// </summary>
        /// <param name="localPos">Local position to rotate.</param>
        /// <param name="degrees">Rotation angle (0, 90, 180, 270).</param>
        /// <returns>Rotated position.</returns>
        private GridPosition RotatePosition(GridPosition localPos, int degrees)
        {
            return degrees switch
            {
                RotationConstants.None => localPos,
                // Rotate 90° clockwise: (x, y) -> (y, -x)
                RotationConstants.Quarter => new GridPosition(localPos.Y, -localPos.X),
                // Rotate 180°: (x, y) -> (-x, -y)
                RotationConstants.Half => new GridPosition(-localPos.X, -localPos.Y),
                // Rotate 270° clockwise (90° counter-clockwise): (x, y) -> (-y, x)
                RotationConstants.ThreeQuarter => new GridPosition(-localPos.Y, localPos.X),
                _ => throw new ArgumentException($"Invalid rotation: {degrees}")
            };
        }

        /// <summary>
        /// Returns a string representation of this component.
        /// </summary>
        public override string ToString()
        {
            var customValueStr = CustomValue.HasValue ? $" V={CustomValue.Value}" : "";
            return $"Component[{InstanceId}:{ComponentDefinitionId}@{Position} R{Rotation}°]{customValueStr} ({_pins.Count} pins)";
        }
    }
}
