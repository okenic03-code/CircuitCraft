using NUnit.Framework;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Commands;

namespace CircuitCraft.Tests.Commands
{
    /// <summary>
    /// Characterization tests for PlaceComponentCommand — captures ALL current behavior as a
    /// safety net before any refactoring.
    /// </summary>
    [TestFixture]
    public class PlaceComponentCommandTests
    {
        private BoardState _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardState(10, 10);
        }

        #region Helper Methods

        private static List<PinInstance> CreatePins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            return pins;
        }

        private PlaceComponentCommand MakeCommand(
            string defId = "resistor",
            int x = 3,
            int y = 3,
            int rotation = 0,
            int pinCount = 2,
            float? customValue = null)
        {
            return new PlaceComponentCommand(
                _board, defId, new GridPosition(x, y), rotation, CreatePins(pinCount), customValue);
        }

        #endregion

        #region Description Tests

        [Test]
        public void Description_ContainsComponentDefId()
        {
            var cmd = MakeCommand(defId: "led_red");
            Assert.IsTrue(cmd.Description.Contains("led_red"),
                $"Expected description to contain defId. Actual: '{cmd.Description}'");
        }

        [Test]
        public void Description_ContainsPosition()
        {
            var cmd = MakeCommand(x: 5, y: 7);
            Assert.IsTrue(cmd.Description.Contains("5") || cmd.Description.Contains("7"),
                $"Expected description to contain position. Actual: '{cmd.Description}'");
        }

        [Test]
        public void Description_IsNotNullOrEmpty()
        {
            var cmd = MakeCommand();
            Assert.IsNotNull(cmd.Description);
            Assert.IsNotEmpty(cmd.Description);
        }

        #endregion

        #region Execute Tests

        [Test]
        public void Execute_PlacesComponentOnBoard()
        {
            var cmd = MakeCommand(x: 3, y: 3);
            Assert.AreEqual(0, _board.Components.Count);

            cmd.Execute();

            Assert.AreEqual(1, _board.Components.Count);
        }

        [Test]
        public void Execute_ComponentHasCorrectDefId()
        {
            var cmd = MakeCommand(defId: "capacitor_100nf", x: 0, y: 0);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.AreEqual("capacitor_100nf", comp.ComponentDefinitionId);
        }

        [Test]
        public void Execute_ComponentHasCorrectPosition()
        {
            var cmd = MakeCommand(x: 4, y: 6);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.AreEqual(new GridPosition(4, 6), comp.Position);
        }

        [Test]
        public void Execute_ComponentHasCorrectRotation()
        {
            var cmd = MakeCommand(rotation: 90);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.AreEqual(90, comp.Rotation);
        }

        [Test]
        public void Execute_ComponentHasCorrectPinCount()
        {
            var cmd = MakeCommand(pinCount: 3);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.AreEqual(3, comp.Pins.Count);
        }

        [Test]
        public void Execute_ComponentHasCorrectCustomValue()
        {
            var cmd = MakeCommand(customValue: 4700f);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.IsTrue(comp.CustomValue.HasValue);
            Assert.AreEqual(4700f, comp.CustomValue.Value, 0.001f);
        }

        [Test]
        public void Execute_ComponentHasNullCustomValue_WhenNotProvided()
        {
            var cmd = MakeCommand(customValue: null);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.IsFalse(comp.CustomValue.HasValue);
        }

        [Test]
        public void Execute_ComponentHasZeroCustomValue_WhenExplicitlySet()
        {
            var cmd = MakeCommand(customValue: 0f);
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.IsTrue(comp.CustomValue.HasValue);
            Assert.AreEqual(0f, comp.CustomValue.Value, 0.001f);
        }

        [Test]
        public void Execute_ComponentIsNotFixed()
        {
            var cmd = MakeCommand();
            cmd.Execute();

            var comp = _board.Components[0];
            Assert.IsFalse(comp.IsFixed);
        }

        [Test]
        public void Execute_WithNullPins_StillPlacesComponent()
        {
            var cmd = new PlaceComponentCommand(
                _board, "resistor", new GridPosition(0, 0), 0, null);

            cmd.Execute();

            Assert.AreEqual(1, _board.Components.Count);
            Assert.AreEqual(0, _board.Components[0].Pins.Count);
        }

        [Test]
        public void Execute_Twice_PlacesSecondComponentWithDifferentId()
        {
            var cmd1 = MakeCommand(x: 0, y: 0);
            var cmd2 = MakeCommand(x: 5, y: 5);

            cmd1.Execute();
            cmd2.Execute();

            Assert.AreEqual(2, _board.Components.Count);
            Assert.AreNotEqual(_board.Components[0].InstanceId, _board.Components[1].InstanceId);
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_AfterExecute_RemovesComponentFromBoard()
        {
            var cmd = MakeCommand();
            cmd.Execute();

            Assert.AreEqual(1, _board.Components.Count);

            cmd.Undo();

            Assert.AreEqual(0, _board.Components.Count);
        }

        [Test]
        public void Undo_AfterExecute_PositionBecomesVacant()
        {
            var cmd = MakeCommand(x: 3, y: 3);
            cmd.Execute();
            cmd.Undo();

            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        [Test]
        public void Undo_AfterExecute_ComponentNotFoundById()
        {
            var cmd = MakeCommand();
            cmd.Execute();
            int placedId = _board.Components[0].InstanceId;

            cmd.Undo();

            Assert.IsNull(_board.GetComponent(placedId));
        }

        [Test]
        public void Undo_WithoutExecute_DoesNotThrow()
        {
            var cmd = MakeCommand();

            // Undo without Execute — should not throw (no placed instance id yet)
            Assert.DoesNotThrow(() => cmd.Undo());
        }

        [Test]
        public void Undo_AfterExecute_BoardIsEmpty()
        {
            var cmd = MakeCommand();
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual(0, _board.Components.Count);
        }

        #endregion

        #region Execute-Undo Cycle Tests

        [Test]
        public void ExecuteUndo_ThenExecuteAgain_PlacesComponentAtSamePosition()
        {
            var cmd = MakeCommand(x: 2, y: 2);

            cmd.Execute();
            int firstId = _board.Components[0].InstanceId;
            cmd.Undo();
            cmd.Execute();
            int secondId = _board.Components[0].InstanceId;

            // Both placed at same position but different InstanceIds (auto-increment)
            Assert.AreEqual(1, _board.Components.Count);
            Assert.AreEqual(new GridPosition(2, 2), _board.Components[0].Position);
            Assert.AreNotEqual(firstId, secondId, "Re-execute should produce a new InstanceId");
        }

        [Test]
        public void Execute_ViaCommandHistory_ThenUndo_RemovesComponent()
        {
            var history = new CommandHistory();
            var cmd = MakeCommand();

            history.ExecuteCommand(cmd);
            Assert.AreEqual(1, _board.Components.Count);

            history.Undo();
            Assert.AreEqual(0, _board.Components.Count);
        }

        [Test]
        public void Execute_ViaCommandHistory_ThenUndo_ThenRedo_ReplacesComponent()
        {
            var history = new CommandHistory();
            var cmd = MakeCommand();

            history.ExecuteCommand(cmd);
            history.Undo();
            history.Redo();

            Assert.AreEqual(1, _board.Components.Count);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Execute_WithRotation270_IsAccepted()
        {
            var cmd = MakeCommand(rotation: 270);
            cmd.Execute();

            Assert.AreEqual(270, _board.Components[0].Rotation);
        }

        [Test]
        public void Execute_WithRotation180_IsAccepted()
        {
            var cmd = MakeCommand(rotation: 180);
            cmd.Execute();

            Assert.AreEqual(180, _board.Components[0].Rotation);
        }

        [Test]
        public void Execute_WithEmptyPinList_ComponentHasZeroPins()
        {
            var cmd = new PlaceComponentCommand(
                _board, "ground", new GridPosition(0, 0), 0, new List<PinInstance>());

            cmd.Execute();

            Assert.AreEqual(0, _board.Components[0].Pins.Count);
        }

        [Test]
        public void Execute_WithSinglePin_ComponentHasOnePin()
        {
            var pins = new List<PinInstance> { new PinInstance(0, "anode", new GridPosition(0, 0)) };
            var cmd = new PlaceComponentCommand(_board, "probe", new GridPosition(0, 0), 0, pins);

            cmd.Execute();

            Assert.AreEqual(1, _board.Components[0].Pins.Count);
        }

        #endregion
    }
}
