using System;
using System.Collections.Generic;
using CircuitCraft.Core;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Pure logic for Manhattan wire path building and trace hit detection.
    /// </summary>
    public static class WirePathCalculator
    {
        /// <summary>
        /// Builds a Manhattan wire path between two grid points.
        /// </summary>
        /// <param name="start">Path start position.</param>
        /// <param name="end">Path end position.</param>
        /// <returns>Ordered list of one or two contiguous Manhattan segments.</returns>
        public static List<(GridPosition start, GridPosition end)> BuildManhattanSegments(GridPosition start, GridPosition end)
        {
            var segments = new List<(GridPosition start, GridPosition end)>();

            if (start.X == end.X || start.Y == end.Y)
            {
                segments.Add((start, end));
                return segments;
            }

            var corner = new GridPosition(end.X, start.Y);
            segments.Add((start, corner));
            segments.Add((corner, end));

            return segments;
        }

        /// <summary>
        /// Checks whether a grid point lies on a trace segment.
        /// </summary>
        /// <param name="trace">Trace segment to test.</param>
        /// <param name="point">Grid point to test.</param>
        /// <returns>True when the point lies on the trace segment; otherwise false.</returns>
        public static bool IsPointOnTrace(TraceSegment trace, GridPosition point)
        {
            if (trace.Start.X == trace.End.X)
            {
                if (point.X != trace.Start.X)
                    return false;

                int minY = Math.Min(trace.Start.Y, trace.End.Y);
                int maxY = Math.Max(trace.Start.Y, trace.End.Y);
                return point.Y >= minY && point.Y <= maxY;
            }

            if (point.Y != trace.Start.Y)
                return false;

            int minX = Math.Min(trace.Start.X, trace.End.X);
            int maxX = Math.Max(trace.Start.X, trace.End.X);
            return point.X >= minX && point.X <= maxX;
        }
    }
}
