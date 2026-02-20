using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    public class DeleteTraceNetCommand : ICommand
    {
        private readonly BoardState _boardState;
        private readonly int _initialNetId;

        private int _currentNetId;
        private string _savedNetName;
        private readonly List<(GridPosition start, GridPosition end)> _savedTraces = new List<(GridPosition, GridPosition)>();
        private readonly List<PinReference> _savedPins = new List<PinReference>();
        private bool _hasCapturedState;

        public string Description => $"Delete trace net {_initialNetId}";

        public DeleteTraceNetCommand(BoardState boardState, int netId)
        {
            _boardState = boardState;
            _initialNetId = netId;
            _currentNetId = netId;
        }

        public void Execute()
        {
            var net = _boardState.GetNet(_currentNetId);
            if (net == null)
            {
                _hasCapturedState = false;
                return;
            }

            _savedNetName = net.NetName;

            _savedTraces.Clear();
            foreach (var trace in _boardState.GetTraces(_currentNetId).ToList())
            {
                _savedTraces.Add((trace.Start, trace.End));
            }

            _savedPins.Clear();
            _savedPins.AddRange(net.ConnectedPins.ToList());

            _hasCapturedState = true;

            foreach (var trace in _boardState.GetTraces(_currentNetId).ToList())
            {
                _boardState.RemoveTrace(trace.SegmentId);
            }
        }

        public void Undo()
        {
            if (!_hasCapturedState)
                return;

            var recreatedNet = _boardState.CreateNet(_savedNetName);
            _currentNetId = recreatedNet.NetId;

            foreach (var (start, end) in _savedTraces)
            {
                _boardState.AddTrace(_currentNetId, start, end);
            }

            foreach (var pin in _savedPins)
            {
                _boardState.ConnectPinToNet(_currentNetId, pin);
            }
        }
    }
}
