using System;
using System.Collections.Generic;
using System.Reflection;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using CircuitCraft.Views;
using NUnit.Framework;
using UnityEngine;

namespace CircuitCraft.Tests.Views
{
    [TestFixture]
    public class SimulationDataMapperTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ExtractNodeVoltages_NullResult_ReturnsEmpty()
        {
            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(null);

            Assert.IsNotNull(nodeVoltages);
            Assert.AreEqual(0, nodeVoltages.Count);
        }

        [Test]
        public void ExtractNodeVoltages_NullProbeResults_ReturnsEmpty()
        {
            var result = new SimulationResult { ProbeResults = null };

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);

            Assert.IsNotNull(nodeVoltages);
            Assert.AreEqual(0, nodeVoltages.Count);
        }

        [Test]
        public void ExtractNodeVoltages_EmptyProbeResults_ReturnsEmpty()
        {
            var result = new SimulationResult();

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);

            Assert.AreEqual(0, nodeVoltages.Count);
        }

        [Test]
        public void ExtractNodeVoltages_NonVoltageProbes_AreFilteredOut()
        {
            var result = new SimulationResult
            {
                ProbeResults = new List<ProbeResult>
                {
                    new("I_R1", ProbeType.Current, "R1", 0.002),
                    new("P_R1", ProbeType.Power, "R1", 0.0004)
                }
            };

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);

            Assert.AreEqual(0, nodeVoltages.Count);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void ExtractNodeVoltages_WhitespaceOrNullTargets_AreFilteredOut(string target)
        {
            var result = new SimulationResult
            {
                ProbeResults = new List<ProbeResult>
                {
                    new("V_target", ProbeType.Voltage, target, 1.23)
                }
            };

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);

            Assert.AreEqual(0, nodeVoltages.Count);
        }

        [Test]
        public void ExtractNodeVoltages_ValidVoltageProbes_AreExtracted()
        {
            var result = new SimulationResult
            {
                ProbeResults = new List<ProbeResult>
                {
                    new("V_a", ProbeType.Voltage, "A", 3.3),
                    new("V_b", ProbeType.Voltage, "B", 1.1),
                    new("I_r1", ProbeType.Current, "R1", 0.002)
                }
            };

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);

            Assert.AreEqual(2, nodeVoltages.Count);
            Assert.AreEqual(3.3, nodeVoltages["A"]);
            Assert.AreEqual(1.1, nodeVoltages["B"]);
        }

        [Test]
        public void GetComponentCurrent_NullResult_ReturnsNull()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 1, null);

            Assert.IsNull(current);
        }

        [Test]
        public void GetComponentCurrent_NullDefinition_ReturnsNull()
        {
            var result = MakeCurrentResult("R1", 0.01);

            var current = SimulationDataMapper.GetComponentCurrent(null, 1, result);

            Assert.IsNull(current);
        }

        [Test]
        public void GetComponentCurrent_GroundKind_ReturnsNull()
        {
            var definition = CreateDefinition(ComponentKind.Ground);
            var result = MakeCurrentResult("X1", 0.01);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 1, result);

            Assert.IsNull(current);
        }

        [Test]
        public void GetComponentCurrent_ProbeKind_ReturnsNull()
        {
            var definition = CreateDefinition(ComponentKind.Probe);
            var result = MakeCurrentResult("X1", 0.01);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 1, result);

            Assert.IsNull(current);
        }

        [Test]
        public void GetComponentCurrent_ResistorKind_UsesElementPrefix()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);
            var result = MakeCurrentResult("R5", 0.004);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 5, result);

            Assert.AreEqual(0.004, current);
        }

        [Test]
        public void GetComponentCurrent_LedKind_UsesElementPrefix()
        {
            var definition = CreateDefinition(ComponentKind.LED);
            var result = MakeCurrentResult("D7", 0.0025);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 7, result);

            Assert.AreEqual(0.0025, current);
        }

        [Test]
        public void GetComponentCurrent_VoltageSourceKind_UsesElementPrefix()
        {
            var definition = CreateDefinition(ComponentKind.VoltageSource);
            var result = MakeCurrentResult("V2", 0.015);

            var current = SimulationDataMapper.GetComponentCurrent(definition, 2, result);

            Assert.AreEqual(0.015, current);
        }

        [TestCase(0.005f, 0, 0.005f)]
        [TestCase(-0.005f, 0, -0.005f)]
        [TestCase(0.005f, 1, -0.005f)]
        [TestCase(-0.005f, 1, 0.005f)]
        [TestCase(0.005f, 2, 0f)]
        [TestCase(0.005f, 3, 0f)]
        public void GetPinSignedCurrentContribution_ReturnsExpected(float current, int pinIndex, float expected)
        {
            var contribution = SimulationDataMapper.GetPinSignedCurrentContribution(current, pinIndex);

            Assert.AreEqual(expected, contribution);
        }

        [Test]
        public void ResolveResistorValue_ComponentCustomValue_HasPriority()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 220f);
            var component = CreatePlacedComponent(customValue: 330f);

            var resistance = SimulationDataMapper.ResolveResistorValue(definition, component);

            Assert.AreEqual(330f, resistance);
        }

        [Test]
        public void ResolveResistorValue_NoCustomValue_UsesDefinitionResistance()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 680f);
            var component = CreatePlacedComponent();

            var resistance = SimulationDataMapper.ResolveResistorValue(definition, component);

            Assert.AreEqual(680f, resistance);
        }

        [Test]
        public void ResolveResistorValue_ZeroDefinitionResistance_UsesFallback()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 0f);
            var component = CreatePlacedComponent();

            var resistance = SimulationDataMapper.ResolveResistorValue(definition, component);

            Assert.AreEqual(1000f, resistance);
        }

        [Test]
        public void ResolveResistorValue_NullDefinition_ReturnsZero()
        {
            var component = CreatePlacedComponent();

            var resistance = SimulationDataMapper.ResolveResistorValue(null, component);

            Assert.AreEqual(0f, resistance);
        }

        [Test]
        public void ResolveResistorValue_NullComponent_UsesDefinitionResistance()
        {
            var definition = CreateDefinition(ComponentKind.Resistor, 470f);

            var resistance = SimulationDataMapper.ResolveResistorValue(definition, null);

            Assert.AreEqual(470f, resistance);
        }

        [Test]
        public void BuildTraceSegmentCurrentMap_EmptyTraces_ReturnsEmpty()
        {
            var board = new BoardState(10, 10);
            var map = SimulationDataMapper.BuildTraceSegmentCurrentMap(
                board.Traces,
                new Dictionary<int, ComponentView>(),
                board,
                new SimulationResult(),
                1e-6f);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void BuildTraceSegmentCurrentMap_NoMatchingNetCurrent_ReturnsEmpty()
        {
            var board = new BoardState(10, 10);
            var traceNet = board.CreateNet("TRACE_NET");
            var sourceNet = board.CreateNet("SOURCE_NET");
            board.AddTrace(traceNet.NetId, new GridPosition(0, 0), new GridPosition(1, 0));

            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);
            var component = board.PlaceComponent("res", new GridPosition(2, 0), 0, CreatePins(2));
            ConnectPin(board, sourceNet, component, 0);

            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.01);

            var map = SimulationDataMapper.BuildTraceSegmentCurrentMap(board.Traces, componentViews, board, result, 1e-6f);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void BuildTraceSegmentCurrentMap_BelowThreshold_IsFilteredOut()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var trace = board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(1, 0));

            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);
            var component = board.PlaceComponent("res", new GridPosition(2, 0), 0, CreatePins(2));
            ConnectPin(board, net, component, 0);

            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.002);

            var map = SimulationDataMapper.BuildTraceSegmentCurrentMap(board.Traces, componentViews, board, result, 0.01f);

            Assert.IsFalse(map.ContainsKey(trace.SegmentId));
        }

        [Test]
        public void BuildTraceSegmentCurrentMap_ValidNetCurrent_MapsTraceSegment()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var trace = board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(1, 0));

            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);
            var component = board.PlaceComponent("res", new GridPosition(2, 0), 0, CreatePins(2));
            ConnectPin(board, net, component, 0);

            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.005);

            var map = SimulationDataMapper.BuildTraceSegmentCurrentMap(board.Traces, componentViews, board, result, 1e-6f);

            Assert.IsTrue(map.ContainsKey(trace.SegmentId));
            Assert.AreEqual(0.005f, map[trace.SegmentId], 1e-7f);
        }

        [Test]
        public void BuildTraceSegmentCurrentMap_CancelledSignedCurrent_UsesFallbackCurrent()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var trace = board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(1, 0));

            var definition = CreateDefinition(ComponentKind.Resistor, 1000f);
            var component = board.PlaceComponent("res", new GridPosition(2, 0), 0, CreatePins(2));
            ConnectPin(board, net, component, 0);
            ConnectPin(board, net, component, 1);

            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.02);

            var map = SimulationDataMapper.BuildTraceSegmentCurrentMap(board.Traces, componentViews, board, result, 1e-6f);

            Assert.IsTrue(map.ContainsKey(trace.SegmentId));
            Assert.AreEqual(0.02f, map[trace.SegmentId], 1e-7f);
        }

        [Test]
        public void GetResistorPowerMap_NoResistors_ReturnsEmpty()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var component = board.PlaceComponent("led", new GridPosition(0, 0), 0, CreatePins(2));
            ConnectPin(board, net, component, 0);

            var definition = CreateDefinition(ComponentKind.LED);
            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"D{component.InstanceId}", 0.01);

            var map = SimulationDataMapper.GetResistorPowerMap(componentViews, board, result);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void GetResistorPowerMap_ResistorWithCurrent_ComputesI2R()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var component = board.PlaceComponent("res", new GridPosition(0, 0), 0, CreatePins(2));
            ConnectPin(board, net, component, 0);

            var definition = CreateDefinition(ComponentKind.Resistor, 200f);
            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.05);

            var map = SimulationDataMapper.GetResistorPowerMap(componentViews, board, result);

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(0.05 * 0.05 * 200.0, map[component.InstanceId], 1e-9);
        }

        [Test]
        public void GetResistorPowerMap_CustomValueResistance_HasPriority()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var component = board.PlaceComponent("res", new GridPosition(0, 0), 0, CreatePins(2), customValue: 330f);
            ConnectPin(board, net, component, 0);

            var definition = CreateDefinition(ComponentKind.Resistor, 100f);
            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.1);

            var map = SimulationDataMapper.GetResistorPowerMap(componentViews, board, result);

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual(0.1 * 0.1 * 330.0, map[component.InstanceId], 1e-9);
        }

        [Test]
        public void GetResistorPowerMap_NonPositiveResistance_IsSkipped()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("N1");
            var component = board.PlaceComponent("res", new GridPosition(0, 0), 0, CreatePins(2), customValue: -5f);
            ConnectPin(board, net, component, 0);

            var definition = CreateDefinition(ComponentKind.Resistor, 220f);
            var view = CreateComponentView(definition);
            var componentViews = new Dictionary<int, ComponentView> { [component.InstanceId] = view };
            var result = MakeCurrentResult($"R{component.InstanceId}", 0.1);

            var map = SimulationDataMapper.GetResistorPowerMap(componentViews, board, result);

            Assert.AreEqual(0, map.Count);
        }

        private SimulationResult MakeCurrentResult(string elementId, double current)
        {
            return new SimulationResult
            {
                ProbeResults = new List<ProbeResult>
                {
                    new($"I_{elementId}", ProbeType.Current, elementId, current)
                }
            };
        }

        private ComponentDefinition CreateDefinition(ComponentKind kind, float resistance = 0f)
        {
            var definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            _createdObjects.Add(definition);

            SetPrivateField(definition, "_kind", kind);
            SetPrivateField(definition, "_resistanceOhms", resistance);

            return definition;
        }

        private ComponentView CreateComponentView(ComponentDefinition definition)
        {
            var gameObject = new GameObject($"View_{definition.Kind}");
            _createdObjects.Add(gameObject);
            var view = gameObject.AddComponent<ComponentView>();
            view.Initialize(definition);
            return view;
        }

        private static PlacedComponent CreatePlacedComponent(float? customValue = null)
        {
            return new PlacedComponent(1, "res", new GridPosition(0, 0), 0, CreatePins(2), customValue);
        }

        private static List<PinInstance> CreatePins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
            {
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            }

            return pins;
        }

        private static void ConnectPin(BoardState board, Net net, PlacedComponent component, int pinIndex)
        {
            board.ConnectPinToNet(net.NetId, new PinReference(component.InstanceId, pinIndex, component.GetPinWorldPosition(pinIndex)));
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value) where T : class
        {
            var type = target.GetType();
            while (type is not null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field is not null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
    }
}
