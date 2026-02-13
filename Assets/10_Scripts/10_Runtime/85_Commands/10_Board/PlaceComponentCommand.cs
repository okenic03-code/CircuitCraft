using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Commands
{
    public class PlaceComponentCommand : ICommand
    {
        private readonly BoardState _boardState;
        private readonly string _componentDefId;
        private readonly GridPosition _position;
        private readonly int _rotation;
        private readonly List<PinInstance> _pins;
        private readonly float? _customValue;
        private int _placedInstanceId;

        public string Description => $"Place {_componentDefId} at {_position}";

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
            _pins = pins != null ? pins.ToList() : new List<PinInstance>();
            _customValue = customValue;
        }

        public void Execute()
        {
            var placed = _boardState.PlaceComponent(_componentDefId, _position, _rotation, _pins, _customValue);
            _placedInstanceId = placed.InstanceId;
        }

        public void Undo()
        {
            _boardState.RemoveComponent(_placedInstanceId);
        }
    }
}
