using System;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents the boundaries of the circuit board grid.
    /// </summary>
    public readonly struct BoardBounds : IEquatable<BoardBounds>
    {
        /// <summary>Gets the width of the board in grid cells.</summary>
        public int Width { get; }

        /// <summary>Gets the height of the board in grid cells.</summary>
        public int Height { get; }

        /// <summary>
        /// Creates new board bounds.
        /// </summary>
        /// <param name="width">Width in grid cells.</param>
        /// <param name="height">Height in grid cells.</param>
        public BoardBounds(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Checks if a grid position is within the board bounds.
        /// </summary>
        /// <param name="pos">Position to check.</param>
        /// <returns>True if position is within bounds.</returns>
        public bool Contains(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < Width &&
                   pos.Y >= 0 && pos.Y < Height;
        }

        /// <summary>
        /// Checks equality with another board bounds.
        /// </summary>
        public bool Equals(BoardBounds other)
        {
            return Width == other.Width && Height == other.Height;
        }

        /// <summary>
        /// Checks equality with an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is BoardBounds other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(BoardBounds left, BoardBounds right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(BoardBounds left, BoardBounds right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}
