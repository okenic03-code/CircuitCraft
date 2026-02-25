using System;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents a position on a discrete 2D grid.
    /// This is the fundamental coordinate type for circuit board layout.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        /// <summary>Gets the X coordinate (column).</summary>
        public int X { get; }

        /// <summary>Gets the Y coordinate (row).</summary>
        public int Y { get; }

        /// <summary>
        /// Creates a new grid position.
        /// </summary>
        /// <param name="x">X coordinate (column).</param>
        /// <param name="y">Y coordinate (row).</param>
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Calculates the Manhattan distance to another grid position.
        /// </summary>
        /// <param name="other">The other grid position.</param>
        /// <returns>Manhattan distance (|dx| + |dy|).</returns>
        public int ManhattanDistance(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Gets the 4-connected neighbors (up, down, left, right).
        /// </summary>
        /// <returns>Array of 4 neighboring positions.</returns>
        public GridPosition[] GetNeighbors()
        {
            return new[]
            {
                new GridPosition(X, Y + 1), // Up
                new GridPosition(X, Y - 1), // Down
                new GridPosition(X - 1, Y), // Left
                new GridPosition(X + 1, Y)  // Right
            };
        }

        /// <summary>
        /// Fills a buffer with the 4-connected neighbors (up, down, left, right).
        /// The buffer must have at least offset + 4 elements.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="offset">Starting index in the buffer (default 0).</param>
        public void FillNeighbors(GridPosition[] buffer, int offset = 0)
        {
            buffer[offset] = new GridPosition(X, Y + 1); // Up
            buffer[offset + 1] = new GridPosition(X, Y - 1); // Down
            buffer[offset + 2] = new GridPosition(X - 1, Y); // Left
            buffer[offset + 3] = new GridPosition(X + 1, Y); // Right
        }

        /// <summary>
        /// Checks equality with another grid position.
        /// </summary>
        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Checks equality with an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this grid position.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a string representation in (X, Y) format.
        /// </summary>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
