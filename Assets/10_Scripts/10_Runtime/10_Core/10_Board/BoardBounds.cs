using System;
using System.Collections.Generic;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents an axis-aligned rectangle in board grid space.
    /// </summary>
    public readonly struct BoardBounds : IEquatable<BoardBounds>
    {
        /// <summary>Gets an unbounded sentinel value.</summary>
        public static BoardBounds Unbounded { get; } = new(int.MinValue / 2, int.MinValue / 2, int.MaxValue, int.MaxValue);

        /// <summary>Gets the minimum X coordinate (inclusive).</summary>
        public int MinX { get; }

        /// <summary>Gets the minimum Y coordinate (inclusive).</summary>
        public int MinY { get; }

        /// <summary>Gets the maximum X coordinate (exclusive).</summary>
        public int MaxX { get; }

        /// <summary>Gets the maximum Y coordinate (exclusive).</summary>
        public int MaxY { get; }

        /// <summary>Gets the width of the board bounds in grid cells.</summary>
        public int Width { get; }

        /// <summary>Gets the height of the board bounds in grid cells.</summary>
        public int Height { get; }

        /// <summary>
        /// Creates new board bounds at origin.
        /// </summary>
        /// <param name="width">Width in grid cells.</param>
        /// <param name="height">Height in grid cells.</param>
        public BoardBounds(int width, int height)
            : this(0, 0, width, height)
        {
        }

        /// <summary>
        /// Creates new board bounds from a minimum coordinate and size.
        /// </summary>
        /// <param name="minX">Minimum X coordinate (inclusive).</param>
        /// <param name="minY">Minimum Y coordinate (inclusive).</param>
        /// <param name="width">Width in grid cells.</param>
        /// <param name="height">Height in grid cells.</param>
        public BoardBounds(int minX, int minY, int width, int height)
        {
            MinX = minX;
            MinY = minY;
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
            MaxX = MinX + Width;
            MaxY = MinY + Height;
        }

        /// <summary>
        /// Creates board bounds that enclose the provided positions.
        /// </summary>
        /// <param name="positions">Positions to include.</param>
        /// <returns>Computed bounds, or a 1x1 bounds at origin if empty.</returns>
        public static BoardBounds FromContent(IEnumerable<GridPosition> positions)
        {
            if (positions is null)
                throw new ArgumentNullException(nameof(positions));

            using (var enumerator = positions.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return new BoardBounds(0, 0, 1, 1);
                }

                var first = enumerator.Current;
                var minX = first.X;
                var minY = first.Y;
                var maxX = first.X;
                var maxY = first.Y;

                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    minX = Math.Min(minX, current.X);
                    minY = Math.Min(minY, current.Y);
                    maxX = Math.Max(maxX, current.X);
                    maxY = Math.Max(maxY, current.Y);
                }

                return new BoardBounds(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
            }
        }

        /// <summary>
        /// Checks if a grid position is within the bounds.
        /// </summary>
        /// <param name="pos">Position to check.</param>
        /// <returns>True if position is within bounds.</returns>
        public bool Contains(GridPosition pos)
        {
            return pos.X >= MinX && pos.X < MaxX &&
                   pos.Y >= MinY && pos.Y < MaxY;
        }

        /// <summary>
        /// Checks equality with another board bounds.
        /// </summary>
        public bool Equals(BoardBounds other)
        {
            return MinX == other.MinX && MinY == other.MinY &&
                   Width == other.Width && Height == other.Height;
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
                var hash = MinX;
                hash = (hash * 397) ^ MinY;
                hash = (hash * 397) ^ Width;
                hash = (hash * 397) ^ Height;
                return hash;
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
            return $"({MinX},{MinY}) {Width}x{Height}";
        }
    }
}
