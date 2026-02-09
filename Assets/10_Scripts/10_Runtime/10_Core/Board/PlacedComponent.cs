using System;
using System.Collections.Generic;
using System.Linq;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents a component instance placed on the circuit board.
    /// </summary>
    public class PlacedComponent
    {
        private readonly List<PinInstance> _pins = new List<PinInstance>();

        /// <summary>Gets the unique instance ID for this component.</summary>
        public int InstanceId { get; }

        /// <summary>Gets the component definition ID (references ComponentDefinition.Id).</summary>
        public string ComponentDefinitionId { get; }

        /// <summary>Gets the position of this component on the board grid.</summary>
        public GridPosition Position { get; }

        /// <summary>Gets the rotation of this component in degrees (0, 90, 180, 270).</summary>
        public int Rotation { get; }

        /// <summary>Gets the read-only list of pins on this component.</summary>
        public IReadOnlyList<PinInstance> Pins => _pins.AsReadOnly();

        /// <summary>
        /// Creates a new placed component.
        /// </summary>
        /// <param name="instanceId">Unique instance ID.</param>
        /// <param name="componentDefId">Component definition ID.</param>
        /// <param name="position">Position on the board grid.</param>
        /// <param name="rotation">Rotation in degrees (0, 90, 180, 270).</param>
        /// <param name="pins">Pin instances for this component.</param>
        public PlacedComponent(int instanceId, string componentDefId, GridPosition position, 
                               int rotation, IEnumerable<PinInstance> pins)
        {
            if (instanceId < 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId), "Instance ID must be non-negative.");
            if (string.IsNullOrWhiteSpace(componentDefId))
                throw new ArgumentException("Component definition ID cannot be null or empty.", nameof(componentDefId));
            if (rotation != 0 && rotation != 90 && rotation != 180 && rotation != 270)
                throw new ArgumentOutOfRangeException(nameof(rotation), "Rotation must be 0, 90, 180, or 270 degrees.");
            if (pins == null)
                throw new ArgumentNullException(nameof(pins));

            InstanceId = instanceId;
            ComponentDefinitionId = componentDefId;
            Position = position;
            Rotation = rotation;
            _pins.AddRange(pins);
        }

        /// <summary>
        /// Gets the world position of a pin by applying component position and rotation.
        /// </summary>
        /// <param name="pinIndex">Pin index (0-based).</param>
        /// <returns>World grid position of the pin.</returns>
        public GridPosition GetPinWorldPosition(int pinIndex)
        {
            var pin = _pins.FirstOrDefault(p => p.PinIndex == pinIndex);
            if (pin == null)
                throw new ArgumentException($"Pin index {pinIndex} not found on component {InstanceId}.", nameof(pinIndex));

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
            switch (degrees)
            {
                case 0:
                    return localPos;
                case 90:
                    // Rotate 90° clockwise: (x, y) -> (y, -x)
                    return new GridPosition(localPos.Y, -localPos.X);
                case 180:
                    // Rotate 180°: (x, y) -> (-x, -y)
                    return new GridPosition(-localPos.X, -localPos.Y);
                case 270:
                    // Rotate 270° clockwise (90° counter-clockwise): (x, y) -> (-y, x)
                    return new GridPosition(-localPos.Y, localPos.X);
                default:
                    throw new ArgumentException($"Invalid rotation: {degrees}");
            }
        }

        /// <summary>
        /// Returns a string representation of this component.
        /// </summary>
        public override string ToString()
        {
            return $"Component[{InstanceId}:{ComponentDefinitionId}@{Position} R{Rotation}°] ({_pins.Count} pins)";
        }
    }
}
