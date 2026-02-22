using System;
using System.Collections.Generic;

namespace CircuitCraft.Commands
{
    /// <summary>
    /// Maintains undo/redo history for executed board commands.
    /// </summary>
    public class CommandHistory
    {
        private readonly int _maxCapacity;
        private readonly LinkedList<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();

        /// <summary>
        /// Initializes a new command history with an optional maximum undo capacity.
        /// </summary>
        /// <param name="maxCapacity">Maximum number of commands retained in the undo history.</param>
        public CommandHistory(int maxCapacity = 100)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Gets a value indicating whether an undo operation is currently available.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets a value indicating whether a redo operation is currently available.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Raised after a command is executed and added to history.
        /// </summary>
        public event Action<ICommand> OnCommandExecuted;

        /// <summary>
        /// Raised after a command is undone.
        /// </summary>
        public event Action<ICommand> OnUndo;

        /// <summary>
        /// Raised after a command is redone.
        /// </summary>
        public event Action<ICommand> OnRedo;

        /// <summary>
        /// Raised whenever undo/redo history content changes.
        /// </summary>
        public event Action OnHistoryChanged;

        /// <summary>
        /// Executes a command, stores it in undo history, and clears redo history.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
        public void ExecuteCommand(ICommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            command.Execute();
            _undoStack.AddLast(command);

            while (_undoStack.Count > _maxCapacity)
            {
                _undoStack.RemoveFirst();
            }

            _redoStack.Clear();
            OnCommandExecuted?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Undoes the most recently executed command if available.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undoStack.Last.Value;
            _undoStack.RemoveLast();
            command.Undo();
            _redoStack.Push(command);
            OnUndo?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Redoes the most recently undone command if available.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.AddLast(command);
            OnRedo?.Invoke(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Clears undo and redo histories.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
