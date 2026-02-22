using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    /// <summary>
    /// Places a component instance on the board and supports undo removal.
    /// </summary>
    public class PlaceComponentCommand : ICommand
    {
        private readonly BoardState _boardState;
        private readonly string _componentDefId;
        private readonly GridPosition _position;
        private readonly int _rotation;
        private readonly List<PinInstance> _pins;
        private readonly float? _customValue;
        private int _placedInstanceId;

        /// <summary>
        /// Gets a user-facing description of this placement command.
        /// </summary>
        public string Description => $"Place {_componentDefId} at {_position}";

        /// <summary>
        /// Creates a command that places a component at the provided board position.
        /// </summary>
        /// <param name="boardState">The board state to mutate.</param>
        /// <param name="componentDefId">The component definition identifier.</param>
        /// <param name="position">The origin placement position on the grid.</param>
        /// <param name="rotation">The rotation in degrees.</param>
        /// <param name="pins">The component pin instances used for placement.</param>
        /// <param name="customValue">User-specified custom electrical value (null to use definition default).</param>
        public PlaceComponentCommand(
            BoardState boardState,
            string componentDefId,
            GridPosition position,
            int rotation,
            IEnumerable<PinInstance> pins,
            float? customValue = null)
        {
            _boardState = boardState;
            _componentDefId = componentDefId;
            _position = position;
            _rotation = rotation;
            _pins = pins?.ToList() ?? new();
            _customValue = customValue;
        }

        /// <summary>
        /// Executes the component placement.
        /// </summary>
        public void Execute()
        {
            var placed = _boardState.PlaceComponent(_componentDefId, _position, _rotation, _pins, _customValue);
            _placedInstanceId = placed.InstanceId;
        }

        /// <summary>
        /// Undoes the component placement by removing the placed instance.
        /// </summary>
        public void Undo()
        {
            _boardState.RemoveComponent(_placedInstanceId);
        }
    }
}
