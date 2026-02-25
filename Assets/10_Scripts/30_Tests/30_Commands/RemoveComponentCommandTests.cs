using NUnit.Framework;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Commands;

namespace CircuitCraft.Tests.Commands
{
    /// <summary>
    /// Characterization tests for RemoveComponentCommand — captures ALL current behavior as a
    /// safety net before any refactoring. Tests must pass against the CURRENT implementation as-is.
    /// </summary>
    [TestFixture]
    public class RemoveComponentCommandTests
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

        private PlacedComponent PlaceAt(int x, int y, string defId = "resistor", int pinCount = 2)
        {
            return _board.PlaceComponent(defId, new GridPosition(x, y), 0, CreatePins(pinCount));
        }

        private PlacedComponent PlaceFixedAt(int x, int y, string defId = "vsource")
        {
            return _board.PlaceComponent(defId, new GridPosition(x, y), 0, CreatePins(2), null, isFixed: true);
        }

        private void ConnectPin(PlacedComponent comp, int pinIndex, Net net)
        {
            var pinRef = new PinReference(comp.InstanceId, pinIndex, comp.GetPinWorldPosition(pinIndex));
            _board.ConnectPinToNet(net.NetId, pinRef);
        }

        #endregion

        #region Description Tests

        [Test]
        public void Description_ContainsInstanceId()
        {
            var comp = PlaceAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            Assert.IsTrue(cmd.Description.Contains(comp.InstanceId.ToString()),
                $"Description should contain instance id {comp.InstanceId}. Actual: '{cmd.Description}'");
        }

        [Test]
        public void Description_IsNotNullOrEmpty()
        {
            var comp = PlaceAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            Assert.IsNotNull(cmd.Description);
            Assert.IsNotEmpty(cmd.Description);
        }

        #endregion

        #region Execute Tests

        [Test]
        public void Execute_RemovesComponentFromBoard()
        {
            var comp = PlaceAt(3, 3);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            cmd.Execute();

            Assert.AreEqual(0, _board.Components.Count);
        }

        [Test]
        public void Execute_PositionBecomesVacant()
        {
            var comp = PlaceAt(5, 5);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            cmd.Execute();

            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(5, 5)));
        }

        [Test]
        public void Execute_NonExistentInstanceId_DoesNotThrow()
        {
            var cmd = new RemoveComponentCommand(_board, 9999);

            Assert.DoesNotThrow(() => cmd.Execute());
        }

        [Test]
        public void Execute_NonExistentInstanceId_BoardUnchanged()
        {
            PlaceAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, 9999);

            cmd.Execute();

            Assert.AreEqual(1, _board.Components.Count);
        }

        [Test]
        public void Execute_FixedComponent_IsNotRemoved()
        {
            var comp = PlaceFixedAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            cmd.Execute();

            Assert.AreEqual(1, _board.Components.Count);
        }

        [Test]
        public void Execute_FixedComponent_DoesNotThrow()
        {
            var comp = PlaceFixedAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            Assert.DoesNotThrow(() => cmd.Execute());
        }

        [Test]
        public void Execute_RemovesConnectedTracesAtPinPosition()
        {
            // Place component at (2,2) with pin[0] at local (0,0) -> world (2,2)
            var comp = _board.PlaceComponent("resistor", new GridPosition(2, 2), 0,
                new[] { new PinInstance(0, "pin0", new GridPosition(0, 0)) });
            var net = _board.CreateNet("NET1");
            var pinWorldPos = comp.GetPinWorldPosition(0); // (2,2)
            _board.AddTrace(net.NetId, pinWorldPos, new GridPosition(pinWorldPos.X + 3, pinWorldPos.Y));

            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();

            // Trace touching the pin should be removed (auto-cleanup on RemoveComponent)
            Assert.AreEqual(0, _board.Traces.Count);
        }

        [Test]
        public void Execute_RemovesComponentFromPinNetConnection()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("NET1");
            ConnectPin(comp, 0, net);

            Assert.AreEqual(1, net.ConnectedPins.Count);

            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();

            // Net gets auto-deleted when last pin is removed
            Assert.IsNull(_board.GetNet(net.NetId));
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_WithoutExecute_DoesNothing()
        {
            var comp = PlaceAt(0, 0);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            // Undo without Execute - hasCapturedState is false, should be a no-op
            Assert.DoesNotThrow(() => cmd.Undo());
            Assert.AreEqual(1, _board.Components.Count, "Component should still be present");
        }

        [Test]
        public void Undo_AfterExecute_RestoresComponentToBoard()
        {
            var comp = PlaceAt(3, 3);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Components.Count);

            cmd.Undo();

            Assert.AreEqual(1, _board.Components.Count);
        }

        [Test]
        public void Undo_AfterExecute_ComponentHasSameDefId()
        {
            var comp = PlaceAt(3, 3, defId: "led_green");
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual("led_green", _board.Components[0].ComponentDefinitionId);
        }

        [Test]
        public void Undo_AfterExecute_ComponentHasSamePosition()
        {
            var comp = PlaceAt(4, 7);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual(new GridPosition(4, 7), _board.Components[0].Position);
        }

        [Test]
        public void Undo_AfterExecute_ComponentHasSamePinCount()
        {
            var comp = PlaceAt(0, 0, pinCount: 3);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual(3, _board.Components[0].Pins.Count);
        }

        [Test]
        public void Undo_AfterExecute_RestoresCustomValue()
        {
            var comp = _board.PlaceComponent("resistor", new GridPosition(0, 0), 0, CreatePins(2), 2200f);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            var restored = _board.Components[0];
            Assert.IsTrue(restored.CustomValue.HasValue);
            Assert.AreEqual(2200f, restored.CustomValue.Value, 0.001f);
        }

        [Test]
        public void Undo_AfterExecute_RestoredComponentHasNewInstanceId()
        {
            var comp = PlaceAt(0, 0);
            int originalId = comp.InstanceId;
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            // Restored component gets a new auto-incremented InstanceId (not the original one)
            int restoredId = _board.Components[0].InstanceId;
            Assert.AreNotEqual(originalId, restoredId,
                "CHARACTERIZATION: Restored component receives a new InstanceId (auto-increment)");
        }

        [Test]
        public void Undo_AfterExecute_RestoresNetConnection()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("NET_A");
            ConnectPin(comp, 0, net);

            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();

            // Net is gone after execute (auto-deleted when pin removed)
            Assert.IsNull(_board.GetNet(net.NetId));

            cmd.Undo();

            // Net should be recreated
            var restoredNet = _board.GetNetByName("NET_A");
            Assert.IsNotNull(restoredNet, "Net should be recreated after Undo");
            Assert.AreEqual(1, restoredNet.ConnectedPins.Count, "Restored net should have 1 pin");
        }

        [Test]
        public void Undo_AfterExecute_RestoresPinToRestoredNet()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("VIN");
            ConnectPin(comp, 0, net);

            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            var restoredComp = _board.Components[0];
            Assert.IsTrue(restoredComp.Pins[0].ConnectedNetId.HasValue,
                "Restored component's pin should be connected to a net");
        }

        [Test]
        public void Undo_FixedComponent_DoesNotRestore_SinceExecuteWasNoOp()
        {
            var comp = PlaceFixedAt(0, 0);
            int originalCount = _board.Components.Count;

            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute(); // no-op for fixed
            cmd.Undo();    // hasCapturedState=false → no-op

            Assert.AreEqual(originalCount, _board.Components.Count);
        }

        [Test]
        public void Undo_AfterExecute_PositionIsOccupied()
        {
            var comp = PlaceAt(3, 3);
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);
            cmd.Execute();
            cmd.Undo();

            Assert.IsTrue(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        #endregion

        #region Execute-Undo-Redo Cycle via CommandHistory

        [Test]
        public void ExecuteViaHistory_ThenUndo_ThenRedo_RemovesComponent()
        {
            var comp = PlaceAt(0, 0);
            var history = new CommandHistory();
            var cmd = new RemoveComponentCommand(_board, comp.InstanceId);

            history.ExecuteCommand(cmd);
            Assert.AreEqual(0, _board.Components.Count, "After execute: component removed");

            history.Undo();
            Assert.AreEqual(1, _board.Components.Count, "After undo: component restored");

            history.Redo();
            Assert.AreEqual(0, _board.Components.Count, "After redo: component removed again");
        }

        [Test]
        public void ExecuteViaHistory_MultipleComponents_OnlyTargetIsRemoved()
        {
            var comp1 = PlaceAt(0, 0);
            var comp2 = PlaceAt(5, 5);
            var history = new CommandHistory();
            var cmd = new RemoveComponentCommand(_board, comp1.InstanceId);

            history.ExecuteCommand(cmd);

            Assert.AreEqual(1, _board.Components.Count);
            Assert.AreEqual(comp2.InstanceId, _board.Components[0].InstanceId);
        }

        #endregion
    }
}
