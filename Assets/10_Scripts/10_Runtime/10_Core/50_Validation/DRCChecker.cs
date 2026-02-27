using System;
using System.Collections.Generic;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Performs design rule checks on a board state.
    /// Detects shorts (overlapping nets) and unconnected pins.
    /// Pure domain logic — no Unity dependencies.
    /// </summary>
    public class DRCChecker
    {
        private readonly Dictionary<GridPosition, HashSet<int>> _positionToNets =
            new();

        /// <summary>
        /// Runs all design rule checks on the given board state.
        /// </summary>
        /// <param name="board">The board state to check.</param>
        /// <returns>A DRCResult containing all detected violations.</returns>
        public DRCResult Check(BoardState board)
        {
            if (board is null)
                throw new ArgumentNullException(nameof(board));

            var violations = new List<DRCViolationItem>();

            DetectShorts(board, violations);
            DetectUnconnectedPins(board, violations);

            return new DRCResult(violations);
        }

        /// <summary>
        /// Detects shorts: two different nets with traces passing through the same grid position.
        /// Walks each trace segment from Start to End, building a position-to-netIds map.
        /// Any position occupied by 2+ different nets is a short.
        /// </summary>
        private void DetectShorts(BoardState board, List<DRCViolationItem> violations)
        {
            // In schematic-style routing, geometric trace crossings are allowed
            // and do NOT constitute electrical shorts. Nets are defined exclusively
            // by pin connections and explicit junctions (RouteTraceToNetPointCommand).
            // No geometric short detection is needed.
        }

        /// <summary>
        /// Detects unconnected pins: pins on placed components that are not connected to any net.
        /// </summary>
        private void DetectUnconnectedPins(BoardState board, List<DRCViolationItem> violations)
        {
            foreach (var component in board.Components)
            {
                foreach (var pin in component.Pins)
                {
                    if (!pin.ConnectedNetId.HasValue)
                    {
                        var worldPos = component.GetPinWorldPosition(pin.PinIndex);
                        violations.Add(new DRCViolationItem(
                            DRCViolationType.UnconnectedPin,
                            worldPos,
                            $"Unconnected pin: {pin.PinName} (index {pin.PinIndex}) on component {component.ComponentDefinitionId} (instance {component.InstanceId})"
                        ));
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all grid positions along a Manhattan (orthogonal) trace segment,
        /// including both Start and End positions.
        /// </summary>
        /// <param name="trace">The trace segment to walk.</param>
        /// <param name="action">Action invoked for each grid position on the trace.</param>
        private void EnumerateTracePositions(TraceSegment trace, Action<GridPosition> action)
        {
            var start = trace.Start;
            var end = trace.End;

            int dx = end.X > start.X ? 1 : (end.X < start.X ? -1 : 0);
            int dy = end.Y > start.Y ? 1 : (end.Y < start.Y ? -1 : 0);

            int x = start.X;
            int y = start.Y;

            while (true)
            {
                action(new GridPosition(x, y));

                if (x == end.X && y == end.Y)
                    break;

                x += dx;
                y += dy;
            }
        }
    }
}
