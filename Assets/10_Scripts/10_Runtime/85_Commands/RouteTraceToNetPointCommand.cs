using System;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    /// <summary>
    /// Routes from a component pin to a point on an existing trace net (junction behavior).
    /// </summary>
    public class RouteTraceToNetPointCommand : ICommand
    {
        private const string GroundNetName = "0";
        private const string GroundNetAlias = "GND";
        private const string GroundComponentDefinitionId = "ground";

        private readonly BoardState _boardState;
        private readonly PinReference _startPin;
        private readonly int _targetNetId;
        private readonly GridPosition _targetPoint;
        private readonly List<(GridPosition start, GridPosition end)> _segments;

        private readonly List<int> _addedSegmentIds = new();
        private int? _startPinPreviousNetId;
        private int _resolvedNetId;

        private bool _didMerge;
        private int _mergedSourceNetId;
        private string _mergedSourceNetName;
        private readonly List<(GridPosition start, GridPosition end)> _mergedSourceTraces = new();
        private readonly List<PinReference> _mergedSourcePins = new();
        private readonly List<int> _mergedTargetTraceIds = new();
        private bool _didRenameResolvedNet;
        private string _resolvedNetOriginalName;

        /// <summary>
        /// Gets a user-facing description of this junction routing command.
        /// </summary>
        public string Description => $"Route trace from {_startPin} to net {_targetNetId} at {_targetPoint}";

        /// <summary>
        /// Creates a command that routes from a pin to a point on an existing net.
        /// </summary>
        public RouteTraceToNetPointCommand(
            BoardState boardState,
            PinReference startPin,
            int targetNetId,
            GridPosition targetPoint,
            List<(GridPosition start, GridPosition end)> segments)
        {
            _boardState = boardState;
            _startPin = startPin;
            _targetNetId = targetNetId;
            _targetPoint = targetPoint;
            _segments = segments ?? new();
        }

        /// <summary>
        /// Executes trace routing to the target net point.
        /// </summary>
        public void Execute()
        {
            _startPinPreviousNetId = GetPinConnectedNetId(_startPin);
            _addedSegmentIds.Clear();
            _didMerge = false;
            _didRenameResolvedNet = false;
            _resolvedNetOriginalName = null;

            _resolvedNetId = ResolveNetId();
            EnsureResolvedNetGroundName();
            _boardState.ConnectPinToNet(_resolvedNetId, _startPin);

            foreach (var segment in _segments)
            {
                var trace = _boardState.AddTrace(_resolvedNetId, segment.start, segment.end);
                _addedSegmentIds.Add(trace.SegmentId);
            }
        }

        /// <summary>
        /// Undoes routed segments and restores prior net/pin topology.
        /// </summary>
        public void Undo()
        {
            for (int i = _addedSegmentIds.Count - 1; i >= 0; i--)
            {
                _boardState.RemoveTrace(_addedSegmentIds[i]);
            }
            _addedSegmentIds.Clear();

            var resolvedNet = _boardState.GetNet(_resolvedNetId);
            if (resolvedNet is not null
                && (!_startPinPreviousNetId.HasValue || _startPinPreviousNetId.Value != _resolvedNetId))
            {
                DisconnectPinFromNet(_startPin, resolvedNet, _resolvedNetId);
            }

            UnmergeNets();
            RestoreResolvedNetName();
            RestorePreviousPinConnection();
        }

        private int ResolveNetId()
        {
            if (!_startPinPreviousNetId.HasValue || _startPinPreviousNetId.Value == _targetNetId)
            {
                return _targetNetId;
            }

            int preferredTargetNetId = GetPreferredMergeTargetNetId(_startPinPreviousNetId.Value, _targetNetId);
            int sourceNetId = preferredTargetNetId == _startPinPreviousNetId.Value
                ? _targetNetId
                : _startPinPreviousNetId.Value;
            MergeNets(preferredTargetNetId, sourceNetId);
            return preferredTargetNetId;
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

            return secondNetId;
        }

        private static bool IsGroundNet(Net net)
            => net is not null && IsGroundNetName(net.NetName);

        private bool IsGroundPin(PinReference pinRef)
        {
            var component = _boardState.GetComponent(pinRef.ComponentInstanceId);
            if (component is null)
                return false;

            return string.Equals(component.ComponentDefinitionId, GroundComponentDefinitionId, StringComparison.OrdinalIgnoreCase);
        }

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

        private void EnsureResolvedNetGroundName()
        {
            if (!IsGroundPin(_startPin))
                return;

            var net = _boardState.GetNet(_resolvedNetId);
            if (net is null || IsGroundNetName(net.NetName))
                return;

            _didRenameResolvedNet = true;
            _resolvedNetOriginalName = net.NetName;
            net.NetName = GroundNetName;
        }

        private void RestoreResolvedNetName()
        {
            if (!_didRenameResolvedNet || string.IsNullOrEmpty(_resolvedNetOriginalName))
                return;

            var net = _boardState.GetNet(_resolvedNetId);
            if (net is null)
                return;

            net.NetName = _resolvedNetOriginalName;
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

            _boardState.CreateNetWithId(_mergedSourceNetId, _mergedSourceNetName);

            foreach (var (start, end) in _mergedSourceTraces)
            {
                _boardState.AddTrace(_mergedSourceNetId, start, end);
            }

            foreach (var pin in _mergedSourcePins)
            {
                _boardState.ConnectPinToNet(_mergedSourceNetId, pin);
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

        private void DisconnectPinFromNet(PinReference pinRef, Net net, int netId)
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

            if (pinInstance is not null && pinInstance.ConnectedNetId == netId)
            {
                pinInstance.ConnectedNetId = null;
            }
        }

        private void RestorePreviousPinConnection()
        {
            if (!_startPinPreviousNetId.HasValue)
                return;

            if (_boardState.GetNet(_startPinPreviousNetId.Value) is null)
                return;

            _boardState.ConnectPinToNet(_startPinPreviousNetId.Value, _startPin);
        }
    }
}
