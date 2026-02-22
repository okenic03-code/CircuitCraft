using NUnit.Framework;
using System;
using System.Collections.Generic;
using CircuitCraft.Commands;

namespace CircuitCraft.Tests.Commands
{
    /// <summary>
    /// Characterization tests for CommandHistory — captures ALL current behavior as a safety net
    /// before any refactoring. Tests must pass against the CURRENT implementation as-is.
    /// </summary>
    [TestFixture]
    public class CommandHistoryTests
    {
        private CommandHistory _history;

        [SetUp]
        public void SetUp()
        {
            _history = new CommandHistory();
        }

        #region Helper Types

        /// <summary>
        /// Simple fake command that tracks execute/undo call counts.
        /// </summary>
        private class FakeCommand : ICommand
        {
            public int ExecuteCount { get; private set; }
            public int UndoCount { get; private set; }
            public string Description { get; }

            public FakeCommand(string description = "fake")
            {
                Description = description;
            }

            public void Execute() => ExecuteCount++;
            public void Undo() => UndoCount++;
        }

        #endregion

        #region Initial State Tests

        [Test]
        public void NewHistory_CanUndo_IsFalse()
        {
            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void NewHistory_CanRedo_IsFalse()
        {
            Assert.IsFalse(_history.CanRedo);
        }

        #endregion

        #region ExecuteCommand Tests

        [Test]
        public void ExecuteCommand_CallsCommandExecute()
        {
            var cmd = new FakeCommand();
            _history.ExecuteCommand(cmd);

            Assert.AreEqual(1, cmd.ExecuteCount);
        }

        [Test]
        public void ExecuteCommand_MakesCanUndoTrue()
        {
            _history.ExecuteCommand(new FakeCommand());

            Assert.IsTrue(_history.CanUndo);
        }

        [Test]
        public void ExecuteCommand_DoesNotMakeCanRedoTrue()
        {
            _history.ExecuteCommand(new FakeCommand());

            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void ExecuteCommand_NullCommand_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _history.ExecuteCommand(null));
        }

        [Test]
        public void ExecuteCommand_ClearsRedoStack()
        {
            // Build a redo entry
            _history.ExecuteCommand(new FakeCommand("A"));
            _history.Undo(); // CanRedo is now true

            Assert.IsTrue(_history.CanRedo);

            // Execute a new command — must clear the redo stack
            _history.ExecuteCommand(new FakeCommand("B"));

            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void ExecuteCommand_MultipleCommands_AllExecutedInOrder()
        {
            var order = new List<int>();
            var cmd1 = new FakeCommand("c1");
            var cmd2 = new FakeCommand("c2");
            var cmd3 = new FakeCommand("c3");

            _history.ExecuteCommand(cmd1);
            _history.ExecuteCommand(cmd2);
            _history.ExecuteCommand(cmd3);

            Assert.AreEqual(1, cmd1.ExecuteCount);
            Assert.AreEqual(1, cmd2.ExecuteCount);
            Assert.AreEqual(1, cmd3.ExecuteCount);
        }

        [Test]
        public void ExecuteCommand_FiresOnCommandExecutedEvent()
        {
            ICommand capturedCommand = null;
            _history.OnCommandExecuted += cmd => capturedCommand = cmd;

            var fake = new FakeCommand();
            _history.ExecuteCommand(fake);

            Assert.IsNotNull(capturedCommand);
            Assert.AreSame(fake, capturedCommand);
        }

        [Test]
        public void ExecuteCommand_FiresOnHistoryChangedEvent()
        {
            int changeCount = 0;
            _history.OnHistoryChanged += () => changeCount++;

            _history.ExecuteCommand(new FakeCommand());

            Assert.AreEqual(1, changeCount);
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_AfterExecute_CallsCommandUndo()
        {
            var cmd = new FakeCommand();
            _history.ExecuteCommand(cmd);

            _history.Undo();

            Assert.AreEqual(1, cmd.UndoCount);
        }

        [Test]
        public void Undo_AfterExecute_MakesCanUndoFalse_WhenNothingLeft()
        {
            _history.ExecuteCommand(new FakeCommand());

            _history.Undo();

            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void Undo_AfterExecute_MakesCanRedoTrue()
        {
            _history.ExecuteCommand(new FakeCommand());

            _history.Undo();

            Assert.IsTrue(_history.CanRedo);
        }

        [Test]
        public void Undo_WhenCanUndoFalse_DoesNothing()
        {
            // Should not throw
            Assert.DoesNotThrow(() => _history.Undo());
        }

        [Test]
        public void Undo_UndoesInReverseOrder()
        {
            var order = new List<string>();
            var cmd1 = new FakeCommand("A");
            var cmd2 = new FakeCommand("B");
            _history.ExecuteCommand(cmd1);
            _history.ExecuteCommand(cmd2);

            _history.Undo();
            _history.Undo();

            // cmd2 undone first, then cmd1
            Assert.AreEqual(1, cmd2.UndoCount);
            Assert.AreEqual(1, cmd1.UndoCount);
        }

        [Test]
        public void Undo_FiresOnUndoEvent()
        {
            ICommand capturedCommand = null;
            var fake = new FakeCommand();
            _history.ExecuteCommand(fake);
            _history.OnUndo += cmd => capturedCommand = cmd;

            _history.Undo();

            Assert.IsNotNull(capturedCommand);
            Assert.AreSame(fake, capturedCommand);
        }

        [Test]
        public void Undo_FiresOnHistoryChangedEvent()
        {
            _history.ExecuteCommand(new FakeCommand());

            int changeCount = 0;
            _history.OnHistoryChanged += () => changeCount++;

            _history.Undo();

            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void Undo_WhenEmpty_DoesNotFireOnUndoEvent()
        {
            bool fired = false;
            _history.OnUndo += _ => fired = true;

            _history.Undo();

            Assert.IsFalse(fired);
        }

        #endregion

        #region Redo Tests

        [Test]
        public void Redo_AfterUndo_ReExecutesCommand()
        {
            var cmd = new FakeCommand();
            _history.ExecuteCommand(cmd);
            _history.Undo();

            _history.Redo();

            Assert.AreEqual(2, cmd.ExecuteCount, "Command Execute should be called a second time on Redo");
        }

        [Test]
        public void Redo_AfterUndo_MakesCanRedoFalse_WhenNothingLeft()
        {
            _history.ExecuteCommand(new FakeCommand());
            _history.Undo();

            _history.Redo();

            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void Redo_AfterUndo_MakesCanUndoTrue()
        {
            _history.ExecuteCommand(new FakeCommand());
            _history.Undo();

            _history.Redo();

            Assert.IsTrue(_history.CanUndo);
        }

        [Test]
        public void Redo_WhenCanRedoFalse_DoesNothing()
        {
            Assert.DoesNotThrow(() => _history.Redo());
        }

        [Test]
        public void Redo_WhenEmpty_DoesNotFireOnRedoEvent()
        {
            bool fired = false;
            _history.OnRedo += _ => fired = true;

            _history.Redo();

            Assert.IsFalse(fired);
        }

        [Test]
        public void Redo_FiresOnRedoEvent()
        {
            ICommand capturedCommand = null;
            var fake = new FakeCommand();
            _history.ExecuteCommand(fake);
            _history.Undo();
            _history.OnRedo += cmd => capturedCommand = cmd;

            _history.Redo();

            Assert.IsNotNull(capturedCommand);
            Assert.AreSame(fake, capturedCommand);
        }

        [Test]
        public void Redo_FiresOnHistoryChangedEvent()
        {
            _history.ExecuteCommand(new FakeCommand());
            _history.Undo();

            int changeCount = 0;
            _history.OnHistoryChanged += () => changeCount++;

            _history.Redo();

            Assert.AreEqual(1, changeCount);
        }

        #endregion

        #region Execute-Undo-Redo Cycle Tests

        [Test]
        public void FullCycle_Execute_Undo_Redo_ProducesCorrectState()
        {
            var cmd = new FakeCommand();
            _history.ExecuteCommand(cmd);

            Assert.IsTrue(_history.CanUndo, "After execute: CanUndo");
            Assert.IsFalse(_history.CanRedo, "After execute: CanRedo false");

            _history.Undo();

            Assert.IsFalse(_history.CanUndo, "After undo: CanUndo false");
            Assert.IsTrue(_history.CanRedo, "After undo: CanRedo true");

            _history.Redo();

            Assert.IsTrue(_history.CanUndo, "After redo: CanUndo true");
            Assert.IsFalse(_history.CanRedo, "After redo: CanRedo false");

            Assert.AreEqual(2, cmd.ExecuteCount, "Execute count: initial + redo");
            Assert.AreEqual(1, cmd.UndoCount);
        }

        [Test]
        public void MultipleUndo_ThenMultipleRedo_CorrectlyRestoresStack()
        {
            var cmd1 = new FakeCommand("A");
            var cmd2 = new FakeCommand("B");
            _history.ExecuteCommand(cmd1);
            _history.ExecuteCommand(cmd2);

            _history.Undo();
            _history.Undo();

            Assert.IsFalse(_history.CanUndo);
            Assert.IsTrue(_history.CanRedo);

            _history.Redo();
            _history.Redo();

            Assert.IsTrue(_history.CanUndo);
            Assert.IsFalse(_history.CanRedo);
            Assert.AreEqual(2, cmd1.ExecuteCount);
            Assert.AreEqual(2, cmd2.ExecuteCount);
        }

        #endregion

        #region Capacity Limit Tests

        [Test]
        public void CapacityLimit_OldestCommandEvicted_WhenExceeded()
        {
            var history = new CommandHistory(maxCapacity: 3);

            var oldest = new FakeCommand("oldest");
            history.ExecuteCommand(oldest);
            history.ExecuteCommand(new FakeCommand("B"));
            history.ExecuteCommand(new FakeCommand("C"));
            history.ExecuteCommand(new FakeCommand("newest")); // exceeds capacity by 1

            // Can still undo 3 times (capacity 3)
            history.Undo();
            history.Undo();
            history.Undo();

            // Should NOT be able to undo the evicted 'oldest' command
            Assert.IsFalse(history.CanUndo, "Oldest command should have been evicted");
            // oldest should never have been undone
            Assert.AreEqual(0, oldest.UndoCount);
        }

        [Test]
        public void CapacityLimit_ExactlyAtCapacity_NoEviction()
        {
            var history = new CommandHistory(maxCapacity: 3);

            var first = new FakeCommand("first");
            history.ExecuteCommand(first);
            history.ExecuteCommand(new FakeCommand("B"));
            history.ExecuteCommand(new FakeCommand("C")); // exactly at capacity

            // All 3 should be undoable
            history.Undo();
            history.Undo();
            history.Undo();

            Assert.IsFalse(history.CanUndo);
            Assert.AreEqual(1, first.UndoCount, "First command should be undone (not evicted)");
        }

        [Test]
        public void CapacityLimit_CapacityOne_OnlyOneCommandUndoable()
        {
            var history = new CommandHistory(maxCapacity: 1);

            var first = new FakeCommand("first");
            var second = new FakeCommand("second");
            history.ExecuteCommand(first);
            history.ExecuteCommand(second); // evicts first

            history.Undo();

            Assert.IsFalse(history.CanUndo);
            Assert.AreEqual(0, first.UndoCount, "First command should have been evicted");
            Assert.AreEqual(1, second.UndoCount);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_MakesCanUndoFalse()
        {
            _history.ExecuteCommand(new FakeCommand());

            _history.Clear();

            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void Clear_MakesCanRedoFalse()
        {
            _history.ExecuteCommand(new FakeCommand());
            _history.Undo();

            _history.Clear();

            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void Clear_FiresOnHistoryChangedEvent()
        {
            int changeCount = 0;
            _history.OnHistoryChanged += () => changeCount++;

            _history.Clear();

            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void Clear_OnEmptyHistory_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _history.Clear());
        }

        [Test]
        public void Clear_AfterClear_ExecuteCommandWorksNormally()
        {
            _history.ExecuteCommand(new FakeCommand());
            _history.Clear();

            var cmd = new FakeCommand();
            _history.ExecuteCommand(cmd);

            Assert.IsTrue(_history.CanUndo);
            Assert.AreEqual(1, cmd.ExecuteCount);
        }

        #endregion
    }
}
