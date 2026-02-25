using NUnit.Framework;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Commands;

namespace CircuitCraft.Tests.Commands
{
    /// <summary>
    /// Characterization tests for DeleteTraceNetCommand — captures ALL current behavior as a
    /// safety net before any refactoring. Tests must pass against the CURRENT implementation as-is.
    ///
    /// DeleteTraceNetCommand captures the net's name, all traces, and all pin connections, then
    /// removes all traces (causing the net to be auto-deleted). Undo recreates the net and
    /// restores traces and pin connections, getting a new net ID.
    /// </summary>
    [TestFixture]
    public class DeleteTraceNetCommandTests
    {
        private BoardState _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardState(20, 20);
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

        private void ConnectPin(PlacedComponent comp, int pinIndex, Net net)
        {
            _board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, pinIndex, comp.GetPinWorldPosition(pinIndex)));
        }

        #endregion

        #region Description Tests

        [Test]
        public void Description_ContainsNetId()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);

            Assert.IsTrue(cmd.Description.Contains(net.NetId.ToString()),
                $"Description should contain net ID. Actual: '{cmd.Description}'");
        }

        [Test]
        public void Description_IsNotNullOrEmpty()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);

            Assert.IsNotNull(cmd.Description);
            Assert.IsNotEmpty(cmd.Description);
        }

        #endregion

        #region Execute Tests

        [Test]
        public void Execute_RemovesAllTraces()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            _board.AddTrace(net.NetId, new GridPosition(5, 0), new GridPosition(5, 5));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Traces.Count);
        }

        [Test]
        public void Execute_AutoDeletesNet_WhenLastTraceRemoved()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            int netId = net.NetId;

            var cmd = new DeleteTraceNetCommand(_board, netId);
            cmd.Execute();

            Assert.IsNull(_board.GetNet(netId), "Net should be auto-deleted when last trace is removed");
        }

        [Test]
        public void Execute_NonExistentNetId_DoesNotThrow()
        {
            var cmd = new DeleteTraceNetCommand(_board, 9999);

            Assert.DoesNotThrow(() => cmd.Execute());
        }

        [Test]
        public void Execute_NonExistentNetId_BoardUnchanged()
        {
            var net = _board.CreateNet("OTHER");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, 9999);
            cmd.Execute();

            Assert.AreEqual(1, _board.Traces.Count, "Only traces for the target net should be removed");
        }

        [Test]
        public void Execute_ClearsPinConnections_WhenNetAutoDeleted()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("NET_A");
            ConnectPin(comp, 0, net);
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            // Pin connection is cleared because net was auto-deleted by RemoveTrace
            Assert.IsNull(comp.Pins[0].ConnectedNetId,
                "CHARACTERIZATION: Pin connection cleared when net auto-deleted via RemoveTrace");
        }

        [Test]
        public void Execute_OnlyRemovesTargetNetTraces_LeavesOtherNetIntact()
        {
            var netA = _board.CreateNet("NET_A");
            var netB = _board.CreateNet("NET_B");
            _board.AddTrace(netA.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            _board.AddTrace(netB.NetId, new GridPosition(0, 5), new GridPosition(5, 5));

            var cmd = new DeleteTraceNetCommand(_board, netA.NetId);
            cmd.Execute();

            Assert.AreEqual(1, _board.Traces.Count, "NET_B trace should remain");
            Assert.IsNotNull(_board.GetNet(netB.NetId), "NET_B should still exist");
        }

        [Test]
        public void Execute_SingleTrace_RemovesItAndDeletesNet()
        {
            var net = _board.CreateNet("MY_NET");
            _board.AddTrace(net.NetId, new GridPosition(1, 1), new GridPosition(4, 1));
            int savedNetId = net.NetId;

            var cmd = new DeleteTraceNetCommand(_board, savedNetId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Traces.Count);
            Assert.IsNull(_board.GetNet(savedNetId));
        }

        #endregion

        #region Undo Tests

        [Test]
        public void Undo_WithoutExecute_DoesNothing()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            int traceCountBefore = _board.Traces.Count;

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            // Undo without Execute — hasCapturedState is false → no-op
            Assert.DoesNotThrow(() => cmd.Undo());
            Assert.AreEqual(traceCountBefore, _board.Traces.Count);
        }

        [Test]
        public void Undo_AfterExecute_RecreatesNet()
        {
            var net = _board.CreateNet("MY_NET");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Nets.Count, "Net should be gone after Execute");

            cmd.Undo();

            Assert.AreEqual(1, _board.Nets.Count, "Net should be recreated after Undo");
        }

        [Test]
        public void Undo_AfterExecute_RecreatesNetWithSameName()
        {
            var net = _board.CreateNet("SPECIAL_NET");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();
            cmd.Undo();

            var recreated = _board.GetNetByName("SPECIAL_NET");
            Assert.IsNotNull(recreated, "Recreated net should have the same name");
        }

        [Test]
        public void Undo_AfterExecute_RestoredNetHasNewId()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            int originalNetId = net.NetId;

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();
            cmd.Undo();

            var recreated = _board.GetNetByName("NET_A");
            Assert.IsNotNull(recreated);
            Assert.AreNotEqual(originalNetId, recreated.NetId,
                "CHARACTERIZATION: Recreated net gets a new auto-incremented NetId");
        }

        [Test]
        public void Undo_AfterExecute_RestoresSingleTrace()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Traces.Count);

            cmd.Undo();

            Assert.AreEqual(1, _board.Traces.Count, "Trace should be restored after Undo");
        }

        [Test]
        public void Undo_AfterExecute_RestoresMultipleTraces()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            _board.AddTrace(net.NetId, new GridPosition(5, 0), new GridPosition(5, 5));
            _board.AddTrace(net.NetId, new GridPosition(5, 5), new GridPosition(8, 5));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            Assert.AreEqual(0, _board.Traces.Count);

            cmd.Undo();

            Assert.AreEqual(3, _board.Traces.Count, "All 3 traces should be restored");
        }

        [Test]
        public void Undo_AfterExecute_RestoredTracesOnRestoredNet()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();
            cmd.Undo();

            var recreated = _board.GetNetByName("NET_A");
            Assert.IsNotNull(recreated);
            var traces = _board.GetTraces(recreated.NetId);
            Assert.AreEqual(1, traces.Count, "Restored trace should belong to the recreated net");
        }

        [Test]
        public void Undo_AfterExecute_RestoresPinConnections()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("VIN");
            ConnectPin(comp, 0, net);
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();

            // After execute: pin connection is cleared
            Assert.IsNull(comp.Pins[0].ConnectedNetId);

            cmd.Undo();

            // After undo: pin should be reconnected to the recreated net
            Assert.IsTrue(comp.Pins[0].ConnectedNetId.HasValue,
                "Pin connection should be restored after Undo");
        }

        [Test]
        public void Undo_AfterExecute_MultiplePinsRestored()
        {
            var comp1 = PlaceAt(0, 0);
            var comp2 = PlaceAt(5, 0);
            var net = _board.CreateNet("NET_A");
            ConnectPin(comp1, 0, net);
            ConnectPin(comp2, 0, net);
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var cmd = new DeleteTraceNetCommand(_board, net.NetId);
            cmd.Execute();
            cmd.Undo();

            var recreated = _board.GetNetByName("NET_A");
            Assert.IsNotNull(recreated);
            Assert.AreEqual(2, recreated.ConnectedPins.Count,
                "Both pin connections should be restored after Undo");
        }

        [Test]
        public void Undo_NonExistentNet_DoesNothing_SinceExecuteWasNoOp()
        {
            var cmd = new DeleteTraceNetCommand(_board, 9999);
            cmd.Execute(); // sets hasCapturedState = false

            Assert.DoesNotThrow(() => cmd.Undo());
            Assert.AreEqual(0, _board.Nets.Count);
        }

        #endregion

        #region Execute-Undo Cycle via CommandHistory

        [Test]
        public void ExecuteViaHistory_ThenUndo_ThenRedo_CycleCorrectly()
        {
            var net = _board.CreateNet("NET_A");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            var history = new CommandHistory();
            var cmd = new DeleteTraceNetCommand(_board, net.NetId);

            history.ExecuteCommand(cmd);
            Assert.AreEqual(0, _board.Traces.Count, "After execute: traces gone");
            Assert.IsNull(_board.GetNet(net.NetId), "After execute: net gone");

            history.Undo();
            Assert.AreEqual(1, _board.Traces.Count, "After undo: trace restored");
            Assert.IsNotNull(_board.GetNetByName("NET_A"), "After undo: net recreated");

            history.Redo();
            Assert.AreEqual(0, _board.Traces.Count, "After redo: traces gone again");
        }

        [Test]
        public void ExecuteViaHistory_WithPins_UndoRestoresPinConnections()
        {
            var comp = PlaceAt(0, 0);
            var net = _board.CreateNet("VIN");
            ConnectPin(comp, 0, net);
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));

            var history = new CommandHistory();
            var cmd = new DeleteTraceNetCommand(_board, net.NetId);

            history.ExecuteCommand(cmd);
            Assert.IsNull(comp.Pins[0].ConnectedNetId, "After execute: pin disconnected");

            history.Undo();
            Assert.IsTrue(comp.Pins[0].ConnectedNetId.HasValue,
                "After undo: pin should be reconnected");
        }

        [Test]
        public void ExecuteViaHistory_OnlyTargetNetDeleted_OtherNetsUnaffected()
        {
            var netA = _board.CreateNet("NET_A");
            var netB = _board.CreateNet("NET_B");
            _board.AddTrace(netA.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            _board.AddTrace(netB.NetId, new GridPosition(0, 5), new GridPosition(5, 5));

            var history = new CommandHistory();
            history.ExecuteCommand(new DeleteTraceNetCommand(_board, netA.NetId));

            Assert.IsNull(_board.GetNet(netA.NetId), "NET_A should be deleted");
            Assert.IsNotNull(_board.GetNet(netB.NetId), "NET_B should be unaffected");
            Assert.AreEqual(1, _board.Traces.Count, "NET_B trace should remain");
        }

        #endregion
    }
}
