using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using CircuitCraft.Systems;

namespace CircuitCraft.Tests.Systems
{
    /// <summary>
    /// Characterization tests for BoardToNetlistConverter — captures all current behavior
    /// as a safety net before refactoring. Tests must pass against the current implementation.
    /// </summary>
    [TestFixture]
    public class BoardToNetlistConverterTests
    {
        private BoardToNetlistConverter _converter;
        private TestComponentDefinitionProvider _provider;
        private List<ComponentDefinition> _createdDefinitions;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _createdDefinitions = new List<ComponentDefinition>();
            _provider = new TestComponentDefinitionProvider();
            _converter = new BoardToNetlistConverter(_provider);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var def in _createdDefinitions)
            {
                if (def != null)
                    UnityEngine.Object.DestroyImmediate(def);
            }
            _createdDefinitions.Clear();
        }

        // ── Stub Provider ────────────────────────────────────────────────────────

        private class TestComponentDefinitionProvider : IComponentDefinitionProvider
        {
            private readonly Dictionary<string, ComponentDefinition> _definitions
                = new Dictionary<string, ComponentDefinition>();

            public void Register(ComponentDefinition def) => _definitions[def.Id] = def;

            public ComponentDefinition GetDefinition(string componentDefId)
            {
                _definitions.TryGetValue(componentDefId, out var def);
                return def;
            }
        }

        // ── Helper: pin creation ─────────────────────────────────────────────────

        private static List<PinInstance> CreatePins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            return pins;
        }

        // ── Helper: ComponentDefinition factory via reflection ────────────────────

        private ComponentDefinition CreateDefinition(string id, ComponentKind kind, Action<ComponentDefinition> configure = null)
        {
            var def = ScriptableObject.CreateInstance<ComponentDefinition>();
            _createdDefinitions.Add(def);

            SetPrivateField(def, "_id", id);
            SetPrivateField(def, "_displayName", id);
            SetPrivateField(def, "_kind", kind);

            configure?.Invoke(def);

            _provider.Register(def);
            return def;
        }

        private ComponentDefinition CreateResistorDefinition(string id, float ohms)
        {
            return CreateDefinition(id, ComponentKind.Resistor, def =>
                SetPrivateField(def, "_resistanceOhms", ohms));
        }

        private ComponentDefinition CreateVoltageSourceDefinition(string id, float volts)
        {
            return CreateDefinition(id, ComponentKind.VoltageSource, def =>
                SetPrivateField(def, "_voltageVolts", volts));
        }

        private ComponentDefinition CreateCapacitorDefinition(string id, float farads)
        {
            return CreateDefinition(id, ComponentKind.Capacitor, def =>
                SetPrivateField(def, "_capacitanceFarads", farads));
        }

        private ComponentDefinition CreateInductorDefinition(string id, float henrys)
        {
            return CreateDefinition(id, ComponentKind.Inductor, def =>
                SetPrivateField(def, "_inductanceHenrys", henrys));
        }

        private ComponentDefinition CreateCurrentSourceDefinition(string id, float amps)
        {
            return CreateDefinition(id, ComponentKind.CurrentSource, def =>
                SetPrivateField(def, "_currentAmps", amps));
        }

        private ComponentDefinition CreateDiodeDefinition(string id, ComponentKind kind = ComponentKind.Diode)
        {
            return CreateDefinition(id, kind, def =>
            {
                SetPrivateField(def, "_diodeModel", DiodeModel._1N4148);
                SetPrivateField(def, "_saturationCurrent", 1e-9f);
                SetPrivateField(def, "_emissionCoefficient", 1.8f);
                SetPrivateField(def, "_breakdownVoltage", 0f);
                SetPrivateField(def, "_breakdownCurrent", 0f);
            });
        }

        private ComponentDefinition CreateZenerDefinition(string id)
        {
            return CreateDefinition(id, ComponentKind.ZenerDiode, def =>
            {
                SetPrivateField(def, "_diodeModel", DiodeModel.Zener_5V1);
                SetPrivateField(def, "_saturationCurrent", 1e-9f);
                SetPrivateField(def, "_emissionCoefficient", 1.8f);
                SetPrivateField(def, "_breakdownVoltage", 5.1f);
                SetPrivateField(def, "_breakdownCurrent", 0.001f);
            });
        }

        private ComponentDefinition CreateBJTDefinition(string id, BJTPolarity polarity = BJTPolarity.NPN)
        {
            return CreateDefinition(id, ComponentKind.BJT, def =>
            {
                SetPrivateField(def, "_bjtPolarity", polarity);
                SetPrivateField(def, "_bjtModel", BJTModel._2N2222);
                SetPrivateField(def, "_beta", 200f);
                SetPrivateField(def, "_earlyVoltage", 100f);
            });
        }

        private ComponentDefinition CreateMOSFETDefinition(string id, FETPolarity polarity = FETPolarity.NChannel)
        {
            return CreateDefinition(id, ComponentKind.MOSFET, def =>
            {
                SetPrivateField(def, "_fetPolarity", polarity);
                SetPrivateField(def, "_mosfetModel", MOSFETModel._2N7000);
                SetPrivateField(def, "_thresholdVoltage", 2.0f);
                SetPrivateField(def, "_transconductance", 0.3f);
            });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            // Walk up the hierarchy to find the field (ScriptableObject can inherit)
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field == null)
                throw new MissingFieldException($"Field '{fieldName}' not found on {target.GetType().Name}");

            field.SetValue(target, value);
        }

        // ── Helper: build a 2-pin component connected to two nets ────────────────

        private PlacedComponent PlaceConnected2Pin(BoardState board, string defId, int x, int y,
            int netIdA, int netIdB)
        {
            var comp = board.PlaceComponent(defId, new GridPosition(x, y), 0, CreatePins(2));
            board.ConnectPinToNet(netIdA, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netIdB, new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));
            return comp;
        }

        private PlacedComponent PlaceConnected3Pin(BoardState board, string defId, int x, int y,
            int netId0, int netId1, int netId2)
        {
            var comp = board.PlaceComponent(defId, new GridPosition(x, y), 0, CreatePins(3));
            board.ConnectPinToNet(netId0, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netId1, new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));
            board.ConnectPinToNet(netId2, new PinReference(comp.InstanceId, 2, comp.GetPinWorldPosition(2)));
            return comp;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR TESTS
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Constructor_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BoardToNetlistConverter(null));
        }

        [Test]
        public void Constructor_ValidProvider_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new BoardToNetlistConverter(_provider));
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONVERT - ARGUMENT GUARD TESTS
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_NullBoardState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _converter.Convert(null));
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONVERT - EMPTY BOARD
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_EmptyBoard_ReturnsNetlistWithCorrectTitle()
        {
            var board = new BoardState(10, 10);

            var netlist = _converter.Convert(board);

            Assert.AreEqual("BoardState Circuit", netlist.Title);
        }

        [Test]
        public void Convert_EmptyBoard_ReturnsNetlistWithGroundNodeZero()
        {
            var board = new BoardState(10, 10);

            var netlist = _converter.Convert(board);

            Assert.AreEqual("0", netlist.GroundNode);
        }

        [Test]
        public void Convert_EmptyBoard_ReturnsNetlistWithZeroElements()
        {
            var board = new BoardState(10, 10);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(0, netlist.Elements.Count);
        }

        [Test]
        public void Convert_EmptyBoard_ReturnsNetlistWithZeroProbes()
        {
            var board = new BoardState(10, 10);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(0, netlist.Probes.Count);
        }

        // ════════════════════════════════════════════════════════════════════════
        // RESISTOR CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleResistor_ElementIdUsesRPrefix()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            var comp = PlaceConnected2Pin(board, "resistor_1k", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(1, netlist.Elements.Count);
            Assert.AreEqual($"R{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleResistor_ElementTypeIsResistor()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "resistor_1k", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Resistor, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleResistor_NodesMatchNetNames()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "resistor_1k", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.Contains("NET_A", elem.Nodes);
            Assert.Contains("NET_B", elem.Nodes);
        }

        [Test]
        public void Convert_SingleResistor_ValueMatchesDefinitionResistance()
        {
            CreateResistorDefinition("resistor_2k", 2000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "resistor_2k", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(2000.0, netlist.Elements[0].Value, 1e-9);
        }

        // ════════════════════════════════════════════════════════════════════════
        // VOLTAGE SOURCE CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleVoltageSource_ElementIdUsesVPrefix()
        {
            CreateVoltageSourceDefinition("vsource_5v", 5f);
            var board = new BoardState(10, 10);
            var netPos = board.CreateNet("VIN");
            var netNeg = board.CreateNet("GND");
            var comp = PlaceConnected2Pin(board, "vsource_5v", 0, 0, netPos.NetId, netNeg.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"V{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleVoltageSource_ElementTypeIsVoltageSource()
        {
            CreateVoltageSourceDefinition("vsource_5v", 5f);
            var board = new BoardState(10, 10);
            var netPos = board.CreateNet("VIN");
            var netNeg = board.CreateNet("GND");
            PlaceConnected2Pin(board, "vsource_5v", 0, 0, netPos.NetId, netNeg.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.VoltageSource, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleVoltageSource_ValueMatchesDefinitionVoltage()
        {
            CreateVoltageSourceDefinition("vsource_9v", 9f);
            var board = new BoardState(10, 10);
            var netPos = board.CreateNet("VIN");
            var netNeg = board.CreateNet("GND");
            PlaceConnected2Pin(board, "vsource_9v", 0, 0, netPos.NetId, netNeg.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(9.0, netlist.Elements[0].Value, 1e-9);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CAPACITOR CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleCapacitor_ElementIdUsesCPrefix()
        {
            CreateCapacitorDefinition("cap_100nf", 100e-9f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            var comp = PlaceConnected2Pin(board, "cap_100nf", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"C{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleCapacitor_ElementTypeIsCapacitor()
        {
            CreateCapacitorDefinition("cap_100nf", 100e-9f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "cap_100nf", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Capacitor, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleCapacitor_ValueMatchesDefinitionCapacitance()
        {
            CreateCapacitorDefinition("cap_100nf", 100e-9f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "cap_100nf", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(100e-9, netlist.Elements[0].Value, 1e-18);
        }

        // ════════════════════════════════════════════════════════════════════════
        // INDUCTOR CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleInductor_ElementIdUsesLPrefix()
        {
            CreateInductorDefinition("inductor_1mh", 1e-3f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            var comp = PlaceConnected2Pin(board, "inductor_1mh", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"L{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleInductor_ElementTypeIsInductor()
        {
            CreateInductorDefinition("inductor_1mh", 1e-3f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "inductor_1mh", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Inductor, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleInductor_ValueMatchesDefinitionInductance()
        {
            CreateInductorDefinition("inductor_1mh", 1e-3f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "inductor_1mh", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(1e-3, netlist.Elements[0].Value, 1e-12);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CURRENT SOURCE CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleCurrentSource_ElementIdUsesIPrefix()
        {
            CreateCurrentSourceDefinition("isource_10ma", 0.01f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            var comp = PlaceConnected2Pin(board, "isource_10ma", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"I{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleCurrentSource_ElementTypeIsCurrentSource()
        {
            CreateCurrentSourceDefinition("isource_10ma", 0.01f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "isource_10ma", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.CurrentSource, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleCurrentSource_ValueMatchesDefinitionCurrent()
        {
            CreateCurrentSourceDefinition("isource_10ma", 0.01f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "isource_10ma", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(0.01, netlist.Elements[0].Value, 1e-9);
        }

        // ════════════════════════════════════════════════════════════════════════
        // DIODE CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleDiode_ElementIdUsesDPrefix()
        {
            CreateDiodeDefinition("diode_1n4148");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            var comp = PlaceConnected2Pin(board, "diode_1n4148", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"D{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleDiode_ElementTypeIsDiode()
        {
            CreateDiodeDefinition("diode_1n4148");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "diode_1n4148", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Diode, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleDiode_ModelNameIsSet()
        {
            CreateDiodeDefinition("diode_1n4148");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "diode_1n4148", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.IsFalse(string.IsNullOrEmpty(netlist.Elements[0].ModelName),
                "Diode should have a model name");
            Assert.AreEqual("1N4148", netlist.Elements[0].ModelName);
        }

        [Test]
        public void Convert_SingleDiode_SaturationCurrentParameterIsSet()
        {
            CreateDiodeDefinition("diode_1n4148");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "diode_1n4148", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("Is"), "Should have Is parameter");
            Assert.AreEqual(1e-9, elem.Parameters["Is"], 1e-18);
        }

        [Test]
        public void Convert_SingleDiode_EmissionCoefficientParameterIsSet()
        {
            CreateDiodeDefinition("diode_1n4148");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "diode_1n4148", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("N"), "Should have N parameter");
            Assert.AreEqual(1.8, elem.Parameters["N"], 1e-6);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LED CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleLED_ElementIdUsesDPrefix()
        {
            CreateDiodeDefinition("led_red", ComponentKind.LED);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            var comp = PlaceConnected2Pin(board, "led_red", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"D{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleLED_ElementTypeIsDiode()
        {
            CreateDiodeDefinition("led_red", ComponentKind.LED);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "led_red", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Diode, netlist.Elements[0].Type);
        }

        // ════════════════════════════════════════════════════════════════════════
        // ZENER DIODE CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleZenerDiode_ElementIdUsesDPrefix()
        {
            CreateZenerDefinition("zener_5v1");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            var comp = PlaceConnected2Pin(board, "zener_5v1", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"D{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleZenerDiode_ElementTypeIsDiode()
        {
            CreateZenerDefinition("zener_5v1");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "zener_5v1", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.Diode, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleZenerDiode_BreakdownVoltageParameterIsSet()
        {
            CreateZenerDefinition("zener_5v1");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "zener_5v1", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("BV"), "Should have BV parameter");
            Assert.AreEqual(5.1, elem.Parameters["BV"], 1e-4);
        }

        [Test]
        public void Convert_SingleZenerDiode_BreakdownCurrentParameterIsSet()
        {
            CreateZenerDefinition("zener_5v1");
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("ANODE");
            var netK = board.CreateNet("CATHODE");
            PlaceConnected2Pin(board, "zener_5v1", 0, 0, netA.NetId, netK.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("IBV"), "Should have IBV parameter");
            Assert.AreEqual(0.001, elem.Parameters["IBV"], 1e-9);
        }

        // ════════════════════════════════════════════════════════════════════════
        // BJT CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleNPNBJT_ElementIdUsesQPrefix()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            var comp = PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"Q{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleNPNBJT_ElementTypeIsBJT()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.BJT, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleNPNBJT_HasThreeNodes()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(3, netlist.Elements[0].Nodes.Count);
        }

        [Test]
        public void Convert_SingleNPNBJT_ModelNameIsSet()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual("2N2222", netlist.Elements[0].ModelName);
        }

        [Test]
        public void Convert_SingleNPNBJT_ValueIsPositiveForNPN()
        {
            CreateBJTDefinition("bjt_2n2222", BJTPolarity.NPN);
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            // NPN → isNPN=true → Value=1
            Assert.AreEqual(1.0, netlist.Elements[0].Value, 1e-9);
        }

        [Test]
        public void Convert_SinglePNPBJT_ValueIsNegativeForPNP()
        {
            CreateBJTDefinition("bjt_pnp", BJTPolarity.PNP);
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_pnp", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            // PNP → isNPN=false → Value=-1
            Assert.AreEqual(-1.0, netlist.Elements[0].Value, 1e-9);
        }

        [Test]
        public void Convert_SingleNPNBJT_BetaParameterIsSet()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("Bf"), "Should have Bf (Beta) parameter");
            Assert.AreEqual(200.0, elem.Parameters["Bf"], 1e-6);
        }

        [Test]
        public void Convert_SingleNPNBJT_EarlyVoltageParameterIsSet()
        {
            CreateBJTDefinition("bjt_2n2222");
            var board = new BoardState(10, 10);
            var netC = board.CreateNet("COLLECTOR");
            var netB = board.CreateNet("BASE");
            var netE = board.CreateNet("EMITTER");
            PlaceConnected3Pin(board, "bjt_2n2222", 0, 0, netC.NetId, netB.NetId, netE.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("Vaf"), "Should have Vaf (Early voltage) parameter");
            Assert.AreEqual(100.0, elem.Parameters["Vaf"], 1e-6);
        }

        // ════════════════════════════════════════════════════════════════════════
        // MOSFET CONVERSION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_SingleNChannelMOSFET_ElementIdUsesMPrefix()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            var comp = PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual($"M{comp.InstanceId}", netlist.Elements[0].Id);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_ElementTypeIsMOSFET()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(ElementType.MOSFET, netlist.Elements[0].Type);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_HasFourNodes()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            // Drain, Gate, Source, Bulk (auto-tied to Source)
            Assert.AreEqual(4, netlist.Elements[0].Nodes.Count);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_BulkTiedToSource()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            // Nodes[2]=Source, Nodes[3]=Bulk, they must be equal
            Assert.AreEqual(elem.Nodes[2], elem.Nodes[3],
                "Bulk node should be tied to Source node");
            Assert.AreEqual("SOURCE", elem.Nodes[3]);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_ModelNameIsSet()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual("2N7000", netlist.Elements[0].ModelName);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_ValueIsPositiveForNChannel()
        {
            CreateMOSFETDefinition("mosfet_2n7000", FETPolarity.NChannel);
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            // NChannel → isNChannel=true → Value=1
            Assert.AreEqual(1.0, netlist.Elements[0].Value, 1e-9);
        }

        [Test]
        public void Convert_SinglePChannelMOSFET_ValueIsNegativeForPChannel()
        {
            CreateMOSFETDefinition("mosfet_pch", FETPolarity.PChannel);
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_pch", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            // PChannel → isNChannel=false → Value=-1
            Assert.AreEqual(-1.0, netlist.Elements[0].Value, 1e-9);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_ThresholdVoltageParameterIsSet()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("Vto"), "Should have Vto parameter");
            Assert.AreEqual(2.0, elem.Parameters["Vto"], 1e-6);
        }

        [Test]
        public void Convert_SingleNChannelMOSFET_TransconductanceParameterIsSet()
        {
            CreateMOSFETDefinition("mosfet_2n7000");
            var board = new BoardState(10, 10);
            var netD = board.CreateNet("DRAIN");
            var netG = board.CreateNet("GATE");
            var netS = board.CreateNet("SOURCE");
            PlaceConnected3Pin(board, "mosfet_2n7000", 0, 0, netD.NetId, netG.NetId, netS.NetId);

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.IsTrue(elem.Parameters.ContainsKey("Kp"), "Should have Kp parameter");
            Assert.AreEqual(0.3, elem.Parameters["Kp"], 1e-6);
        }

        // ════════════════════════════════════════════════════════════════════════
        // GROUND COMPONENT
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_GroundComponent_ReturnsNoElements()
        {
            CreateDefinition("ground", ComponentKind.Ground);
            var board = new BoardState(10, 10);
            board.PlaceComponent("ground", new GridPosition(0, 0), 0, CreatePins(1));

            var netlist = _converter.Convert(board);

            Assert.AreEqual(0, netlist.Elements.Count,
                "Ground component should not generate a netlist element");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PROBE COMPONENT
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_ProbeComponent_ReturnsNoElements()
        {
            CreateDefinition("probe", ComponentKind.Probe);
            var board = new BoardState(10, 10);
            board.PlaceComponent("probe", new GridPosition(0, 0), 0, CreatePins(1));

            var netlist = _converter.Convert(board);

            Assert.AreEqual(0, netlist.Elements.Count,
                "Probe component should not generate a netlist element");
        }

        // ════════════════════════════════════════════════════════════════════════
        // UNCONNECTED / MISSING NET TESTS
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_UnconnectedPin_NodeNameUsesNCPattern()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            // Place component but don't connect any pins
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2));

            var netlist = _converter.Convert(board);

            Assert.AreEqual(1, netlist.Elements.Count);
            var elem = netlist.Elements[0];
            // Unconnected pins should produce NC_{instanceId}_{pinIndex}
            Assert.AreEqual($"NC_{comp.InstanceId}_0", elem.Nodes[0]);
            Assert.AreEqual($"NC_{comp.InstanceId}_1", elem.Nodes[1]);
        }

        [Test]
        public void Convert_PinConnectedToInvalidNet_NodeNameUsesNCPattern()
        {
            // We test the "net not found" path by directly manipulating
            // This is harder to trigger because ConnectPinToNet validates netId.
            // Instead, verify the unconnected path is consistent with the NC pattern.
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var net = board.CreateNet("NET_A");
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2));
            // Connect only pin 0
            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            // Pin 1 stays unconnected

            var netlist = _converter.Convert(board);

            var elem = netlist.Elements[0];
            Assert.AreEqual("NET_A", elem.Nodes[0], "Pin 0 should map to NET_A");
            Assert.AreEqual($"NC_{comp.InstanceId}_1", elem.Nodes[1],
                "Unconnected pin 1 should use NC pattern");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PROBES PARAMETER
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_WithProbesParameter_ProbesAddedToNetlist()
        {
            var board = new BoardState(10, 10);
            var probes = new[]
            {
                ProbeDefinition.Voltage("V_out", "OUT"),
                ProbeDefinition.Voltage("V_in", "IN")
            };

            var netlist = _converter.Convert(board, probes);

            Assert.AreEqual(2, netlist.Probes.Count);
        }

        [Test]
        public void Convert_WithProbesParameter_ProbeIdAndTargetArePreserved()
        {
            var board = new BoardState(10, 10);
            var probe = ProbeDefinition.Voltage("V_test", "TEST_NODE");

            var netlist = _converter.Convert(board, new[] { probe });

            Assert.AreEqual(1, netlist.Probes.Count);
            Assert.AreEqual("V_test", netlist.Probes[0].Id);
            Assert.AreEqual("TEST_NODE", netlist.Probes[0].Target);
        }

        [Test]
        public void Convert_NullProbesParameter_NoProbesAdded()
        {
            var board = new BoardState(10, 10);

            var netlist = _converter.Convert(board, null);

            Assert.AreEqual(0, netlist.Probes.Count);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CUSTOM VALUE OVERRIDE
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_ResistorWithCustomValue_OverridesDefinitionValue()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            // Place with customValue of 4700 (overrides 1000)
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2), customValue: 4700f);
            board.ConnectPinToNet(netA.NetId, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netB.NetId, new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));

            var netlist = _converter.Convert(board);

            Assert.AreEqual(4700.0, netlist.Elements[0].Value, 1e-9,
                "CustomValue should override definition resistance");
        }

        [Test]
        public void Convert_VoltageSourceWithCustomValue_OverridesDefinitionValue()
        {
            CreateVoltageSourceDefinition("vsource_5v", 5f);
            var board = new BoardState(10, 10);
            var netPos = board.CreateNet("VIN");
            var netNeg = board.CreateNet("GND");
            var comp = board.PlaceComponent("vsource_5v", new GridPosition(0, 0), 0, CreatePins(2), customValue: 12f);
            board.ConnectPinToNet(netPos.NetId, new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(netNeg.NetId, new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));

            var netlist = _converter.Convert(board);

            Assert.AreEqual(12.0, netlist.Elements[0].Value, 1e-9,
                "CustomValue should override definition voltage");
        }

        // ════════════════════════════════════════════════════════════════════════
        // MULTI-COMPONENT CIRCUIT
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_MultiComponentCircuit_AllElementsPresent()
        {
            CreateVoltageSourceDefinition("vsource", 5f);
            CreateResistorDefinition("resistor_1k", 1000f);
            CreateResistorDefinition("resistor_2k", 2000f);

            var board = new BoardState(20, 20);
            var netVin = board.CreateNet("VIN");
            var netMid = board.CreateNet("MID");
            var netGnd = board.CreateNet("GND");

            PlaceConnected2Pin(board, "vsource", 0, 0, netVin.NetId, netGnd.NetId);
            PlaceConnected2Pin(board, "resistor_1k", 2, 0, netVin.NetId, netMid.NetId);
            PlaceConnected2Pin(board, "resistor_2k", 4, 0, netMid.NetId, netGnd.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual(3, netlist.Elements.Count);
        }

        [Test]
        public void Convert_MultiComponentCircuit_ElementTypesAreCorrect()
        {
            CreateVoltageSourceDefinition("vsource", 5f);
            CreateResistorDefinition("resistor_1k", 1000f);

            var board = new BoardState(20, 20);
            var netVin = board.CreateNet("VIN");
            var netOut = board.CreateNet("OUT");
            var netGnd = board.CreateNet("GND");

            PlaceConnected2Pin(board, "vsource", 0, 0, netVin.NetId, netGnd.NetId);
            PlaceConnected2Pin(board, "resistor_1k", 2, 0, netVin.NetId, netOut.NetId);

            var netlist = _converter.Convert(board);

            var types = netlist.Elements.Select(e => e.Type).ToList();
            Assert.Contains(ElementType.VoltageSource, types);
            Assert.Contains(ElementType.Resistor, types);
        }

        // ════════════════════════════════════════════════════════════════════════
        // MISSING COMPONENT DEFINITION
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_ComponentDefinitionNotFound_ThrowsInvalidOperationException()
        {
            // Place a component whose def ID has no registered definition
            var board = new BoardState(10, 10);
            board.PlaceComponent("unknown_component", new GridPosition(0, 0), 0, CreatePins(2));

            Assert.Throws<InvalidOperationException>(() => _converter.Convert(board));
        }

        // ════════════════════════════════════════════════════════════════════════
        // GetElementPrefix STATIC TESTS
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void GetElementPrefix_Resistor_ReturnsR()
        {
            Assert.AreEqual("R", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Resistor));
        }

        [Test]
        public void GetElementPrefix_Capacitor_ReturnsC()
        {
            Assert.AreEqual("C", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Capacitor));
        }

        [Test]
        public void GetElementPrefix_Inductor_ReturnsL()
        {
            Assert.AreEqual("L", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Inductor));
        }

        [Test]
        public void GetElementPrefix_VoltageSource_ReturnsV()
        {
            Assert.AreEqual("V", BoardToNetlistConverter.GetElementPrefix(ComponentKind.VoltageSource));
        }

        [Test]
        public void GetElementPrefix_CurrentSource_ReturnsI()
        {
            Assert.AreEqual("I", BoardToNetlistConverter.GetElementPrefix(ComponentKind.CurrentSource));
        }

        [Test]
        public void GetElementPrefix_Diode_ReturnsD()
        {
            Assert.AreEqual("D", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Diode));
        }

        [Test]
        public void GetElementPrefix_LED_ReturnsD()
        {
            Assert.AreEqual("D", BoardToNetlistConverter.GetElementPrefix(ComponentKind.LED));
        }

        [Test]
        public void GetElementPrefix_ZenerDiode_ReturnsD()
        {
            Assert.AreEqual("D", BoardToNetlistConverter.GetElementPrefix(ComponentKind.ZenerDiode));
        }

        [Test]
        public void GetElementPrefix_BJT_ReturnsQ()
        {
            Assert.AreEqual("Q", BoardToNetlistConverter.GetElementPrefix(ComponentKind.BJT));
        }

        [Test]
        public void GetElementPrefix_MOSFET_ReturnsM()
        {
            Assert.AreEqual("M", BoardToNetlistConverter.GetElementPrefix(ComponentKind.MOSFET));
        }

        [Test]
        public void GetElementPrefix_Ground_ReturnsX()
        {
            // Default case → X (unsupported/fallback prefix)
            Assert.AreEqual("X", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Ground));
        }

        [Test]
        public void GetElementPrefix_Probe_ReturnsX()
        {
            Assert.AreEqual("X", BoardToNetlistConverter.GetElementPrefix(ComponentKind.Probe));
        }

        // ════════════════════════════════════════════════════════════════════════
        // MapComponentKind STATIC TESTS
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void MapComponentKind_Resistor_ReturnsResistor()
        {
            Assert.AreEqual(ElementType.Resistor, BoardToNetlistConverter.MapComponentKind(ComponentKind.Resistor));
        }

        [Test]
        public void MapComponentKind_Capacitor_ReturnsCapacitor()
        {
            Assert.AreEqual(ElementType.Capacitor, BoardToNetlistConverter.MapComponentKind(ComponentKind.Capacitor));
        }

        [Test]
        public void MapComponentKind_Inductor_ReturnsInductor()
        {
            Assert.AreEqual(ElementType.Inductor, BoardToNetlistConverter.MapComponentKind(ComponentKind.Inductor));
        }

        [Test]
        public void MapComponentKind_VoltageSource_ReturnsVoltageSource()
        {
            Assert.AreEqual(ElementType.VoltageSource, BoardToNetlistConverter.MapComponentKind(ComponentKind.VoltageSource));
        }

        [Test]
        public void MapComponentKind_CurrentSource_ReturnsCurrentSource()
        {
            Assert.AreEqual(ElementType.CurrentSource, BoardToNetlistConverter.MapComponentKind(ComponentKind.CurrentSource));
        }

        [Test]
        public void MapComponentKind_Diode_ReturnsDiode()
        {
            Assert.AreEqual(ElementType.Diode, BoardToNetlistConverter.MapComponentKind(ComponentKind.Diode));
        }

        [Test]
        public void MapComponentKind_LED_ReturnsDiode()
        {
            Assert.AreEqual(ElementType.Diode, BoardToNetlistConverter.MapComponentKind(ComponentKind.LED));
        }

        [Test]
        public void MapComponentKind_ZenerDiode_ReturnsDiode()
        {
            Assert.AreEqual(ElementType.Diode, BoardToNetlistConverter.MapComponentKind(ComponentKind.ZenerDiode));
        }

        [Test]
        public void MapComponentKind_BJT_ReturnsBJT()
        {
            Assert.AreEqual(ElementType.BJT, BoardToNetlistConverter.MapComponentKind(ComponentKind.BJT));
        }

        [Test]
        public void MapComponentKind_MOSFET_ReturnsMOSFET()
        {
            Assert.AreEqual(ElementType.MOSFET, BoardToNetlistConverter.MapComponentKind(ComponentKind.MOSFET));
        }

        [Test]
        public void MapComponentKind_Ground_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => BoardToNetlistConverter.MapComponentKind(ComponentKind.Ground));
        }

        [Test]
        public void MapComponentKind_Probe_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => BoardToNetlistConverter.MapComponentKind(ComponentKind.Probe));
        }

        // ════════════════════════════════════════════════════════════════════════
        // BOARD IDs AUTO-INCREMENT FROM 1
        // ════════════════════════════════════════════════════════════════════════

        [Test]
        public void Convert_FirstPlacedComponent_InstanceIdIsOne()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var comp = board.PlaceComponent("resistor_1k", new GridPosition(0, 0), 0, CreatePins(2));

            Assert.AreEqual(1, comp.InstanceId, "First placed component should have InstanceId=1");
        }

        [Test]
        public void Convert_FirstResistor_ElementIdIsR1()
        {
            CreateResistorDefinition("resistor_1k", 1000f);
            var board = new BoardState(10, 10);
            var netA = board.CreateNet("NET_A");
            var netB = board.CreateNet("NET_B");
            PlaceConnected2Pin(board, "resistor_1k", 0, 0, netA.NetId, netB.NetId);

            var netlist = _converter.Convert(board);

            Assert.AreEqual("R1", netlist.Elements[0].Id);
        }
    }
}
