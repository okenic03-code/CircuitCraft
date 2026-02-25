using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CircuitCraft.Core;
using CircuitCraft.Systems;

namespace CircuitCraft.Tests.Systems
{
    [TestFixture]
    public class SaveLoadServiceTests
    {
        private SaveLoadService _service;
        private string _tempDirectory;

        // ── Setup / Teardown ────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _service = new SaveLoadService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "circuitcraft_test_" + Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>Creates a list of PinInstances with staggered horizontal local positions.</summary>
        private static List<PinInstance> CreatePins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            return pins;
        }

        /// <summary>
        /// Builds a small board: one resistor at (0,0) and one voltage source at (5,0),
        /// one net "VIN", traces, and pin connections.
        /// </summary>
        private BoardState BuildFullBoard()
        {
            var board = new BoardState(20, 20);

            // Component 1: resistor at (0,0) with 2 pins
            var resPins = CreatePins(2);
            var resistor = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, resPins);

            // Component 2: vsource at (5,0) with 2 pins
            var vsPins = CreatePins(2);
            var vsource = board.PlaceComponent("vsource_5v", new GridPosition(5, 0), 0, vsPins);

            // Net
            var net = board.CreateNet("VIN");

            // Trace on the net
            board.AddTrace(net.NetId, new GridPosition(1, 0), new GridPosition(5, 0));

            // Pin connections
            board.ConnectPinToNet(net.NetId,
                new PinReference(resistor.InstanceId, 1, resistor.GetPinWorldPosition(1)));
            board.ConnectPinToNet(net.NetId,
                new PinReference(vsource.InstanceId, 0, vsource.GetPinWorldPosition(0)));

            return board;
        }

        // ── Serialize: guard clauses ─────────────────────────────────────────────

        [Test]
        public void Serialize_NullBoardState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Serialize(null, "stage-1"));
        }

        [Test]
        public void Serialize_NullStageId_ThrowsArgumentException()
        {
            var board = new BoardState(10, 10);
            Assert.Throws<ArgumentException>(() => _service.Serialize(board, null));
        }

        [Test]
        public void Serialize_EmptyStageId_ThrowsArgumentException()
        {
            var board = new BoardState(10, 10);
            Assert.Throws<ArgumentException>(() => _service.Serialize(board, "   "));
        }

        // ── Deserialize: guard clauses ────────────────────────────────────────────

        [Test]
        public void Deserialize_NullJson_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.Deserialize(null));
        }

        [Test]
        public void Deserialize_EmptyJson_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.Deserialize(""));
        }

        [Test]
        public void Deserialize_WhitespaceJson_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.Deserialize("   "));
        }

        // ── Serialize: empty board ────────────────────────────────────────────────

        [Test]
        public void Serialize_EmptyBoard_ProducesValidJsonWithMetadata()
        {
            var board = new BoardState(15, 12);
            var json = _service.Serialize(board, "stage-empty");

            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);

            var data = _service.Deserialize(json);
            Assert.AreEqual("stage-empty", data.stageId);
            Assert.AreEqual(15, data.boardWidth);
            Assert.AreEqual(12, data.boardHeight);
            Assert.IsNotNull(data.components);
            Assert.AreEqual(0, data.components.Count);
            Assert.IsNotNull(data.nets);
            Assert.AreEqual(0, data.nets.Count);
            Assert.IsNotNull(data.traces);
            Assert.AreEqual(0, data.traces.Count);
            Assert.IsNotNull(data.pinConnections);
            Assert.AreEqual(0, data.pinConnections.Count);
        }

        // ── Serialize: components ─────────────────────────────────────────────────

        [Test]
        public void Serialize_BoardWithComponent_ComponentSaveDataHasCorrectProperties()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(3, 4), 90, pins, isFixed: false);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(1, data.components.Count);
            var compData = data.components[0];
            Assert.AreEqual(comp.InstanceId, compData.instanceId);
            Assert.AreEqual("resistor_1k", compData.componentDefId);
            Assert.AreEqual(3, compData.positionX);
            Assert.AreEqual(4, compData.positionY);
            Assert.AreEqual(90, compData.rotation);
            Assert.IsFalse(compData.isFixed);
            Assert.IsFalse(compData.hasCustomValue);
        }

        [Test]
        public void Serialize_FixedComponent_IsFixedFlagPreserved()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(1);
            board.PlaceComponent("ground", new GridPosition(0, 0), 0, pins, isFixed: true);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(1, data.components.Count);
            Assert.IsTrue(data.components[0].isFixed);
        }

        [Test]
        public void Serialize_ComponentPins_PinDataMatchesOriginal()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, pins);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            var compData = data.components[0];
            Assert.AreEqual(2, compData.pins.Count);

            Assert.AreEqual(0, compData.pins[0].pinIndex);
            Assert.AreEqual("pin0", compData.pins[0].pinName);
            Assert.AreEqual(0, compData.pins[0].localPositionX);
            Assert.AreEqual(0, compData.pins[0].localPositionY);

            Assert.AreEqual(1, compData.pins[1].pinIndex);
            Assert.AreEqual("pin1", compData.pins[1].pinName);
            Assert.AreEqual(1, compData.pins[1].localPositionX);
            Assert.AreEqual(0, compData.pins[1].localPositionY);
        }

        // ── Serialize: custom value ───────────────────────────────────────────────

        [Test]
        public void Serialize_ComponentWithCustomValue_HasCustomValueTrueAndValueSet()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            board.PlaceComponent("resistor_custom", new GridPosition(0, 0), 0, pins, customValue: 4700f);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            var compData = data.components[0];
            Assert.IsTrue(compData.hasCustomValue);
            Assert.AreEqual(4700f, compData.customValue, 0.001f);
        }

        [Test]
        public void Serialize_ComponentWithoutCustomValue_HasCustomValueFalse()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, pins);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.IsFalse(data.components[0].hasCustomValue);
        }

        // ── Serialize: nets + pin connections ────────────────────────────────────

        [Test]
        public void Serialize_BoardWithNetAndPinConnections_NetAndConnectionDataPopulated()
        {
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, pins);

            var net = board.CreateNet("VIN");
            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(1, data.nets.Count);
            Assert.AreEqual(net.NetId, data.nets[0].netId);
            Assert.AreEqual("VIN", data.nets[0].netName);

            Assert.AreEqual(2, data.pinConnections.Count);

            // Both connections reference the correct component and net
            foreach (var conn in data.pinConnections)
            {
                Assert.AreEqual(comp.InstanceId, conn.componentInstanceId);
                Assert.AreEqual(net.NetId, conn.netId);
            }
        }

        // ── Serialize: traces ─────────────────────────────────────────────────────

        [Test]
        public void Serialize_BoardWithTraces_TraceSaveDataHasCorrectNetIdAndPositions()
        {
            var board = new BoardState(20, 20);
            var net = board.CreateNet("NET1");
            board.AddTrace(net.NetId, new GridPosition(1, 2), new GridPosition(5, 2));

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(1, data.traces.Count);
            var traceData = data.traces[0];
            Assert.AreEqual(net.NetId, traceData.netId);
            Assert.AreEqual(1, traceData.startX);
            Assert.AreEqual(2, traceData.startY);
            Assert.AreEqual(5, traceData.endX);
            Assert.AreEqual(2, traceData.endY);
        }

        // ── RestoreToBoard: guard clauses ─────────────────────────────────────────

        [Test]
        public void RestoreToBoard_NullBoardState_ThrowsArgumentNullException()
        {
            var data = new BoardSaveData { stageId = "s1", boardWidth = 10, boardHeight = 10 };
            Assert.Throws<ArgumentNullException>(() => _service.RestoreToBoard(null, data));
        }

        [Test]
        public void RestoreToBoard_NullData_ThrowsArgumentNullException()
        {
            var board = new BoardState(10, 10);
            Assert.Throws<ArgumentNullException>(() => _service.RestoreToBoard(board, null));
        }

        // ── Round-trip: simple board ──────────────────────────────────────────────

        [Test]
        public void RoundTrip_EmptyBoard_RestoredBoardIsAlsoEmpty()
        {
            var original = new BoardState(10, 10);

            var json = _service.Serialize(original, "stage-rt");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            Assert.AreEqual(0, restored.Components.Count);
            Assert.AreEqual(0, restored.Nets.Count);
            Assert.AreEqual(0, restored.Traces.Count);
        }

        [Test]
        public void RoundTrip_SingleComponent_ComponentPropertiesMatchAfterRestore()
        {
            var original = new BoardState(10, 10);
            var pins = CreatePins(2);
            original.PlaceComponent("resistor_1k", new GridPosition(3, 4), 90, pins, customValue: 2200f);

            var json = _service.Serialize(original, "stage-rt");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            Assert.AreEqual(1, restored.Components.Count);
            var comp = restored.Components[0];
            Assert.AreEqual("resistor_1k", comp.ComponentDefinitionId);
            Assert.AreEqual(3, comp.Position.X);
            Assert.AreEqual(4, comp.Position.Y);
            Assert.AreEqual(90, comp.Rotation);
            Assert.IsTrue(comp.CustomValue.HasValue);
            Assert.AreEqual(2200f, comp.CustomValue.Value, 0.001f);
            Assert.AreEqual(2, comp.Pins.Count);
        }

        [Test]
        public void RoundTrip_FixedComponent_IsFixedPreservedAfterRestore()
        {
            var original = new BoardState(10, 10);
            var pins = CreatePins(1);
            original.PlaceComponent("ground", new GridPosition(0, 0), 0, pins, isFixed: true);

            var json = _service.Serialize(original, "stage-rt");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            Assert.AreEqual(1, restored.Components.Count);
            Assert.IsTrue(restored.Components[0].IsFixed);
        }

        [Test]
        public void RoundTrip_ComponentsNetsTracesAndConnections_StateMatchesOriginal()
        {
            var original = BuildFullBoard();

            var json = _service.Serialize(original, "stage-full");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(20, 20);
            _service.RestoreToBoard(restored, saveData);

            // Same counts
            Assert.AreEqual(original.Components.Count, restored.Components.Count);
            Assert.AreEqual(original.Nets.Count, restored.Nets.Count);
            Assert.AreEqual(original.Traces.Count, restored.Traces.Count);
        }

        [Test]
        public void RoundTrip_NetName_PreservedAfterRestore()
        {
            var original = new BoardState(10, 10);
            original.CreateNet("GND");
            original.CreateNet("VIN");

            var json = _service.Serialize(original, "stage-1");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            Assert.AreEqual(2, restored.Nets.Count);

            var gnd = restored.GetNetByName("GND");
            var vin = restored.GetNetByName("VIN");
            Assert.IsNotNull(gnd, "GND net should be restored");
            Assert.IsNotNull(vin, "VIN net should be restored");
        }

        [Test]
        public void RoundTrip_TracePositions_PreservedAfterRestore()
        {
            var original = new BoardState(20, 20);
            var net = original.CreateNet("NET1");
            original.AddTrace(net.NetId, new GridPosition(0, 5), new GridPosition(10, 5));

            var json = _service.Serialize(original, "stage-1");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(20, 20);
            _service.RestoreToBoard(restored, saveData);

            Assert.AreEqual(1, restored.Traces.Count);
            var trace = restored.Traces[0];
            Assert.AreEqual(0, trace.Start.X);
            Assert.AreEqual(5, trace.Start.Y);
            Assert.AreEqual(10, trace.End.X);
            Assert.AreEqual(5, trace.End.Y);
        }

        // ── RestoreToBoard: ID remapping ──────────────────────────────────────────

        [Test]
        public void RestoreToBoard_IdRemapping_PinConnectionsReferenceNewIds()
        {
            // Build original board with component + net + connection
            var original = new BoardState(10, 10);
            var pins = CreatePins(2);
            var comp = original.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, pins);
            var net = original.CreateNet("VIN");
            board_ConnectPin(original, comp, 0, net.NetId);
            board_ConnectPin(original, comp, 1, net.NetId);

            // Verify original IDs (auto-increment from 1)
            Assert.AreEqual(1, comp.InstanceId);
            Assert.AreEqual(1, net.NetId);

            // Serialize and restore into a fresh board
            var json = _service.Serialize(original, "stage-remap");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            // Restored component and net should also have valid IDs (likely 1 again)
            Assert.AreEqual(1, restored.Components.Count);
            Assert.AreEqual(1, restored.Nets.Count);

            var restoredComp = restored.Components[0];
            var restoredNet = restored.Nets[0];

            // Pin connections should reference the RESTORED (new) component and net IDs
            Assert.AreEqual(2, restoredNet.ConnectedPins.Count,
                "Both pins should be connected to the restored net");

            foreach (var pinRef in restoredNet.ConnectedPins)
            {
                Assert.AreEqual(restoredComp.InstanceId, pinRef.ComponentInstanceId,
                    "PinReference should use restored component ID, not original saved ID");
            }
        }

        // Helper to avoid tuple usage when connecting pins
        private static void board_ConnectPin(BoardState board, PlacedComponent comp, int pinIndex, int netId)
        {
            board.ConnectPinToNet(netId,
                new PinReference(comp.InstanceId, pinIndex, comp.GetPinWorldPosition(pinIndex)));
        }

        // ── ConvertToSaveData: custom value captured ──────────────────────────────

        [Test]
        public void Serialize_CustomValueZero_HasCustomValueTrueAndValueIsZero()
        {
            // Explicitly passing 0f as custom value is a valid override
            var board = new BoardState(10, 10);
            var pins = CreatePins(2);
            board.PlaceComponent("capacitor", new GridPosition(0, 0), 0, pins, customValue: 0f);

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.IsTrue(data.components[0].hasCustomValue);
            Assert.AreEqual(0f, data.components[0].customValue, 0.001f);
        }

        [Test]
        public void Serialize_MultipleComponents_AllCapturedInOrder()
        {
            var board = new BoardState(20, 20);
            board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2));
            board.PlaceComponent("vsource_5v", new GridPosition(5, 0), 0, CreatePins(2));
            board.PlaceComponent("ground", new GridPosition(10, 0), 0, CreatePins(1));

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(3, data.components.Count);
            Assert.AreEqual("resistor_1k", data.components[0].componentDefId);
            Assert.AreEqual("vsource_5v", data.components[1].componentDefId);
            Assert.AreEqual("ground", data.components[2].componentDefId);
        }

        // ── File I/O: SaveToFile / LoadFromFile ───────────────────────────────────

        [Test]
        public void SaveToFile_NullPath_ThrowsArgumentException()
        {
            var board = new BoardState(10, 10);
            Assert.Throws<ArgumentException>(() => _service.SaveToFile(null, board, "stage-1"));
        }

        [Test]
        public void SaveToFile_EmptyPath_ThrowsArgumentException()
        {
            var board = new BoardState(10, 10);
            Assert.Throws<ArgumentException>(() => _service.SaveToFile("", board, "stage-1"));
        }

        [Test]
        public void LoadFromFile_NonExistentPath_ThrowsFileNotFoundException()
        {
            var fakePath = Path.Combine(_tempDirectory, "does_not_exist.json");
            Assert.Throws<FileNotFoundException>(() => _service.LoadFromFile(fakePath));
        }

        [Test]
        public void LoadFromFile_EmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.LoadFromFile(""));
        }

        [Test]
        public void SaveToFile_ValidPath_CreatesFileWithJsonContent()
        {
            var board = new BoardState(10, 10);
            board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2));

            var filePath = Path.Combine(_tempDirectory, "save.json");
            _service.SaveToFile(filePath, board, "stage-file");

            Assert.IsTrue(File.Exists(filePath), "Save file should exist on disk");
            var rawContent = File.ReadAllText(filePath);
            Assert.IsNotEmpty(rawContent);
            // Sanity: the JSON should at least contain the stageId
            StringAssert.Contains("stage-file", rawContent);
        }

        [Test]
        public void SaveToFile_DirectoryDoesNotExist_CreatesDirectoryAndFile()
        {
            var board = new BoardState(10, 10);
            var nestedDir = Path.Combine(_tempDirectory, "sub", "nested");
            var filePath = Path.Combine(nestedDir, "save.json");

            _service.SaveToFile(filePath, board, "stage-nested");

            Assert.IsTrue(File.Exists(filePath), "File should be created even with nested non-existent directories");
        }

        [Test]
        public void FileRoundTrip_SaveThenLoad_DataMatchesOriginal()
        {
            var original = new BoardState(10, 10);
            original.PlaceComponent("resistor_1k", new GridPosition(2, 3), 180, CreatePins(2), customValue: 1000f);
            var net = original.CreateNet("VCC");
            original.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(2, 0));

            var filePath = Path.Combine(_tempDirectory, "test_save.json");
            _service.SaveToFile(filePath, original, "stage-io");

            var loadedData = _service.LoadFromFile(filePath);

            Assert.AreEqual("stage-io", loadedData.stageId);
            Assert.AreEqual(10, loadedData.boardWidth);
            Assert.AreEqual(10, loadedData.boardHeight);
            Assert.AreEqual(1, loadedData.components.Count);
            Assert.AreEqual("resistor_1k", loadedData.components[0].componentDefId);
            Assert.AreEqual(2, loadedData.components[0].positionX);
            Assert.AreEqual(3, loadedData.components[0].positionY);
            Assert.AreEqual(180, loadedData.components[0].rotation);
            Assert.IsTrue(loadedData.components[0].hasCustomValue);
            Assert.AreEqual(1000f, loadedData.components[0].customValue, 0.001f);
            Assert.AreEqual(1, loadedData.nets.Count);
            Assert.AreEqual("VCC", loadedData.nets[0].netName);
            Assert.AreEqual(1, loadedData.traces.Count);
        }

        [Test]
        public void FileRoundTrip_SaveLoadRestoreToBoard_StateMatchesOriginal()
        {
            var original = BuildFullBoard();
            var filePath = Path.Combine(_tempDirectory, "full_round_trip.json");

            _service.SaveToFile(filePath, original, "stage-full-rt");
            var loadedData = _service.LoadFromFile(filePath);
            var restored = new BoardState(20, 20);
            _service.RestoreToBoard(restored, loadedData);

            Assert.AreEqual(original.Components.Count, restored.Components.Count);
            Assert.AreEqual(original.Nets.Count, restored.Nets.Count);
            Assert.AreEqual(original.Traces.Count, restored.Traces.Count);
        }

        // ── Multiple traces on one net ────────────────────────────────────────────

        [Test]
        public void Serialize_MultipleTracesOnOneNet_AllTracesSerializedWithCorrectNetId()
        {
            var board = new BoardState(20, 20);
            var net = board.CreateNet("NET_MULTI");
            board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            board.AddTrace(net.NetId, new GridPosition(5, 0), new GridPosition(5, 5));

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(2, data.traces.Count);
            foreach (var trace in data.traces)
                Assert.AreEqual(net.NetId, trace.netId);
        }

        // ── Multiple nets ─────────────────────────────────────────────────────────

        [Test]
        public void Serialize_MultipleNets_AllNetsSerializedWithDistinctIds()
        {
            var board = new BoardState(20, 20);
            var net1 = board.CreateNet("VIN");
            var net2 = board.CreateNet("GND");
            board.AddTrace(net1.NetId, new GridPosition(0, 0), new GridPosition(3, 0));
            board.AddTrace(net2.NetId, new GridPosition(5, 0), new GridPosition(8, 0));

            var json = _service.Serialize(board, "stage-1");
            var data = _service.Deserialize(json);

            Assert.AreEqual(2, data.nets.Count);
            Assert.AreNotEqual(data.nets[0].netId, data.nets[1].netId);
        }

        // ── Round-trip: pin connection world position remapped from restored component ──

        [Test]
        public void RoundTrip_PinWorldPositionRecalculatedFromRestoredComponent()
        {
            // Pin world position must come from the restored component's GetPinWorldPosition,
            // not from the serialized pinWorldX/Y in PinConnectionSaveData.
            var original = new BoardState(10, 10);
            var pins = CreatePins(2);
            var comp = original.PlaceComponent("resistor_1k", new GridPosition(2, 3), 0, pins);
            var net = original.CreateNet("VOUT");
            board_ConnectPin(original, comp, 0, net.NetId);

            var json = _service.Serialize(original, "stage-1");
            var saveData = _service.Deserialize(json);
            var restored = new BoardState(10, 10);
            _service.RestoreToBoard(restored, saveData);

            // Verify that the pin is actually connected in the restored board
            var restoredComp = restored.Components[0];
            var restoredNet = restored.Nets[0];

            Assert.AreEqual(1, restoredNet.ConnectedPins.Count);
            Assert.AreEqual(restoredComp.InstanceId, restoredNet.ConnectedPins[0].ComponentInstanceId);
            Assert.AreEqual(0, restoredNet.ConnectedPins[0].PinIndex);

            // The stored world position should match what GetPinWorldPosition returns
            var expectedWorldPos = restoredComp.GetPinWorldPosition(0);
            Assert.AreEqual(expectedWorldPos.X, restoredNet.ConnectedPins[0].Position.X);
            Assert.AreEqual(expectedWorldPos.Y, restoredNet.ConnectedPins[0].Position.Y);
        }
    }
}
