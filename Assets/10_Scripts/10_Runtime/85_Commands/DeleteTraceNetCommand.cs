using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    /// <summary>
    /// Deletes all traces from a net and restores them on undo.
    /// </summary>
    public class DeleteTraceNetCommand : ICommand
    {
        private readonly BoardState _boardState;
        private readonly int _initialNetId;

        private int _currentNetId;
        private string _savedNetName;
        private readonly List<(GridPosition start, GridPosition end)> _savedTraces = new();
        private readonly List<PinReference> _savedPins = new();
        private bool _hasCapturedState;

        /// <summary>
        /// Gets a user-facing description of this trace net deletion command.
        /// </summary>
        public string Description => $"Delete trace net {_initialNetId}";

        /// <summary>
        /// Creates a command that removes trace segments for the specified net.
        /// </summary>
        /// <param name="boardState">The board state to mutate.</param>
        /// <param name="netId">The target net identifier.</param>
        public DeleteTraceNetCommand(BoardState boardState, int netId)
        {
            _boardState = boardState;
            _initialNetId = netId;
            _currentNetId = netId;
        }

        /// <summary>
        /// Executes trace deletion for the current net while capturing undo state.
        /// </summary>
        public void Execute()
        {
            var net = _boardState.GetNet(_currentNetId);
            if (net is null)
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

        /// <summary>
        /// Restores the previously deleted net traces and pin connections.
        /// </summary>
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
