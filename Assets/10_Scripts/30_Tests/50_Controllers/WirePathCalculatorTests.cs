using CircuitCraft.Controllers;
using CircuitCraft.Core;
using NUnit.Framework;

namespace CircuitCraft.Tests.Controllers
{
    [TestFixture]
    public class WirePathCalculatorTests
    {
        [Test]
        public void BuildManhattanSegments_SameX_ReturnsSingleSegment()
        {
            var segments = WirePathCalculator.BuildManhattanSegments(new GridPosition(0, 0), new GridPosition(0, 5));

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual(new GridPosition(0, 0), segments[0].start);
            Assert.AreEqual(new GridPosition(0, 5), segments[0].end);
        }

        [Test]
        public void BuildManhattanSegments_SameY_ReturnsSingleSegment()
        {
            var segments = WirePathCalculator.BuildManhattanSegments(new GridPosition(0, 3), new GridPosition(5, 3));

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual(new GridPosition(0, 3), segments[0].start);
            Assert.AreEqual(new GridPosition(5, 3), segments[0].end);
        }

        [Test]
        public void BuildManhattanSegments_DifferentXY_ReturnsTwoSegments()
        {
            var segments = WirePathCalculator.BuildManhattanSegments(new GridPosition(0, 0), new GridPosition(3, 4));

            Assert.AreEqual(2, segments.Count);
            Assert.AreEqual(new GridPosition(0, 0), segments[0].start);
            Assert.AreEqual(new GridPosition(3, 0), segments[0].end);
            Assert.AreEqual(new GridPosition(3, 0), segments[1].start);
            Assert.AreEqual(new GridPosition(3, 4), segments[1].end);
        }

        [Test]
        public void BuildManhattanSegments_DifferentXY_CornerIsAtEndXStartY()
        {
            var start = new GridPosition(2, -1);
            var end = new GridPosition(-4, 7);
            var segments = WirePathCalculator.BuildManhattanSegments(start, end);

            var expectedCorner = new GridPosition(end.X, start.Y);
            Assert.AreEqual(2, segments.Count);
            Assert.AreEqual(expectedCorner, segments[0].end);
            Assert.AreEqual(expectedCorner, segments[1].start);
        }

        [Test]
        public void BuildManhattanSegments_SamePoint_ReturnsSingleSegment()
        {
            var point = new GridPosition(2, 2);
            var segments = WirePathCalculator.BuildManhattanSegments(point, point);

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual(point, segments[0].start);
            Assert.AreEqual(point, segments[0].end);
        }

        [Test]
        public void IsPointOnTrace_VerticalTrace_PointOnTrace_ReturnsTrue()
        {
            var trace = CreateTrace(new GridPosition(2, 1), new GridPosition(2, 5));

            Assert.IsTrue(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(2, 3)));
        }

        [Test]
        public void IsPointOnTrace_VerticalTrace_PointOffTrace_ReturnsFalse()
        {
            var trace = CreateTrace(new GridPosition(2, 1), new GridPosition(2, 5));

            Assert.IsFalse(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(2, 6)));
        }

        [Test]
        public void IsPointOnTrace_VerticalTrace_WrongX_ReturnsFalse()
        {
            var trace = CreateTrace(new GridPosition(2, 1), new GridPosition(2, 5));

            Assert.IsFalse(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(3, 3)));
        }

        [Test]
        public void IsPointOnTrace_HorizontalTrace_PointOnTrace_ReturnsTrue()
        {
            var trace = CreateTrace(new GridPosition(1, 4), new GridPosition(5, 4));

            Assert.IsTrue(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(3, 4)));
        }

        [Test]
        public void IsPointOnTrace_HorizontalTrace_PointOffTrace_ReturnsFalse()
        {
            var trace = CreateTrace(new GridPosition(1, 4), new GridPosition(5, 4));

            Assert.IsFalse(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(6, 4)));
        }

        [Test]
        public void IsPointOnTrace_HorizontalTrace_WrongY_ReturnsFalse()
        {
            var trace = CreateTrace(new GridPosition(1, 4), new GridPosition(5, 4));

            Assert.IsFalse(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(3, 5)));
        }

        [Test]
        public void IsPointOnTrace_PointAtStart_ReturnsTrue()
        {
            var trace = CreateTrace(new GridPosition(1, 4), new GridPosition(5, 4));

            Assert.IsTrue(WirePathCalculator.IsPointOnTrace(trace, trace.Start));
        }

        [Test]
        public void IsPointOnTrace_PointAtEnd_ReturnsTrue()
        {
            var trace = CreateTrace(new GridPosition(1, 4), new GridPosition(5, 4));

            Assert.IsTrue(WirePathCalculator.IsPointOnTrace(trace, trace.End));
        }

        [Test]
        public void IsPointOnTrace_ReversedStartEnd_StillWorks()
        {
            var trace = CreateTrace(new GridPosition(5, 4), new GridPosition(1, 4));

            Assert.IsTrue(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(3, 4)));
            Assert.IsFalse(WirePathCalculator.IsPointOnTrace(trace, new GridPosition(0, 4)));
        }

        private static TraceSegment CreateTrace(GridPosition start, GridPosition end)
        {
            return new TraceSegment(1, 1, start, end);
        }
    }
}
