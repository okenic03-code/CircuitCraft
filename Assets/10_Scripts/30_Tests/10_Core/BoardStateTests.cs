using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Tests.Core
{
    /// <summary>
    /// Characterization tests for BoardState — captures ALL current behavior as a safety net
    /// before any refactoring. Tests must pass against the CURRENT implementation as-is.
    /// </summary>
    [TestFixture]
    public class BoardStateTests
    {
        private BoardState _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardState(10, 10);
        }

        #region Helper Methods

        private static List<PinInstance> CreateTestPins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            return pins;
        }

        private PlacedComponent PlaceComponentAt(int x, int y, string defId = "test_comp", int pinCount = 2)
        {
            return _board.PlaceComponent(defId, new GridPosition(x, y), 0, CreateTestPins(pinCount));
        }

        private PlacedComponent PlaceFixedComponentAt(int x, int y, string defId = "test_comp")
        {
            return _board.PlaceComponent(defId, new GridPosition(x, y), 0, CreateTestPins(2), null, isFixed: true);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_CreatesBoardWithCorrectSuggestedBounds()
        {
            var board = new BoardState(10, 10);

            Assert.AreEqual(10, board.SuggestedBounds.Width);
            Assert.AreEqual(10, board.SuggestedBounds.Height);
            Assert.AreEqual(0, board.SuggestedBounds.MinX);
            Assert.AreEqual(0, board.SuggestedBounds.MinY);
        }

        [Test]
        public void Constructor_CreatesBoardWithNonSquareDimensions()
        {
            var board = new BoardState(20, 5);

            Assert.AreEqual(20, board.SuggestedBounds.Width);
            Assert.AreEqual(5, board.SuggestedBounds.Height);
        }

        [Test]
        public void Constructor_ComponentsAreInitiallyEmpty()
        {
            Assert.IsNotNull(_board.Components);
            Assert.AreEqual(0, _board.Components.Count);
        }

        [Test]
        public void Constructor_NetsAreInitiallyEmpty()
        {
            Assert.IsNotNull(_board.Nets);
            Assert.AreEqual(0, _board.Nets.Count);
        }

        [Test]
        public void Constructor_TracesAreInitiallyEmpty()
        {
            Assert.IsNotNull(_board.Traces);
            Assert.AreEqual(0, _board.Traces.Count);
        }

        [Test]
        public void Constructor_BoundsAliasSuggestedBounds()
        {
            var board = new BoardState(8, 6);
            Assert.AreEqual(board.SuggestedBounds, board.Bounds);
        }

        #endregion

        #region PlaceComponent Tests

        [Test]
        public void PlaceComponent_ReturnsPlacedComponentWithCorrectProperties()
        {
            var pins = CreateTestPins(2);
            var comp = _board.PlaceComponent("resistor_1k", new GridPosition(3, 4), 90, pins);

            Assert.IsNotNull(comp);
            Assert.AreEqual("resistor_1k", comp.ComponentDefinitionId);
            Assert.AreEqual(new GridPosition(3, 4), comp.Position);
            Assert.AreEqual(90, comp.Rotation);
        }

        [Test]
        public void PlaceComponent_AssignsAutoIncrementedInstanceId_StartingFromOne()
        {
            var comp1 = PlaceComponentAt(0, 0);
            var comp2 = PlaceComponentAt(5, 5);

            Assert.AreEqual(1, comp1.InstanceId);
            Assert.AreEqual(2, comp2.InstanceId);
        }

        [Test]
        public void PlaceComponent_StoresCustomValue()
        {
            var comp = _board.PlaceComponent("resistor", new GridPosition(0, 0), 0, CreateTestPins(2), customValue: 4700f);

            Assert.IsTrue(comp.CustomValue.HasValue);
            Assert.AreEqual(4700f, comp.CustomValue.Value, 0.001f);
        }

        [Test]
        public void PlaceComponent_NullCustomValueIsPreserved()
        {
            var comp = _board.PlaceComponent("resistor", new GridPosition(0, 0), 0, CreateTestPins(2), customValue: null);

            Assert.IsFalse(comp.CustomValue.HasValue);
        }

        [Test]
        public void PlaceComponent_StoresIsFixedFlag_WhenTrue()
        {
            var comp = PlaceFixedComponentAt(0, 0);

            Assert.IsTrue(comp.IsFixed);
        }

        [Test]
        public void PlaceComponent_StoresIsFixedFlag_WhenFalse()
        {
            var comp = PlaceComponentAt(0, 0);

            Assert.IsFalse(comp.IsFixed);
        }

        [Test]
        public void PlaceComponent_PinInstancesAreStoredCorrectly()
        {
            var pins = new List<PinInstance>
            {
                new PinInstance(0, "anode", new GridPosition(0, 0)),
                new PinInstance(1, "cathode", new GridPosition(1, 0))
            };
            var comp = _board.PlaceComponent("diode", new GridPosition(2, 2), 0, pins);

            Assert.AreEqual(2, comp.Pins.Count);
            Assert.AreEqual("anode", comp.Pins[0].PinName);
            Assert.AreEqual("cathode", comp.Pins[1].PinName);
        }

        [Test]
        public void PlaceComponent_AddsComponentToComponentsList()
        {
            PlaceComponentAt(0, 0);
            PlaceComponentAt(3, 3);

            Assert.AreEqual(2, _board.Components.Count);
        }

        [Test]
        public void PlaceComponent_DuplicatePosition_ThrowsInvalidOperationException()
        {
            PlaceComponentAt(2, 2);

            Assert.Throws<InvalidOperationException>(() => PlaceComponentAt(2, 2));
        }

        [Test]
        public void PlaceComponent_ValidRotations_AreAccepted()
        {
            // 0, 90, 180, 270 are all valid
            var c0 = _board.PlaceComponent("comp", new GridPosition(0, 0), 0, CreateTestPins(1));
            var c90 = _board.PlaceComponent("comp", new GridPosition(2, 0), 90, CreateTestPins(1));
            var c180 = _board.PlaceComponent("comp", new GridPosition(4, 0), 180, CreateTestPins(1));
            var c270 = _board.PlaceComponent("comp", new GridPosition(6, 0), 270, CreateTestPins(1));

            Assert.AreEqual(0, c0.Rotation);
            Assert.AreEqual(90, c90.Rotation);
            Assert.AreEqual(180, c180.Rotation);
            Assert.AreEqual(270, c270.Rotation);
        }

        #endregion

        #region RemoveComponent Tests

        [Test]
        public void RemoveComponent_SuccessfullyRemovesExistingComponent_ReturnsTrue()
        {
            var comp = PlaceComponentAt(0, 0);

            bool result = _board.RemoveComponent(comp.InstanceId);

            Assert.IsTrue(result);
            Assert.AreEqual(0, _board.Components.Count);
        }

        [Test]
        public void RemoveComponent_NonExistentId_ReturnsFalse()
        {
            bool result = _board.RemoveComponent(9999);

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveComponent_FixedComponent_ReturnsFalse_AndDoesNotRemove()
        {
            var comp = PlaceFixedComponentAt(0, 0);

            bool result = _board.RemoveComponent(comp.InstanceId);

            Assert.IsFalse(result);
            Assert.AreEqual(1, _board.Components.Count, "Fixed component should still be present");
        }

        [Test]
        public void RemoveComponent_ClearsPinConnectedNetId()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("VIN");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));
            _board.ConnectPinToNet(net.NetId, pinRef);

            // Verify pin is connected
            Assert.IsTrue(comp.Pins[0].ConnectedNetId.HasValue);

            _board.RemoveComponent(comp.InstanceId);

            // After removal, net should be auto-deleted (no pins left) — verify net is gone
            Assert.IsNull(_board.GetNet(net.NetId));
        }

        [Test]
        public void RemoveComponent_RemovesComponentFromPosition_PositionBecomesVacant()
        {
            var comp = PlaceComponentAt(3, 3);

            _board.RemoveComponent(comp.InstanceId);

            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        [Test]
        public void RemoveComponent_RemovesTracesConnectedToComponentPins()
        {
            // Place component with pin at (0,0) local which maps to world (2,2) + (0,0) = (2,2)
            var comp = _board.PlaceComponent("comp", new GridPosition(2, 2), 0,
                new[] { new PinInstance(0, "pin0", new GridPosition(0, 0)) });
            var net = _board.CreateNet("NET1");
            // Add a trace that starts at the pin world position
            var pinWorldPos = comp.GetPinWorldPosition(0); // (2,2)
            var trace = _board.AddTrace(net.NetId, pinWorldPos, new GridPosition(pinWorldPos.X + 3, pinWorldPos.Y));

            _board.RemoveComponent(comp.InstanceId);

            // The trace touching the pin should be removed
            Assert.AreEqual(0, _board.Traces.Count, "Traces touching removed component pins should be auto-removed");
        }

        [Test]
        public void RemoveComponent_AutoDeletesOrphanedNets_WhenNoPinsRemain()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("ORPHAN");
            _board.ConnectPinToNet(net.NetId, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));

            Assert.AreEqual(1, _board.Nets.Count);

            _board.RemoveComponent(comp.InstanceId);

            // Net with no remaining connected pins should be auto-deleted
            Assert.IsNull(_board.GetNet(net.NetId));
        }

        [Test]
        public void RemoveComponent_DoesNotDeleteNet_WhenOtherPinsStillConnected()
        {
            var comp1 = PlaceComponentAt(0, 0);
            var comp2 = PlaceComponentAt(5, 5);
            var net = _board.CreateNet("SHARED");
            _board.ConnectPinToNet(net.NetId, new PinReference(comp1.InstanceId, 0, comp1.GetPinWorldPosition(0)));
            _board.ConnectPinToNet(net.NetId, new PinReference(comp2.InstanceId, 0, comp2.GetPinWorldPosition(0)));

            _board.RemoveComponent(comp1.InstanceId);

            // Net still has comp2's pin, so it should survive
            Assert.IsNotNull(_board.GetNet(net.NetId), "Net with remaining pins should not be deleted");
        }

        #endregion

        #region CreateNet Tests

        [Test]
        public void CreateNet_ReturnsNetWithCorrectName()
        {
            var net = _board.CreateNet("VIN");

            Assert.IsNotNull(net);
            Assert.AreEqual("VIN", net.NetName);
        }

        [Test]
        public void CreateNet_AssignsAutoIncrementedNetId_StartingFromOne()
        {
            var net1 = _board.CreateNet("NET1");
            var net2 = _board.CreateNet("NET2");

            Assert.AreEqual(1, net1.NetId);
            Assert.AreEqual(2, net2.NetId);
        }

        [Test]
        public void CreateNet_AddsNetToNetsList()
        {
            _board.CreateNet("A");
            _board.CreateNet("B");
            _board.CreateNet("C");

            Assert.AreEqual(3, _board.Nets.Count);
        }

        [Test]
        public void CreateNet_CanCreateMultipleNets()
        {
            var vin = _board.CreateNet("VIN");
            var gnd = _board.CreateNet("GND");
            var vout = _board.CreateNet("VOUT");

            Assert.AreNotEqual(vin.NetId, gnd.NetId);
            Assert.AreNotEqual(gnd.NetId, vout.NetId);
            Assert.AreNotEqual(vin.NetId, vout.NetId);
        }

        [Test]
        public void CreateNet_NewNet_HasNoConnectedPins()
        {
            var net = _board.CreateNet("EMPTY");

            Assert.AreEqual(0, net.ConnectedPins.Count);
        }

        [Test]
        public void Net_IsGround_TrueForGNDName()
        {
            var gnd = _board.CreateNet("GND");
            Assert.IsTrue(gnd.IsGround);
        }

        [Test]
        public void Net_IsGround_TrueForZeroName()
        {
            var zero = _board.CreateNet("0");
            Assert.IsTrue(zero.IsGround);
        }

        [Test]
        public void Net_IsGround_FalseForOtherNames()
        {
            var vin = _board.CreateNet("VIN");
            Assert.IsFalse(vin.IsGround);
        }

        [Test]
        public void Net_IsPower_TrueForVPrefixedName()
        {
            var vin = _board.CreateNet("VIN");
            Assert.IsTrue(vin.IsPower);
        }

        [Test]
        public void Net_IsPower_FalseForNonVPrefixedName()
        {
            var gnd = _board.CreateNet("GND");
            Assert.IsFalse(gnd.IsPower);
        }

        #endregion

        #region ConnectPinToNet Tests

        [Test]
        public void ConnectPinToNet_UpdatesPinConnectedNetId()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("VIN");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            _board.ConnectPinToNet(net.NetId, pinRef);

            Assert.IsTrue(comp.Pins[0].ConnectedNetId.HasValue);
            Assert.AreEqual(net.NetId, comp.Pins[0].ConnectedNetId.Value);
        }

        [Test]
        public void ConnectPinToNet_AddsToNetConnectedPins()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("VIN");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            _board.ConnectPinToNet(net.NetId, pinRef);

            Assert.AreEqual(1, net.ConnectedPins.Count);
        }

        [Test]
        public void ConnectPinToNet_WhenPinAlreadyOnDifferentNet_DisconnectsFromPreviousNet()
        {
            var comp = PlaceComponentAt(0, 0);
            var netA = _board.CreateNet("NET_A");
            var netB = _board.CreateNet("NET_B");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            _board.ConnectPinToNet(netA.NetId, pinRef);
            Assert.AreEqual(1, netA.ConnectedPins.Count);

            _board.ConnectPinToNet(netB.NetId, pinRef);

            // Pin should no longer be on netA
            Assert.AreEqual(0, netA.ConnectedPins.Count, "Pin should be removed from previous net");
            // Pin should be on netB
            Assert.AreEqual(1, netB.ConnectedPins.Count);
            Assert.AreEqual(netB.NetId, comp.Pins[0].ConnectedNetId.Value);
        }

        [Test]
        public void ConnectPinToNet_AutoDeletesPreviousNet_WhenItBecomesEmpty()
        {
            var comp = PlaceComponentAt(0, 0);
            var netA = _board.CreateNet("NET_A");
            var netB = _board.CreateNet("NET_B");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            _board.ConnectPinToNet(netA.NetId, pinRef);
            // netA now has exactly 1 pin; moving it should orphan netA
            _board.ConnectPinToNet(netB.NetId, pinRef);

            Assert.IsNull(_board.GetNet(netA.NetId), "Empty previous net should be auto-deleted");
        }

        [Test]
        public void ConnectPinToNet_SameNet_DoesNotAddDuplicate()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("VIN");
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            _board.ConnectPinToNet(net.NetId, pinRef);
            _board.ConnectPinToNet(net.NetId, pinRef); // connect again to same net

            // Should not duplicate
            Assert.AreEqual(1, net.ConnectedPins.Count);
        }

        [Test]
        public void ConnectPinToNet_InvalidNetId_ThrowsArgumentException()
        {
            var comp = PlaceComponentAt(0, 0);
            var pinRef = new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0));

            Assert.Throws<ArgumentException>(() => _board.ConnectPinToNet(9999, pinRef));
        }

        [Test]
        public void ConnectPinToNet_InvalidComponentId_ThrowsArgumentException()
        {
            var net = _board.CreateNet("VIN");
            var pinRef = new PinReference(9999, 0, new GridPosition(0, 0));

            Assert.Throws<ArgumentException>(() => _board.ConnectPinToNet(net.NetId, pinRef));
        }

        #endregion

        #region AddTrace Tests

        [Test]
        public void AddTrace_ReturnsTraceWithCorrectProperties()
        {
            var net = _board.CreateNet("VIN");
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));

            Assert.IsNotNull(trace);
            Assert.AreEqual(net.NetId, trace.NetId);
            Assert.AreEqual(new GridPosition(0, 0), trace.Start);
            Assert.AreEqual(new GridPosition(0, 3), trace.End);
        }

        [Test]
        public void AddTrace_AssignsAutoIncrementedSegmentId_StartingFromOne()
        {
            var net = _board.CreateNet("VIN");
            var t1 = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));
            var t2 = _board.AddTrace(net.NetId, new GridPosition(1, 0), new GridPosition(1, 3));

            Assert.AreEqual(1, t1.SegmentId);
            Assert.AreEqual(2, t2.SegmentId);
        }

        [Test]
        public void AddTrace_AddsToTracesList()
        {
            var net = _board.CreateNet("VIN");
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(3, 0));

            Assert.AreEqual(1, _board.Traces.Count);
        }

        [Test]
        public void AddTrace_InvalidNetId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _board.AddTrace(9999, new GridPosition(0, 0), new GridPosition(0, 3)));
        }

        [Test]
        public void AddTrace_HorizontalTrace_IsAccepted()
        {
            var net = _board.CreateNet("H");
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 5), new GridPosition(5, 5));

            Assert.AreEqual(new GridPosition(0, 5), trace.Start);
            Assert.AreEqual(new GridPosition(5, 5), trace.End);
        }

        [Test]
        public void AddTrace_VerticalTrace_IsAccepted()
        {
            var net = _board.CreateNet("V");
            var trace = _board.AddTrace(net.NetId, new GridPosition(3, 0), new GridPosition(3, 7));

            Assert.AreEqual(new GridPosition(3, 0), trace.Start);
            Assert.AreEqual(new GridPosition(3, 7), trace.End);
        }

        #endregion

        #region RemoveTrace Tests

        [Test]
        public void RemoveTrace_SuccessfullyRemovesTrace_ReturnsTrue()
        {
            var net = _board.CreateNet("VIN");
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));

            bool result = _board.RemoveTrace(trace.SegmentId);

            Assert.IsTrue(result);
            Assert.AreEqual(0, _board.Traces.Count);
        }

        [Test]
        public void RemoveTrace_NonExistentSegmentId_ReturnsFalse()
        {
            bool result = _board.RemoveTrace(9999);

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveTrace_WhenNetHasNoMoreTraces_AndNoConnectedPins_AutoDeletesNet()
        {
            var net = _board.CreateNet("ORPHAN_NET");
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));

            _board.RemoveTrace(trace.SegmentId);

            // Net with no more traces and no pins should be auto-deleted
            Assert.IsNull(_board.GetNet(net.NetId), "Net with no traces and no pins should be auto-deleted");
        }

        [Test]
        public void RemoveTrace_WhenNetHasNoMoreTraces_ClearsPinConnections()
        {
            var comp = PlaceComponentAt(0, 0);
            var net = _board.CreateNet("NET1");
            _board.ConnectPinToNet(net.NetId, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));

            _board.RemoveTrace(trace.SegmentId);

            // Pin should have its connection cleared when the last trace is removed
            Assert.IsNull(comp.Pins[0].ConnectedNetId,
                "Pin ConnectedNetId should be cleared when last trace is removed");
        }

        [Test]
        public void RemoveTrace_WhenNetStillHasOtherTraces_DoesNotDeleteNet()
        {
            var net = _board.CreateNet("NET1");
            var t1 = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));
            var t2 = _board.AddTrace(net.NetId, new GridPosition(0, 3), new GridPosition(5, 3));

            _board.RemoveTrace(t1.SegmentId);

            Assert.IsNotNull(_board.GetNet(net.NetId), "Net should survive while it still has traces");
            Assert.AreEqual(1, _board.Traces.Count);
        }

        [Test]
        public void RemoveTrace_MultipleTraces_OnlyRemovesTargetTrace()
        {
            var net = _board.CreateNet("NET1");
            var t1 = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));
            var t2 = _board.AddTrace(net.NetId, new GridPosition(0, 3), new GridPosition(5, 3));
            var t3 = _board.AddTrace(net.NetId, new GridPosition(5, 3), new GridPosition(5, 0));

            _board.RemoveTrace(t2.SegmentId);

            Assert.AreEqual(2, _board.Traces.Count);
            Assert.IsNotNull(_board.Traces.FirstOrDefault(t => t.SegmentId == t1.SegmentId));
            Assert.IsNull(_board.Traces.FirstOrDefault(t => t.SegmentId == t2.SegmentId));
            Assert.IsNotNull(_board.Traces.FirstOrDefault(t => t.SegmentId == t3.SegmentId));
        }

        #endregion

        #region GetComponent / GetComponentAt / IsPositionOccupied Tests

        [Test]
        public void GetComponent_ReturnsCorrectComponent_ByInstanceId()
        {
            var comp = PlaceComponentAt(2, 3);

            var found = _board.GetComponent(comp.InstanceId);

            Assert.IsNotNull(found);
            Assert.AreEqual(comp.InstanceId, found.InstanceId);
            Assert.AreEqual(new GridPosition(2, 3), found.Position);
        }

        [Test]
        public void GetComponent_ReturnsNull_ForNonExistentInstanceId()
        {
            var found = _board.GetComponent(9999);

            Assert.IsNull(found);
        }

        [Test]
        public void GetComponentAt_ReturnsComponent_AtOccupiedPosition()
        {
            var comp = PlaceComponentAt(4, 6);

            var found = _board.GetComponentAt(new GridPosition(4, 6));

            Assert.IsNotNull(found);
            Assert.AreEqual(comp.InstanceId, found.InstanceId);
        }

        [Test]
        public void GetComponentAt_ReturnsNull_AtEmptyPosition()
        {
            var found = _board.GetComponentAt(new GridPosition(7, 7));

            Assert.IsNull(found);
        }

        [Test]
        public void IsPositionOccupied_ReturnsTrue_WhenComponentPlacedAt()
        {
            PlaceComponentAt(3, 3);

            Assert.IsTrue(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        [Test]
        public void IsPositionOccupied_ReturnsFalse_WhenNoComponentAt()
        {
            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        [Test]
        public void IsPositionOccupied_ReturnsFalse_AfterComponentRemoved()
        {
            var comp = PlaceComponentAt(3, 3);
            _board.RemoveComponent(comp.InstanceId);

            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(3, 3)));
        }

        #endregion

        #region GetNet / GetNetByName Tests

        [Test]
        public void GetNet_ReturnsCorrectNet_ByNetId()
        {
            var net = _board.CreateNet("VIN");

            var found = _board.GetNet(net.NetId);

            Assert.IsNotNull(found);
            Assert.AreEqual("VIN", found.NetName);
        }

        [Test]
        public void GetNet_ReturnsNull_ForNonExistentNetId()
        {
            var found = _board.GetNet(9999);

            Assert.IsNull(found);
        }

        [Test]
        public void GetNetByName_ReturnsCorrectNet_ByName()
        {
            _board.CreateNet("VIN");
            _board.CreateNet("GND");
            var vout = _board.CreateNet("VOUT");

            var found = _board.GetNetByName("VOUT");

            Assert.IsNotNull(found);
            Assert.AreEqual(vout.NetId, found.NetId);
        }

        [Test]
        public void GetNetByName_ReturnsNull_WhenNameNotFound()
        {
            _board.CreateNet("VIN");

            var found = _board.GetNetByName("NONEXISTENT");

            Assert.IsNull(found);
        }

        [Test]
        public void GetNetByName_IsCaseSensitive()
        {
            _board.CreateNet("VIN");

            var found = _board.GetNetByName("vin");

            // Name lookup is case-sensitive (FirstOrDefault with ==)
            Assert.IsNull(found);
        }

        #endregion

        #region GetTraces Tests

        [Test]
        public void GetTraces_ReturnsTracesForGivenNetId()
        {
            var net1 = _board.CreateNet("NET1");
            var net2 = _board.CreateNet("NET2");
            _board.AddTrace(net1.NetId, new GridPosition(0, 0), new GridPosition(0, 3));
            _board.AddTrace(net1.NetId, new GridPosition(0, 3), new GridPosition(5, 3));
            _board.AddTrace(net2.NetId, new GridPosition(1, 1), new GridPosition(1, 5));

            var traces = _board.GetTraces(net1.NetId);

            Assert.AreEqual(2, traces.Count);
            Assert.IsTrue(traces.All(t => t.NetId == net1.NetId));
        }

        [Test]
        public void GetTraces_ReturnsEmptyList_ForNetWithNoTraces()
        {
            var net = _board.CreateNet("EMPTY");

            var traces = _board.GetTraces(net.NetId);

            Assert.IsNotNull(traces);
            Assert.AreEqual(0, traces.Count);
        }

        [Test]
        public void GetTraces_ReturnsEmptyList_ForNonExistentNetId()
        {
            var traces = _board.GetTraces(9999);

            Assert.IsNotNull(traces);
            Assert.AreEqual(0, traces.Count);
        }

        #endregion

        #region ComputeContentBounds Tests

        [Test]
        public void ComputeContentBounds_EmptyBoard_ReturnsSuggestedBounds()
        {
            var bounds = _board.ComputeContentBounds();

            Assert.AreEqual(_board.SuggestedBounds, bounds);
        }

        [Test]
        public void ComputeContentBounds_WithSingleComponent_ReturnsBoundsEnclosingIt()
        {
            // Place component at (3,4), pins at local (0,0) and (1,0) -> world (3,4) and (4,4)
            var pins = new List<PinInstance>
            {
                new PinInstance(0, "p0", new GridPosition(0, 0)),
                new PinInstance(1, "p1", new GridPosition(1, 0))
            };
            _board.PlaceComponent("comp", new GridPosition(3, 4), 0, pins);

            var bounds = _board.ComputeContentBounds();

            // Component origin is (3,4), pins at world (3,4) and (4,4)
            Assert.LessOrEqual(bounds.MinX, 3);
            Assert.LessOrEqual(bounds.MinY, 4);
            Assert.GreaterOrEqual(bounds.MaxX, 5); // MaxX is exclusive, rightmost pin is x=4, so MaxX >= 5
            Assert.GreaterOrEqual(bounds.MaxY, 5); // MaxY is exclusive, highest Y=4, so MaxY >= 5
        }

        [Test]
        public void ComputeContentBounds_WithMultipleComponents_ReturnsEnclosingBounds()
        {
            PlaceComponentAt(0, 0);
            PlaceComponentAt(8, 8);

            var bounds = _board.ComputeContentBounds();

            Assert.LessOrEqual(bounds.MinX, 0);
            Assert.LessOrEqual(bounds.MinY, 0);
            Assert.GreaterOrEqual(bounds.MaxX, 8);
            Assert.GreaterOrEqual(bounds.MaxY, 8);
        }

        [Test]
        public void ComputeContentBounds_IncludesTraceEndpoints()
        {
            var net = _board.CreateNet("VIN");
            // Add a trace from (0,0) to (9,0) — which extends to the edge
            _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(9, 0));

            var bounds = _board.ComputeContentBounds();

            Assert.LessOrEqual(bounds.MinX, 0);
            Assert.GreaterOrEqual(bounds.MaxX, 9); // must encompass x=9
        }

        [Test]
        public void ComputeContentBounds_IncludesComponentPinPositions()
        {
            // Place component at (5,5) with pins at local (2,0) -> world (7,5)
            var pins = new List<PinInstance>
            {
                new PinInstance(0, "p", new GridPosition(2, 0))
            };
            _board.PlaceComponent("comp", new GridPosition(5, 5), 0, pins);

            var bounds = _board.ComputeContentBounds();

            // Pin world pos is (7,5); bounds must cover it
            Assert.LessOrEqual(bounds.MinX, 5);
            Assert.GreaterOrEqual(bounds.MaxX, 8); // x=7 must be inside, MaxX is exclusive so >= 8
        }

        #endregion

        #region Event Firing Tests

        [Test]
        public void OnComponentPlaced_FiresWhenComponentPlaced()
        {
            PlacedComponent capturedComponent = null;
            _board.OnComponentPlaced += c => capturedComponent = c;

            var comp = PlaceComponentAt(0, 0);

            Assert.IsNotNull(capturedComponent);
            Assert.AreEqual(comp.InstanceId, capturedComponent.InstanceId);
        }

        [Test]
        public void OnComponentRemoved_FiresWhenComponentRemoved()
        {
            int capturedInstanceId = -1;
            var comp = PlaceComponentAt(0, 0);
            _board.OnComponentRemoved += id => capturedInstanceId = id;

            _board.RemoveComponent(comp.InstanceId);

            Assert.AreEqual(comp.InstanceId, capturedInstanceId);
        }

        [Test]
        public void OnComponentRemoved_DoesNotFire_WhenRemovingFixedComponent()
        {
            bool fired = false;
            var comp = PlaceFixedComponentAt(0, 0);
            _board.OnComponentRemoved += _ => fired = true;

            _board.RemoveComponent(comp.InstanceId);

            Assert.IsFalse(fired, "OnComponentRemoved should NOT fire for fixed components");
        }

        [Test]
        public void OnComponentRemoved_DoesNotFire_WhenRemovingNonExistentComponent()
        {
            bool fired = false;
            _board.OnComponentRemoved += _ => fired = true;

            _board.RemoveComponent(9999);

            Assert.IsFalse(fired);
        }

        [Test]
        public void OnNetCreated_FiresWhenNetCreated()
        {
            Net capturedNet = null;
            _board.OnNetCreated += n => capturedNet = n;

            var net = _board.CreateNet("VIN");

            Assert.IsNotNull(capturedNet);
            Assert.AreEqual("VIN", capturedNet.NetName);
        }

        [Test]
        public void OnTraceAdded_FiresWhenTraceAdded()
        {
            TraceSegment capturedTrace = null;
            _board.OnTraceAdded += t => capturedTrace = t;
            var net = _board.CreateNet("VIN");

            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));

            Assert.IsNotNull(capturedTrace);
            Assert.AreEqual(trace.SegmentId, capturedTrace.SegmentId);
        }

        [Test]
        public void OnTraceRemoved_FiresWhenTraceRemoved()
        {
            int capturedSegmentId = -1;
            var net = _board.CreateNet("VIN");
            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(0, 3));
            _board.OnTraceRemoved += id => capturedSegmentId = id;

            _board.RemoveTrace(trace.SegmentId);

            Assert.AreEqual(trace.SegmentId, capturedSegmentId);
        }

        [Test]
        public void OnTraceRemoved_DoesNotFire_ForNonExistentSegmentId()
        {
            bool fired = false;
            _board.OnTraceRemoved += _ => fired = true;

            _board.RemoveTrace(9999);

            Assert.IsFalse(fired);
        }

        [Test]
        public void OnPinsConnected_FiresWhenSecondPinConnectedToNet()
        {
            int capturedNetId = -1;
            _board.OnPinsConnected += (netId, p1, p2) => capturedNetId = netId;

            var comp1 = PlaceComponentAt(0, 0);
            var comp2 = PlaceComponentAt(5, 5);
            var net = _board.CreateNet("VIN");

            // First pin — no event yet
            _board.ConnectPinToNet(net.NetId, new PinReference(comp1.InstanceId, 0, comp1.GetPinWorldPosition(0)));
            Assert.AreEqual(-1, capturedNetId, "OnPinsConnected should not fire for first pin");

            // Second pin — event fires
            _board.ConnectPinToNet(net.NetId, new PinReference(comp2.InstanceId, 0, comp2.GetPinWorldPosition(0)));
            Assert.AreEqual(net.NetId, capturedNetId, "OnPinsConnected should fire when second pin connects");
        }

        #endregion

        #region ID Auto-Increment Isolation Tests

        [Test]
        public void InstanceIds_AreUnique_AcrossMultiplePlacements()
        {
            var ids = new HashSet<int>();
            for (int i = 0; i < 5; i++)
                ids.Add(PlaceComponentAt(i * 2, 0).InstanceId);

            Assert.AreEqual(5, ids.Count, "All instance IDs must be unique");
        }

        [Test]
        public void NetIds_AreUnique_AcrossMultipleCreateNetCalls()
        {
            var ids = new HashSet<int>();
            for (int i = 0; i < 5; i++)
                ids.Add(_board.CreateNet($"NET{i}").NetId);

            Assert.AreEqual(5, ids.Count, "All net IDs must be unique");
        }

        [Test]
        public void SegmentIds_AreUnique_AcrossMultipleAddTraceCalls()
        {
            var net = _board.CreateNet("VIN");
            var ids = new HashSet<int>();
            for (int i = 0; i < 5; i++)
                ids.Add(_board.AddTrace(net.NetId, new GridPosition(0, i), new GridPosition(3, i)).SegmentId);

            Assert.AreEqual(5, ids.Count, "All segment IDs must be unique");
        }

        #endregion

        #region Integration / Scenario Tests

        [Test]
        public void FullVoltageDividerTopology_IsTrackedCorrectly()
        {
            // Simulates: VIN -> R1 -> VOUT -> R2 -> GND
            var board = new BoardState(20, 10);
            var netVin = board.CreateNet("VIN");
            var netGnd = board.CreateNet("GND");
            var netVout = board.CreateNet("VOUT");

            var vSourcePins = new[]
            {
                new PinInstance(0, "positive", new GridPosition(0, 0)),
                new PinInstance(1, "negative", new GridPosition(0, 1))
            };
            var vsource = board.PlaceComponent("vsource_5v", new GridPosition(0, 0), 0, vSourcePins);
            board.ConnectPinToNet(netVin.NetId, new PinReference(vsource.InstanceId, 0, vsource.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netGnd.NetId, new PinReference(vsource.InstanceId, 1, vsource.GetPinWorldPosition(1)));

            var r1Pins = new[]
            {
                new PinInstance(0, "t1", new GridPosition(0, 0)),
                new PinInstance(1, "t2", new GridPosition(1, 0))
            };
            var r1 = board.PlaceComponent("resistor_1k", new GridPosition(3, 0), 0, r1Pins);
            board.ConnectPinToNet(netVin.NetId, new PinReference(r1.InstanceId, 0, r1.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netVout.NetId, new PinReference(r1.InstanceId, 1, r1.GetPinWorldPosition(1)));

            var r2Pins = new[]
            {
                new PinInstance(0, "t1", new GridPosition(0, 0)),
                new PinInstance(1, "t2", new GridPosition(1, 0))
            };
            var r2 = board.PlaceComponent("resistor_2k", new GridPosition(6, 0), 0, r2Pins);
            board.ConnectPinToNet(netVout.NetId, new PinReference(r2.InstanceId, 0, r2.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netGnd.NetId, new PinReference(r2.InstanceId, 1, r2.GetPinWorldPosition(1)));

            Assert.AreEqual(3, board.Components.Count, "Should have 3 components");
            Assert.AreEqual(3, board.Nets.Count, "Should have 3 nets");
            Assert.AreEqual(2, netVin.ConnectedPins.Count, "VIN net should have 2 pins (vsource+ and r1t1)");
            Assert.AreEqual(2, netGnd.ConnectedPins.Count, "GND net should have 2 pins");
            Assert.AreEqual(2, netVout.ConnectedPins.Count, "VOUT net should have 2 pins");
        }

        [Test]
        public void PlaceThenRemoveComponent_LeavesNoArtifacts()
        {
            var comp = PlaceComponentAt(5, 5);
            _board.RemoveComponent(comp.InstanceId);

            Assert.AreEqual(0, _board.Components.Count);
            Assert.IsNull(_board.GetComponent(comp.InstanceId));
            Assert.IsNull(_board.GetComponentAt(new GridPosition(5, 5)));
            Assert.IsFalse(_board.IsPositionOccupied(new GridPosition(5, 5)));
        }

        [Test]
        public void AddThenRemoveTrace_NetSurvives_WhenStillHasPinsConnected()
        {
            var comp1 = PlaceComponentAt(0, 0);
            var comp2 = PlaceComponentAt(5, 5);
            var net = _board.CreateNet("NET1");
            _board.ConnectPinToNet(net.NetId, new PinReference(comp1.InstanceId, 0, comp1.GetPinWorldPosition(0)));
            _board.ConnectPinToNet(net.NetId, new PinReference(comp2.InstanceId, 0, comp2.GetPinWorldPosition(0)));

            var trace = _board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            _board.RemoveTrace(trace.SegmentId);

            // Net has pins connected (comp1 and comp2), so it should not be deleted
            // BUT: current impl clears pins and deletes net when last trace is removed
            // This characterization test captures the ACTUAL behavior:
            Assert.IsNull(_board.GetNet(net.NetId),
                "CHARACTERIZATION: Current implementation deletes net and clears pins when last trace removed, even if pins were connected");
        }

        [Test]
        public void ToString_ReturnsNonEmptyString()
        {
            var str = _board.ToString();

            Assert.IsNotNull(str);
            Assert.IsNotEmpty(str);
        }

        #endregion
    }
}
