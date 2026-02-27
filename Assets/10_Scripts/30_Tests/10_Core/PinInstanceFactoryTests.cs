using System.Collections.Generic;
using System.Reflection;
using CircuitCraft.Core;
using CircuitCraft.Data;
using NUnit.Framework;
using UnityEngine;

namespace CircuitCraft.Tests.Core
{
    [TestFixture]
    public class PinInstanceFactoryTests
    {
        private readonly List<ComponentDefinition> _createdDefinitions = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdDefinitions.Count; i++)
            {
                if (_createdDefinitions[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdDefinitions[i]);
                }
            }

            _createdDefinitions.Clear();
        }

        [Test]
        public void CreatePinInstances_GroundWithoutExplicitPins_UsesSingleGroundPin()
        {
            var definition = CreateDefinition("ground_test", ComponentKind.Ground, new PinDefinition[0]);

            var pins = PinInstanceFactory.CreatePinInstances(definition);

            Assert.AreEqual(1, pins.Count);
            Assert.AreEqual("GND", pins[0].PinName);
            Assert.AreEqual(new GridPosition(0, 0), pins[0].LocalPosition);
        }

        [Test]
        public void CreatePinInstances_ProbeWithoutExplicitPins_UsesSingleProbePin()
        {
            var definition = CreateDefinition("probe_test", ComponentKind.Probe, new PinDefinition[0]);

            var pins = PinInstanceFactory.CreatePinInstances(definition);

            Assert.AreEqual(1, pins.Count);
            Assert.AreEqual("P", pins[0].PinName);
            Assert.AreEqual(new GridPosition(0, 0), pins[0].LocalPosition);
        }

        [Test]
        public void CreatePinInstances_WithExplicitPins_PreservesExplicitPins()
        {
            var explicitPins = new[]
            {
                new PinDefinition("IN", new Vector2Int(2, 3)),
                new PinDefinition("OUT", new Vector2Int(4, 3))
            };
            var definition = CreateDefinition("explicit_test", ComponentKind.Resistor, explicitPins);

            var pins = PinInstanceFactory.CreatePinInstances(definition);

            Assert.AreEqual(2, pins.Count);
            Assert.AreEqual("IN", pins[0].PinName);
            Assert.AreEqual(new GridPosition(2, 3), pins[0].LocalPosition);
            Assert.AreEqual("OUT", pins[1].PinName);
            Assert.AreEqual(new GridPosition(4, 3), pins[1].LocalPosition);
        }

        private ComponentDefinition CreateDefinition(string id, ComponentKind kind, PinDefinition[] pins)
        {
            var definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            _createdDefinitions.Add(definition);

            SetPrivateField(definition, "_id", id);
            SetPrivateField(definition, "_displayName", id);
            SetPrivateField(definition, "_kind", kind);
            SetPrivateField(definition, "_pins", pins);

            return definition;
        }

        private static void SetPrivateField<TValue>(ComponentDefinition definition, string fieldName, TValue value)
        {
            var field = typeof(ComponentDefinition).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on ComponentDefinition.");
            field.SetValue(definition, value);
        }
    }
}
