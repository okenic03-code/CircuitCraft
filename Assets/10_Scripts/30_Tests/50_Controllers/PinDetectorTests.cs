using System.Collections.Generic;
using NUnit.Framework;
using CircuitCraft.Core;
using CircuitCraft.Controllers;

namespace CircuitCraft.Tests.Controllers
{
    [TestFixture]
    public class PinDetectorTests
    {
        private BoardState _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardState(20, 20);
        }

        private PlacedComponent PlaceComponentAt(GridPosition position, params GridPosition[] pinLocalPositions)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < pinLocalPositions.Length; i++)
            {
                pins.Add(new PinInstance(i, $"pin{i}", pinLocalPositions[i]));
            }

            return _board.PlaceComponent("test_component", position, 0, pins);
        }

        [Test]
        public void TryGetNearestPin_NullComponent_ReturnsFalse()
        {
            bool found = PinDetector.TryGetNearestPin(null, new GridPosition(0, 0), out PinReference pinRef);

            Assert.IsFalse(found);
            Assert.AreEqual(default(PinReference), pinRef);
        }

        [Test]
        public void TryGetNearestPin_ExactPinPosition_ReturnsTrue()
        {
            var component = PlaceComponentAt(new GridPosition(5, 7), new GridPosition(0, 0));
            var mousePos = component.GetPinWorldPosition(0);

            bool found = PinDetector.TryGetNearestPin(component, mousePos, out PinReference pinRef);

            Assert.IsTrue(found);
            Assert.AreEqual(component.InstanceId, pinRef.ComponentInstanceId);
            Assert.AreEqual(0, pinRef.PinIndex);
            Assert.AreEqual(mousePos, pinRef.Position);
        }

        [Test]
        public void TryGetNearestPin_AdjacentPosition_ReturnsTrue()
        {
            var component = PlaceComponentAt(new GridPosition(10, 10), new GridPosition(0, 0));
            var mousePos = new GridPosition(11, 10);

            bool found = PinDetector.TryGetNearestPin(component, mousePos, out PinReference pinRef);

            Assert.IsTrue(found);
            Assert.AreEqual(component.InstanceId, pinRef.ComponentInstanceId);
            Assert.AreEqual(0, pinRef.PinIndex);
            Assert.AreEqual(new GridPosition(10, 10), pinRef.Position);
        }

        [Test]
        public void TryGetNearestPin_TooFarAway_ReturnsFalse()
        {
            var component = PlaceComponentAt(new GridPosition(0, 0), new GridPosition(0, 0));
            var mousePos = new GridPosition(2, 0);

            bool found = PinDetector.TryGetNearestPin(component, mousePos, out PinReference pinRef);

            Assert.IsFalse(found);
            Assert.AreEqual(default(PinReference), pinRef);
        }

        [Test]
        public void TryGetNearestPin_MultiplePins_ReturnsNearest()
        {
            var component = PlaceComponentAt(
                new GridPosition(3, 3),
                new GridPosition(0, 0),
                new GridPosition(4, 0),
                new GridPosition(0, 4));
            var mousePos = new GridPosition(7, 3);

            bool found = PinDetector.TryGetNearestPin(component, mousePos, out PinReference pinRef);

            Assert.IsTrue(found);
            Assert.AreEqual(1, pinRef.PinIndex, "Pin 1 should be nearest at world position (7,3)");
            Assert.AreEqual(new GridPosition(7, 3), pinRef.Position);
        }

        [Test]
        public void TryGetNearestPin_ComponentWithNoPins_ReturnsFalse()
        {
            var component = _board.PlaceComponent("empty_pins_component", new GridPosition(1, 1), 0, new List<PinInstance>());

            bool found = PinDetector.TryGetNearestPin(component, new GridPosition(1, 1), out PinReference pinRef);

            Assert.IsFalse(found);
            Assert.AreEqual(default(PinReference), pinRef);
        }

        [Test]
        public void TryGetNearestPinFromAll_EmptyList_ReturnsFalse()
        {
            bool found = PinDetector.TryGetNearestPinFromAll(new List<PlacedComponent>(), new GridPosition(0, 0), out PinReference pinRef);

            Assert.IsFalse(found);
            Assert.AreEqual(default(PinReference), pinRef);
        }

        [Test]
        public void TryGetNearestPinFromAll_MultipleComponents_ReturnsGlobalNearest()
        {
            var farComponent = PlaceComponentAt(new GridPosition(0, 0), new GridPosition(0, 0));
            var nearComponent = PlaceComponentAt(new GridPosition(8, 8), new GridPosition(0, 0), new GridPosition(1, 0));
            var mousePos = new GridPosition(9, 8);

            bool found = PinDetector.TryGetNearestPinFromAll(_board.Components, mousePos, out PinReference pinRef);

            Assert.IsTrue(found);
            Assert.AreEqual(nearComponent.InstanceId, pinRef.ComponentInstanceId);
            Assert.AreEqual(1, pinRef.PinIndex);
            Assert.AreEqual(new GridPosition(9, 8), pinRef.Position);
            Assert.AreNotEqual(farComponent.InstanceId, pinRef.ComponentInstanceId);
        }

        [Test]
        public void TryGetNearestPinFromAll_AllTooFar_ReturnsFalse()
        {
            PlaceComponentAt(new GridPosition(0, 0), new GridPosition(0, 0));
            PlaceComponentAt(new GridPosition(10, 10), new GridPosition(0, 0));

            bool found = PinDetector.TryGetNearestPinFromAll(_board.Components, new GridPosition(5, 5), out PinReference pinRef);

            Assert.IsFalse(found);
            Assert.AreEqual(default(PinReference), pinRef);
        }

        [Test]
        public void TryGetNearestPin_CustomMaxDistance_Respected()
        {
            var component = PlaceComponentAt(new GridPosition(0, 0), new GridPosition(0, 0));
            var mousePos = new GridPosition(2, 0);

            bool foundWithDefault = PinDetector.TryGetNearestPin(component, mousePos, out _);
            bool foundWithCustom = PinDetector.TryGetNearestPin(component, mousePos, out PinReference pinRef, maxDistance: 2);

            Assert.IsFalse(foundWithDefault, "Default maxDistance=1 should reject distance 2.");
            Assert.IsTrue(foundWithCustom, "Custom maxDistance=2 should accept distance 2.");
            Assert.AreEqual(component.InstanceId, pinRef.ComponentInstanceId);
            Assert.AreEqual(0, pinRef.PinIndex);
            Assert.AreEqual(new GridPosition(0, 0), pinRef.Position);
        }
    }
}
