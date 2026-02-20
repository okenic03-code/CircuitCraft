using System;
using System.Collections.Generic;

namespace CircuitCraft.Commands
{
    public class CommandHistory
    {
        private readonly int _maxCapacity;
        private readonly List<ICommand> _undoStack = new List<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        public CommandHistory(int maxCapacity = 100)
        {
            _maxCapacity = maxCapacity;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event Action<ICommand> OnCommandExecuted;
        public event Action<ICommand> OnUndo;
        public event Action<ICommand> OnRedo;
        public event Action OnHistoryChanged;

        public void ExecuteCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Execute();
            _undoStack.Add(command);

            while (_undoStack.Count > _maxCapacity)
            {
                _undoStack.RemoveAt(0);
            }

            _redoStack.Clear();
            OnCommandExecuted?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            command.Undo();
            _redoStack.Push(command);
            OnUndo?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Add(command);
            OnRedo?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
