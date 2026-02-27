using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Core;

namespace CircuitCraft.Tests.Core
{
    [TestFixture]
    public class DRCCheckerTests
    {
        private DRCChecker _checker;

        [SetUp]
        public void SetUp()
        {
            _checker = new DRCChecker();
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private static List<PinInstance> CreatePins(int count)
        {
            var pins = new List<PinInstance>();
            for (int i = 0; i < count; i++)
                pins.Add(new PinInstance(i, $"pin{i}", new GridPosition(i, 0)));
            return pins;
        }

        // ── Test 1: null board throws ArgumentNullException ──────────────────────

        [Test]
        public void Check_NullBoard_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _checker.Check(null));
        }

        // ── Test 2: empty board → no violations ──────────────────────────────────

        [Test]
        public void Check_EmptyBoard_ReturnsNoViolations()
        {
            var board = new BoardState(10, 10);

            var result = _checker.Check(board);

            Assert.IsFalse(result.HasViolations);
            Assert.AreEqual(0, result.ShortCount);
            Assert.AreEqual(0, result.UnconnectedCount);
            Assert.AreEqual(0, result.Violations.Count);
        }

        // ── Test 3: all pins connected → no unconnected pin violations ───────────

        [Test]
        public void Check_AllPinsConnected_ReturnsNoUnconnectedPins()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("NET1");

            var pins = CreatePins(2);
            var comp = board.PlaceComponent("resistor", new GridPosition(0, 0), 0, pins);

            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));
            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 1, comp.GetPinWorldPosition(1)));

            var result = _checker.Check(board);

            Assert.AreEqual(0, result.UnconnectedCount);
        }

        // ── Test 4: unconnected pins are detected ────────────────────────────────

        [Test]
        public void Check_UnconnectedPins_DetectsAllPins()
        {
            var board = new BoardState(10, 10);

            var pins = CreatePins(2);
            board.PlaceComponent("resistor", new GridPosition(0, 0), 0, pins);

            var result = _checker.Check(board);

            Assert.IsTrue(result.HasViolations);
            Assert.AreEqual(2, result.UnconnectedCount);
            Assert.IsTrue(result.Violations.All(v => v.ViolationType == DRCViolationType.UnconnectedPin));
        }

        // ── Test 5: traces from different nets crossing geometrically → no short (schematic-style) ───

        [Test]
        public void Check_OverlappingTracesFromDifferentNets_NoShort()
        {
            var board = new BoardState(20, 20);
            var net1 = board.CreateNet("NET1");
            var net2 = board.CreateNet("NET2");

            // net1 trace: horizontal from (0,0) to (5,0)
            board.AddTrace(net1.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            // net2 trace: vertical from (3,0) to (3,5) — geometrically crosses at (3,0)
            board.AddTrace(net2.NetId, new GridPosition(3, 0), new GridPosition(3, 5));

            var result = _checker.Check(board);

            // Schematic-style: geometric crossing is NOT a short
            Assert.AreEqual(0, result.ShortCount);
        }

        // ── Test 6: traces from same net overlapping → no short ──────────────────

        [Test]
        public void Check_OverlappingTracesFromSameNet_NoShort()
        {
            var board = new BoardState(20, 20);
            var net = board.CreateNet("NET1");

            // Two traces on the same net that share positions
            board.AddTrace(net.NetId, new GridPosition(0, 0), new GridPosition(5, 0));
            board.AddTrace(net.NetId, new GridPosition(3, 0), new GridPosition(8, 0));

            var result = _checker.Check(board);

            Assert.AreEqual(0, result.ShortCount);
        }

        // ── Test 7: traces from different nets that do NOT overlap → no short ─────

        [Test]
        public void Check_NonOverlappingTracesFromDifferentNets_NoShort()
        {
            var board = new BoardState(20, 20);
            var net1 = board.CreateNet("NET1");
            var net2 = board.CreateNet("NET2");

            // net1: (0,0)→(2,0)
            board.AddTrace(net1.NetId, new GridPosition(0, 0), new GridPosition(2, 0));
            // net2: (5,0)→(7,0) — separated, no overlap
            board.AddTrace(net2.NetId, new GridPosition(5, 0), new GridPosition(7, 0));

            var result = _checker.Check(board);

            Assert.AreEqual(0, result.ShortCount);
        }

        // ── Test 8: crossing traces AND unconnected pins → only unconnected detected ──

        [Test]
        public void Check_CrossingTracesAndUnconnectedPins_DetectsOnlyUnconnected()
        {
            var board = new BoardState(20, 20);
            var net1 = board.CreateNet("NET1");
            var net2 = board.CreateNet("NET2");

            // Crossing traces (schematic-style: NOT a short)
            board.AddTrace(net1.NetId, new GridPosition(0, 3), new GridPosition(5, 3));
            board.AddTrace(net2.NetId, new GridPosition(2, 0), new GridPosition(2, 5));

            // Unconnected pins: component with 2 pins not wired to anything
            var pins = CreatePins(2);
            board.PlaceComponent("resistor", new GridPosition(10, 10), 0, pins);

            var result = _checker.Check(board);

            Assert.IsTrue(result.HasViolations);
            Assert.AreEqual(0, result.ShortCount);
            Assert.AreEqual(2, result.UnconnectedCount);
        }

        // ── Test 9: component with some pins connected, some not ─────────────────

        [Test]
        public void Check_PartiallyConnectedComponent_DetectsOnlyUnconnectedPins()
        {
            var board = new BoardState(10, 10);
            var net = board.CreateNet("NET1");

            var pins = CreatePins(3);
            var comp = board.PlaceComponent("transistor", new GridPosition(0, 0), 0, pins);

            // Only connect pin 0
            board.ConnectPinToNet(net.NetId,
                new PinReference(comp.InstanceId, 0, comp.GetPinWorldPosition(0)));

            var result = _checker.Check(board);

            Assert.IsTrue(result.HasViolations);
            Assert.AreEqual(2, result.UnconnectedCount,
                "Pins 1 and 2 are unconnected; only pin 0 was wired.");
            Assert.AreEqual(0, result.ShortCount);
        }
    }
}
