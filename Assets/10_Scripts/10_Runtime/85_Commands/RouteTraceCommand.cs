using System;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    /// <summary>
    /// Command that routes trace segments between two pins and manages net connections.
    /// Supports undo/redo through the CommandHistory system.
    /// </summary>
    public class RouteTraceCommand : ICommand
    {
        private const string GroundNetName = "0";
        private const string GroundNetAlias = "GND";
        private const string GroundComponentDefinitionId = "ground";

        private readonly BoardState _boardState;
        private readonly PinReference _startPin;
        private readonly PinReference _endPin;
        private readonly List<(GridPosition start, GridPosition end)> _segments;

        // State captured during Execute for Undo
        private int _netId;
        private string _netName;
        private int? _restoredRouteNetId;
        private readonly List<int> _addedSegmentIds = new();
        private int? _startPinPreviousNetId;
        private int? _endPinPreviousNetId;

        private bool _didMerge;
        private int _mergedSourceNetId;
        private string _mergedSourceNetName;
        private readonly List<(GridPosition start, GridPosition end)> _mergedSourceTraces = new();
        private readonly List<PinReference> _mergedSourcePins = new();
        private readonly List<int> _mergedTargetTraceIds = new();

        /// <summary>
        /// Gets a user-facing description of this routing command.
        /// </summary>
        public string Description => $"Route trace from {_startPin} to {_endPin}";

        /// <summary>
        /// Creates a new route trace command.
        /// </summary>
        /// <param name="boardState">The board state to operate on.</param>
        /// <param name="startPin">The starting pin reference.</param>
        /// <param name="endPin">The ending pin reference.</param>
        /// <param name="segments">Manhattan trace segments to add.</param>
        public RouteTraceCommand(
            BoardState boardState,
            PinReference startPin,
            PinReference endPin,
            List<(GridPosition start, GridPosition end)> segments)
        {
            _boardState = boardState;
            _startPin = startPin;
            _endPin = endPin;
            _segments = segments;
        }

        /// <summary>
        /// Executes trace routing, net resolution, and optional net merge behavior.
        /// </summary>
        public void Execute()
        {
            // Capture previous pin net connections for undo
            _startPinPreviousNetId = GetPinConnectedNetId(_startPin);
            _endPinPreviousNetId = GetPinConnectedNetId(_endPin);

            _addedSegmentIds.Clear();
            _didMerge = false;
            _restoredRouteNetId = null;

            // Resolve which net to use (may create a new one)
            _netId = ResolveNetId();
            _netName = _boardState.GetNet(_netId)?.NetName;

            // Connect pins to the net
            _boardState.ConnectPinToNet(_netId, _startPin);
            _boardState.ConnectPinToNet(_netId, _endPin);

            // Add trace segments
            foreach (var segment in _segments)
            {
                var trace = _boardState.AddTrace(_netId, segment.start, segment.end);
                _addedSegmentIds.Add(trace.SegmentId);
            }
        }

        /// <summary>
        /// Undoes routed trace segments and restores previous pin and net state.
        /// </summary>
        public void Undo()
        {
            // Remove all trace segments added by this command (reverse order)
            for (int i = _addedSegmentIds.Count - 1; i >= 0; i--)
            {
                _boardState.RemoveTrace(_addedSegmentIds[i]);
            }
            _addedSegmentIds.Clear();

            // BoardState.RemoveTrace auto-cleans the net when no traces remain
            // (disconnects all pins, removes net). Check if net still exists.
            var net = _boardState.GetNet(_netId);

            if (net is not null)
            {
                // Net still has other traces. Only disconnect pins that we newly
                // connected (skip pins that were already on this net before Execute).
                if (!_startPinPreviousNetId.HasValue || _startPinPreviousNetId.Value != _netId)
                {
                    DisconnectPinFromNet(_startPin, net);
                }

                if (!_endPinPreviousNetId.HasValue || _endPinPreviousNetId.Value != _netId)
                {
                    DisconnectPinFromNet(_endPin, net);
                }
            }

            // Restore previous pin connections if they were on different nets
            RestorePreviousPinConnection(_startPin, _startPinPreviousNetId);
            RestorePreviousPinConnection(_endPin, _endPinPreviousNetId);

            UnmergeNets();
        }

        private int ResolveNetId()
        {
            int? startNetId = _startPinPreviousNetId;
            int? endNetId = _endPinPreviousNetId;

            if (startNetId.HasValue && endNetId.HasValue)
            {
                if (startNetId.Value != endNetId.Value)
                {
                    int targetNetId = GetPreferredMergeTargetNetId(startNetId.Value, endNetId.Value);
                    int sourceNetId = targetNetId == startNetId.Value ? endNetId.Value : startNetId.Value;
                    MergeNets(targetNetId, sourceNetId);

                    return targetNetId;
                }

                return startNetId.Value;
            }

            if (startNetId.HasValue)
            {
                return startNetId.Value;
            }

            if (endNetId.HasValue)
            {
                return endNetId.Value;
            }

            // Neither pin connected — create a new net
            string netName = IsGroundPin(_startPin) || IsGroundPin(_endPin)
                ? GroundNetName
                : $"NET{_boardState.Nets.Count + 1}";
            return _boardState.CreateNet(netName).NetId;
        }

        private int GetPreferredMergeTargetNetId(int firstNetId, int secondNetId)
        {
            var firstNet = _boardState.GetNet(firstNetId);
            var secondNet = _boardState.GetNet(secondNetId);

            bool firstIsGround = IsGroundNet(firstNet);
            bool secondIsGround = IsGroundNet(secondNet);

            if (firstIsGround && !secondIsGround)
                return firstNetId;

            if (!firstIsGround && secondIsGround)
                return secondNetId;

            return firstNetId;
        }

        private bool IsGroundPin(PinReference pinRef)
        {
            var component = _boardState.GetComponent(pinRef.ComponentInstanceId);
            if (component is null)
                return false;

            return string.Equals(component.ComponentDefinitionId, GroundComponentDefinitionId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGroundNet(Net net)
            => net is not null && IsGroundNetName(net.NetName);

        private static bool IsGroundNetName(string netName)
        {
            return string.Equals(netName, GroundNetName, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(netName, GroundNetAlias, StringComparison.OrdinalIgnoreCase);
        }

        private void MergeNets(int targetNetId, int sourceNetId)
        {
            if (targetNetId == sourceNetId)
                return;

            var sourceNet = _boardState.GetNet(sourceNetId);
            if (sourceNet is null)
                return;

            _didMerge = true;
            _mergedSourceNetId = sourceNetId;
            _mergedSourceNetName = sourceNet.NetName;

            _mergedSourcePins.Clear();
            _mergedSourcePins.AddRange(sourceNet.ConnectedPins.ToList());

            _mergedSourceTraces.Clear();
            foreach (var trace in _boardState.GetTraces(sourceNetId).ToList())
            {
                _mergedSourceTraces.Add((trace.Start, trace.End));
            }

            _mergedTargetTraceIds.Clear();
            foreach (var pin in _mergedSourcePins)
            {
                _boardState.ConnectPinToNet(targetNetId, pin);
            }

            foreach (var trace in _boardState.GetTraces(sourceNetId).ToList())
            {
                var newTrace = _boardState.AddTrace(targetNetId, trace.Start, trace.End);
                _mergedTargetTraceIds.Add(newTrace.SegmentId);
                _boardState.RemoveTrace(trace.SegmentId);
            }
        }

        private void UnmergeNets()
        {
            if (!_didMerge)
                return;

            foreach (var traceId in _mergedTargetTraceIds)
            {
                _boardState.RemoveTrace(traceId);
            }
            _mergedTargetTraceIds.Clear();

            var sourceNet = _boardState.CreateNet(_mergedSourceNetName);
            int newSourceNetId = sourceNet.NetId;

            foreach (var (start, end) in _mergedSourceTraces)
            {
                _boardState.AddTrace(newSourceNetId, start, end);
            }

            foreach (var pin in _mergedSourcePins)
            {
                _boardState.ConnectPinToNet(newSourceNetId, pin);
            }
        }

        private int? GetPinConnectedNetId(PinReference pinRef)
        {
            var component = _boardState.GetComponent(pinRef.ComponentInstanceId);
            if (component is null)
                return null;

            PinInstance pin = null;
            foreach (var existingPin in component.Pins)
            {
                if (existingPin.PinIndex == pinRef.PinIndex)
                {
                    pin = existingPin;
                    break;
                }
            }

            return pin?.ConnectedNetId;
        }

        private void DisconnectPinFromNet(PinReference pinRef, Net net)
        {
            net.RemovePin(pinRef);

            var component = _boardState.GetComponent(pinRef.ComponentInstanceId);
            if (component is null)
                return;

            PinInstance pinInstance = null;
            foreach (var existingPin in component.Pins)
            {
                if (existingPin.PinIndex == pinRef.PinIndex)
                {
                    pinInstance = existingPin;
                    break;
                }
            }

            if (pinInstance is not null && pinInstance.ConnectedNetId == _netId)
            {
                pinInstance.ConnectedNetId = null;
            }
        }

        private void RestorePreviousPinConnection(PinReference pinRef, int? previousNetId)
        {
            if (!previousNetId.HasValue)
                return;

            int targetNetId = previousNetId.Value;
            if (_restoredRouteNetId.HasValue && previousNetId.Value == _netId)
            {
                targetNetId = _restoredRouteNetId.Value;
            }

            var previousNet = _boardState.GetNet(targetNetId);
            if (previousNet is null)
            {
                if (previousNetId.Value != _netId || string.IsNullOrEmpty(_netName))
                    return;

                previousNet = _boardState.CreateNet(_netName);
                _restoredRouteNetId = previousNet.NetId;
                targetNetId = previousNet.NetId;
            }

            _boardState.ConnectPinToNet(targetNetId, pinRef);
        }
    }
}
