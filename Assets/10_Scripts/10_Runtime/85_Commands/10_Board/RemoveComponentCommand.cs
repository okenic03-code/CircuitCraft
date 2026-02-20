using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    public class RemoveComponentCommand : ICommand
    {
        private readonly BoardState _boardState;
        private readonly int _instanceId;

        private int _currentInstanceId;
        private string _componentDefId;
        private GridPosition _position;
        private int _rotation;
        private List<PinInstance> _pins;
        private bool _hasCapturedState;
        private float? _capturedCustomValue;
        private readonly List<(int netId, string netName)> _capturedNets = new List<(int, string)>();
        private readonly List<(int netId, GridPosition start, GridPosition end)> _capturedTraces = new List<(int, GridPosition, GridPosition)>();
        private readonly List<(int netId, int pinIndex)> _capturedPinConnections = new List<(int, int)>();

        public string Description => $"Remove component {_instanceId}";

        public RemoveComponentCommand(BoardState boardState, int instanceId)
        {
            _boardState = boardState;
            _instanceId = instanceId;
            _currentInstanceId = instanceId;
        }

        public void Execute()
        {
            var component = _boardState.GetComponent(_currentInstanceId);
            if (component == null)
                return;

            _componentDefId = component.ComponentDefinitionId;
            _position = component.Position;
            _rotation = component.Rotation;
            _pins = component.Pins.ToList();
            _capturedCustomValue = component.CustomValue;

            CaptureConnectedTopology(component);

            _hasCapturedState = true;

            _boardState.RemoveComponent(_currentInstanceId);
        }

        public void Undo()
        {
            if (!_hasCapturedState)
                return;

            var restored = _boardState.PlaceComponent(_componentDefId, _position, _rotation, _pins, _capturedCustomValue);
            _currentInstanceId = restored.InstanceId;

            var netIdMap = new Dictionary<int, int>();
            foreach (var (capturedNetId, capturedNetName) in _capturedNets)
            {
                var existingNet = _boardState.GetNet(capturedNetId);
                if (existingNet != null)
                {
                    netIdMap[capturedNetId] = existingNet.NetId;
                    continue;
                }

                var recreatedNet = _boardState.CreateNet(capturedNetName);
                netIdMap[capturedNetId] = recreatedNet.NetId;
            }

            foreach (var (capturedNetId, start, end) in _capturedTraces)
            {
                if (!netIdMap.TryGetValue(capturedNetId, out int restoredNetId))
                    continue;

                _boardState.AddTrace(restoredNetId, start, end);
            }

            foreach (var (capturedNetId, pinIndex) in _capturedPinConnections)
            {
                if (!netIdMap.TryGetValue(capturedNetId, out int restoredNetId))
                    continue;

                var pinRef = new PinReference(_currentInstanceId, pinIndex, restored.GetPinWorldPosition(pinIndex));
                _boardState.ConnectPinToNet(restoredNetId, pinRef);
            }
        }

        private void CaptureConnectedTopology(PlacedComponent component)
        {
            _capturedNets.Clear();
            _capturedTraces.Clear();
            _capturedPinConnections.Clear();

            var capturedNetIds = new HashSet<int>();
            var capturedTraceIds = new HashSet<int>();

            foreach (var pin in component.Pins)
            {
                if (!pin.ConnectedNetId.HasValue)
                    continue;

                int netId = pin.ConnectedNetId.Value;
                _capturedPinConnections.Add((netId, pin.PinIndex));

                if (capturedNetIds.Add(netId))
                {
                    var net = _boardState.GetNet(netId);
                    _capturedNets.Add((netId, net?.NetName ?? $"NET{netId}"));
                }

                var pinPosition = component.GetPinWorldPosition(pin.PinIndex);
                foreach (var trace in _boardState.GetTraces(netId))
                {
                    if (trace.Start != pinPosition && trace.End != pinPosition)
                        continue;

                    if (capturedTraceIds.Add(trace.SegmentId))
                    {
                        _capturedTraces.Add((netId, trace.Start, trace.End));
                    }
                }
            }
        }
    }
}
