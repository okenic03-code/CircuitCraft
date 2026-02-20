using System;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents a routed board trace segment between two orthogonal grid points.
    /// </summary>
    public class TraceSegment
    {
        /// <summary>Gets the unique segment identifier.</summary>
        public int SegmentId { get; }

        /// <summary>Gets the net this trace segment belongs to.</summary>
        public int NetId { get; }

        /// <summary>Gets the segment start position.</summary>
        public GridPosition Start { get; }

        /// <summary>Gets the segment end position.</summary>
        public GridPosition End { get; }

        /// <summary>
        /// Creates a new orthogonal trace segment.
        /// </summary>
        public TraceSegment(int segmentId, int netId, GridPosition start, GridPosition end)
        {
            if (segmentId <= 0)
                throw new ArgumentOutOfRangeException(nameof(segmentId), "Segment ID must be positive.");
            if (netId <= 0)
                throw new ArgumentOutOfRangeException(nameof(netId), "Net ID must be positive.");
            if (start == end)
                throw new ArgumentException("Trace segment start and end cannot be the same point.");
            if (start.X != end.X && start.Y != end.Y)
                throw new ArgumentException("Trace segment must be Manhattan (horizontal or vertical).", nameof(end));

            SegmentId = segmentId;
            NetId = netId;
            Start = start;
            End = end;
        }

        /// <summary>
        /// Returns a string representation of this trace segment.
        /// </summary>
        public override string ToString()
        {
            return $"Trace[{SegmentId}] Net{NetId}: {Start} -> {End}";
        }
    }
}
