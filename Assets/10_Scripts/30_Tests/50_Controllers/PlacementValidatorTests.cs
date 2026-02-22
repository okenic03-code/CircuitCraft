using System.Collections.Generic;
using CircuitCraft.Controllers;
using CircuitCraft.Core;
using NUnit.Framework;

namespace CircuitCraft.Tests.Controllers
{
    [TestFixture]
    public class PlacementValidatorTests
    {
        private BoardState _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardState(20, 20);
        }

        [Test]
        public void IsValidPlacement_NullBoardState_ReturnsTrue()
        {
            var isValid = PlacementValidator.IsValidPlacement(null, new GridPosition(5, 5));

            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValidPlacement_EmptyPosition_ReturnsTrue()
        {
            var isValid = PlacementValidator.IsValidPlacement(_board, new GridPosition(5, 5));

            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValidPlacement_OccupiedPosition_ReturnsFalse()
        {
            PlaceAt(5, 5);

            var isValid = PlacementValidator.IsValidPlacement(_board, new GridPosition(5, 5));

            Assert.IsFalse(isValid);
        }

        [Test]
        public void IsValidPlacement_DifferentPosition_ReturnsTrue()
        {
            PlaceAt(5, 5);

            var isValid = PlacementValidator.IsValidPlacement(_board, new GridPosition(6, 5));

            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValidPlacement_AfterRemoval_ReturnsTrue()
        {
            var instanceId = PlaceAt(5, 5);
            var removed = _board.RemoveComponent(instanceId);

            var isValid = PlacementValidator.IsValidPlacement(_board, new GridPosition(5, 5));

            Assert.IsTrue(removed);
            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValidPlacement_MultipleComponents_ValidatesCorrectly()
        {
            PlaceAt(1, 1);
            PlaceAt(3, 4);
            PlaceAt(10, 12);

            Assert.IsFalse(PlacementValidator.IsValidPlacement(_board, new GridPosition(1, 1)));
            Assert.IsFalse(PlacementValidator.IsValidPlacement(_board, new GridPosition(3, 4)));
            Assert.IsFalse(PlacementValidator.IsValidPlacement(_board, new GridPosition(10, 12)));
            Assert.IsTrue(PlacementValidator.IsValidPlacement(_board, new GridPosition(2, 1)));
            Assert.IsTrue(PlacementValidator.IsValidPlacement(_board, new GridPosition(0, 0)));
        }

        private int PlaceAt(int x, int y)
        {
            var placed = _board.PlaceComponent("test", new GridPosition(x, y), 0, new List<PinInstance>());
            return placed.InstanceId;
        }
    }
}
