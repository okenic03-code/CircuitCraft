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
            _hasCapturedState = true;

            _boardState.RemoveComponent(_currentInstanceId);
        }

        public void Undo()
        {
            if (!_hasCapturedState)
                return;

            var restored = _boardState.PlaceComponent(_componentDefId, _position, _rotation, _pins);
            _currentInstanceId = restored.InstanceId;
        }
    }
}
