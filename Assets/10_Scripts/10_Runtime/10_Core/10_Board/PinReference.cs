using System;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents a reference to a specific pin on a placed component.
    /// </summary>
    public readonly struct PinReference : IEquatable<PinReference>
    {
        /// <summary>Gets the instance ID of the component this pin belongs to.</summary>
        public int ComponentInstanceId { get; }

        /// <summary>Gets the pin index on the component (0-based).</summary>
        public int PinIndex { get; }

        /// <summary>Gets the world position of this pin on the grid.</summary>
        public GridPosition Position { get; }

        /// <summary>
        /// Creates a new pin reference.
        /// </summary>
        /// <param name="componentInstanceId">Instance ID of the component.</param>
        /// <param name="pinIndex">Pin index (0-based).</param>
        /// <param name="position">World grid position of the pin.</param>
        public PinReference(int componentInstanceId, int pinIndex, GridPosition position)
        {
            ComponentInstanceId = componentInstanceId;
            PinIndex = pinIndex;
            Position = position;
        }

        /// <summary>
        /// Checks equality with another pin reference.
        /// </summary>
        public bool Equals(PinReference other)
        {
            return ComponentInstanceId == other.ComponentInstanceId &&
                   PinIndex == other.PinIndex &&
                   Position.Equals(other.Position);
        }

        /// <summary>
        /// Checks equality with an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is PinReference other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this pin reference.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ComponentInstanceId;
                hash = (hash * 397) ^ PinIndex;
                hash = (hash * 397) ^ Position.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        public override string ToString()
        {
            return $"Pin[C{ComponentInstanceId}:P{PinIndex}@{Position}]";
        }
    }
}
