using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Cysharp.Threading.Tasks;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Systems;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests proving the complete simulation pipeline:
    /// BoardState -> BoardToNetlistConverter -> SpiceSharpSimulationService
    /// </summary>
    [TestFixture]
    public class BoardStateSimulationTests
    {
        private const string TestAssetBasePath = "Assets/70_Data/10_ScriptableObjects/40_Components";
        private const string VSourceAssetName = "test_vsource_5v";
        private const string Resistor1kAssetName = "test_resistor_1k";
        private const string Resistor2kAssetName = "test_resistor_2k";

        private ISimulationService _simulationService;
        private List<string> _createdAssetPaths;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Ensure the test asset directory exists
            EnsureDirectoryExists(TestAssetBasePath);
            _createdAssetPaths = new List<string>();
            
            // Create test assets
            CreateVoltageSourceAsset(VSourceAssetName, 5f);
            CreateResistorAsset(Resistor1kAssetName, 1000f);
            CreateResistorAsset(Resistor2kAssetName, 2000f);
            AssertAssetKind(VSourceAssetName, ComponentKind.VoltageSource);
            AssertAssetKind(Resistor1kAssetName, ComponentKind.Resistor);
            AssertAssetKind(Resistor2kAssetName, ComponentKind.Resistor);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up test assets
            foreach (var path in _createdAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            AssetDatabase.Refresh();
        }

        [SetUp]
        public void Setup()
        {
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Tests the complete simulation pipeline with a voltage divider circuit.
        /// Circuit: 5V source -> 1k resistor -> VOUT node -> 2k resistor -> GND
        /// Expected: Vout = 5V * 2k / (1k + 2k) = 3.333V
        /// </summary>
        [UnityTest]
        public IEnumerator VoltageDivider_EndToEnd_CalculatesCorrectOutputVoltage()
        {
            // Arrange: Create the board and circuit
            var board = new BoardState(10, 10);
            
            // Create nets: VIN (from voltage source), GND (ground), VOUT (output)
            var netVin = board.CreateNet("VIN");
            var netGnd = board.CreateNet("GND"); // SpiceSharp uses "0" for ground, but we use "GND" as netlist GroundNode
            var netVout = board.CreateNet("VOUT");

            // Place voltage source at (0, 0)
            // V1: positive -> VIN, negative -> GND
            var vsourcePins = CreateVSourcePins();
            var vsource = board.PlaceComponent(VSourceAssetName, new GridPosition(0, 0), 0, vsourcePins);
            
            // Connect voltage source pins to nets
            // Pin 0 (positive) -> VIN
            board.ConnectPinToNet(netVin.NetId, new PinReference(vsource.InstanceId, 0, vsource.GetPinWorldPosition(0)));
            // Pin 1 (negative) -> GND
            board.ConnectPinToNet(netGnd.NetId, new PinReference(vsource.InstanceId, 1, vsource.GetPinWorldPosition(1)));

            // Place R1 (1k) at (2, 0) - between VIN and VOUT
            var r1Pins = CreateResistorPins();
            var r1 = board.PlaceComponent(Resistor1kAssetName, new GridPosition(2, 0), 0, r1Pins);
            
            // Connect R1: terminal1 -> VIN, terminal2 -> VOUT
            board.ConnectPinToNet(netVin.NetId, new PinReference(r1.InstanceId, 0, r1.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netVout.NetId, new PinReference(r1.InstanceId, 1, r1.GetPinWorldPosition(1)));

            // Place R2 (2k) at (4, 0) - between VOUT and GND
            var r2Pins = CreateResistorPins();
            var r2 = board.PlaceComponent(Resistor2kAssetName, new GridPosition(4, 0), 0, r2Pins);
            
            // Connect R2: terminal1 -> VOUT, terminal2 -> GND
            board.ConnectPinToNet(netVout.NetId, new PinReference(r2.InstanceId, 0, r2.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netGnd.NetId, new PinReference(r2.InstanceId, 1, r2.GetPinWorldPosition(1)));

            // Create component provider that loads our test assets
            var componentProvider = new TestComponentProvider();

            // Convert board to netlist
            var converter = new BoardToNetlistConverter(componentProvider);
            var probes = new[] { ProbeDefinition.Voltage("V_out", "VOUT") };
            var netlist = converter.Convert(board, probes);

            // Create simulation request
            var request = SimulationRequest.DCOperatingPoint(netlist);
            request.IsSafetyChecksEnabled = true;

            // Act: Run simulation
            SimulationResult result = null;
            Exception simulationException = null;
            yield return _simulationService.RunAsync(request).ToCoroutine(
                value => result = value,
                ex => simulationException = ex);

            if (simulationException != null)
            {
                Assert.Fail($"Simulation task faulted: {simulationException}");
            }

            Assert.IsNotNull(result, "Simulation returned null result.");

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");
            
            var vOut = result.GetVoltage("VOUT");
            Assert.IsNotNull(vOut, "VOUT voltage not found in results");

            // Expected: Vout = 5V * 2k / (1k + 2k) = 3.3333...V
            double expectedVout = 5.0 * 2000.0 / (1000.0 + 2000.0);
            Assert.AreEqual(expectedVout, vOut.Value, 0.01, 
                $"VOUT = {vOut.Value}V (expected {expectedVout}V +/- 0.01V)");
        }

        /// <summary>
        /// Tests that the board state correctly tracks component and net counts.
        /// </summary>
        [Test]
        public void BoardState_TracksProperly_ComponentsAndNets()
        {
            // Arrange & Act
            var board = new BoardState(10, 10);
            
            var net1 = board.CreateNet("NET1");
            var net2 = board.CreateNet("NET2");
            
            var pins = CreateResistorPins();
            var comp = board.PlaceComponent(Resistor1kAssetName, new GridPosition(0, 0), 0, pins);

            // Assert
            Assert.AreEqual(2, board.Nets.Count, "Should have 2 nets");
            Assert.AreEqual(1, board.Components.Count, "Should have 1 component");
            Assert.AreEqual(Resistor1kAssetName, comp.ComponentDefinitionId);
        }

        /// <summary>
        /// Tests that pin connections are properly tracked through the pipeline.
        /// </summary>
        [Test]
        public void BoardToNetlistConverter_ProducesValidNetlist_WithCorrectNodes()
        {
            // Arrange
            var board = new BoardState(10, 10);
            
            var netVin = board.CreateNet("VIN");
            var netGnd = board.CreateNet("GND");
            
            var vsourcePins = CreateVSourcePins();
            var vsource = board.PlaceComponent(VSourceAssetName, new GridPosition(0, 0), 0, vsourcePins);
            
            board.ConnectPinToNet(netVin.NetId, new PinReference(vsource.InstanceId, 0, vsource.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netGnd.NetId, new PinReference(vsource.InstanceId, 1, vsource.GetPinWorldPosition(1)));

            var componentProvider = new TestComponentProvider();
            var converter = new BoardToNetlistConverter(componentProvider);

            // Act
            var netlist = converter.Convert(board);

            // Assert
            Assert.IsNotNull(netlist);
            Assert.AreEqual(1, netlist.Elements.Count, "Should have 1 element (voltage source)");
            
            var vsElement = netlist.Elements[0];
            Assert.AreEqual(ElementType.VoltageSource, vsElement.Type);
            Assert.IsTrue(vsElement.Nodes.Contains("VIN"), "Nodes should contain VIN");
            Assert.IsTrue(vsElement.Nodes.Contains("GND"), "Nodes should contain GND");
        }

        #region Helper Methods

        private PinInstance[] CreateVSourcePins()
        {
            return new[]
            {
                new PinInstance(0, "positive", new GridPosition(0, 0)),
                new PinInstance(1, "negative", new GridPosition(0, 1))
            };
        }

        private PinInstance[] CreateResistorPins()
        {
            return new[]
            {
                new PinInstance(0, "terminal1", new GridPosition(0, 0)),
                new PinInstance(1, "terminal2", new GridPosition(1, 0))
            };
        }

        private void EnsureDirectoryExists(string path)
        {
            var folders = path.Split('/');
            var currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                var nextFolder = folders[i];
                var fullPath = currentPath + "/" + nextFolder;
                
                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    AssetDatabase.CreateFolder(currentPath, nextFolder);
                }
                currentPath = fullPath;
            }
        }

        private void CreateVoltageSourceAsset(string assetName, float voltage)
        {
            var path = $"{TestAssetBasePath}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ComponentDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SetPrivateField(asset, "_id", assetName);
            SetPrivateField(asset, "_displayName", $"Test Voltage Source {voltage}V");
            SetPrivateField(asset, "_kind", ComponentKind.VoltageSource);
            SetPrivateField(asset, "_voltageVolts", voltage);
            
            EditorUtility.SetDirty(asset);
            if (!_createdAssetPaths.Contains(path))
            {
                _createdAssetPaths.Add(path);
            }
        }

        private void CreateResistorAsset(string assetName, float resistance)
        {
            var path = $"{TestAssetBasePath}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ComponentDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SetPrivateField(asset, "_id", assetName);
            SetPrivateField(asset, "_displayName", $"Test Resistor {resistance}Ohm");
            SetPrivateField(asset, "_kind", ComponentKind.Resistor);
            SetPrivateField(asset, "_resistanceOhms", resistance);
            
            EditorUtility.SetDirty(asset);
            if (!_createdAssetPaths.Contains(path))
            {
                _createdAssetPaths.Add(path);
            }
        }

        private void AssertAssetKind(string assetName, ComponentKind expectedKind)
        {
            var path = $"{TestAssetBasePath}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
            Assert.IsNotNull(asset, $"Test asset not found: {path}");
            Assert.AreEqual(expectedKind, asset.Kind, $"Unexpected kind for {assetName}");
        }

        private static void SetPrivateField<TValue>(ComponentDefinition asset, string fieldName, TValue value)
        {
            var field = typeof(ComponentDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found on ComponentDefinition.");
            field.SetValue(asset, value);
        }

        #endregion

        #region Test Component Provider

        /// <summary>
        /// Component provider that loads ComponentDefinition assets from the test asset path.
        /// </summary>
        private class TestComponentProvider : IComponentDefinitionProvider
        {
            public ComponentDefinition GetDefinition(string componentDefId)
            {
                var path = $"{TestAssetBasePath}/{componentDefId}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
                
                if (asset == null)
                {
                    Debug.LogError($"Failed to load ComponentDefinition at path: {path}");
                }
                
                return asset;
            }
        }

        #endregion
    }
}
