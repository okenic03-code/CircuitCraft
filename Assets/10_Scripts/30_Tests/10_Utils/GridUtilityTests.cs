using NUnit.Framework;
using UnityEngine;
using CircuitCraft.Utils;

namespace CircuitCraft.Tests.Utils
{
    /// <summary>
    /// NUnit EditMode characterization tests for GridUtility static utility class.
    /// Documents current behavior for GridToWorldPosition and IsInsideSuggestedArea.
    /// ScreenToGridPosition is excluded — requires Camera, not available in EditMode.
    /// </summary>
    [TestFixture]
    public class GridUtilityTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // GridToWorldPosition
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void GridToWorldPosition_OriginZero_CellSizeOne_GridPosZero_ReturnsWorldOrigin()
        {
            Vector3 origin = Vector3.zero;
            float cellSize = 1f;
            Vector2Int gridPos = new Vector2Int(0, 0);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            Assert.AreEqual(new Vector3(0f, 0f, 0f), result);
        }

        [Test]
        public void GridToWorldPosition_OriginZero_CellSizeOne_GridPos3x5_ReturnsWorld3x0x5()
        {
            Vector3 origin = Vector3.zero;
            float cellSize = 1f;
            Vector2Int gridPos = new Vector2Int(3, 5);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            // gridPosition.y maps to worldZ, gridPosition.x maps to worldX
            Assert.AreEqual(new Vector3(3f, 0f, 5f), result);
        }

        [Test]
        public void GridToWorldPosition_CellSizeTwo_GridPos1x1_ReturnsWorld2x0x2()
        {
            Vector3 origin = Vector3.zero;
            float cellSize = 2f;
            Vector2Int gridPos = new Vector2Int(1, 1);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            Assert.AreEqual(new Vector3(2f, 0f, 2f), result);
        }

        [Test]
        public void GridToWorldPosition_NegativeGridPos_ReturnsNegativeWorldXZ()
        {
            Vector3 origin = Vector3.zero;
            float cellSize = 1f;
            Vector2Int gridPos = new Vector2Int(-1, -2);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            Assert.AreEqual(new Vector3(-1f, 0f, -2f), result);
        }

        [Test]
        public void GridToWorldPosition_NonZeroOrigin_OffsetIsAdded()
        {
            Vector3 origin = new Vector3(10f, 0f, 10f);
            float cellSize = 1f;
            Vector2Int gridPos = new Vector2Int(3, 3);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            Assert.AreEqual(new Vector3(13f, 0f, 13f), result);
        }

        [Test]
        public void GridToWorldPosition_OriginWithNonZeroY_YComponentPreserved()
        {
            Vector3 origin = new Vector3(0f, 5f, 0f);
            float cellSize = 1f;
            Vector2Int gridPos = new Vector2Int(1, 1);

            Vector3 result = GridUtility.GridToWorldPosition(gridPos, cellSize, origin);

            Assert.AreEqual(5f, result.y, 0.0001f, "Y component of gridOrigin must be preserved in world result.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IsInsideSuggestedArea
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void IsInsideSuggestedArea_OriginPos_5x5Area_ReturnsTrue()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(0, 0), 5, 5);

            Assert.IsTrue(result, "(0,0) should be inside a 5x5 suggested area.");
        }

        [Test]
        public void IsInsideSuggestedArea_EdgePos4x4_5x5Area_ReturnsTrue()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(4, 4), 5, 5);

            Assert.IsTrue(result, "(4,4) is the last valid position in a 5x5 area.");
        }

        [Test]
        public void IsInsideSuggestedArea_XEqualsWidth_ReturnsFalse()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(5, 0), 5, 5);

            Assert.IsFalse(result, "(5,0) is out-of-bounds on X in a 5x5 area.");
        }

        [Test]
        public void IsInsideSuggestedArea_YEqualsHeight_ReturnsFalse()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(0, 5), 5, 5);

            Assert.IsFalse(result, "(0,5) is out-of-bounds on Y in a 5x5 area.");
        }

        [Test]
        public void IsInsideSuggestedArea_NegativeX_ReturnsFalse()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(-1, 0), 5, 5);

            Assert.IsFalse(result, "Negative X position must be outside the suggested area.");
        }

        [Test]
        public void IsInsideSuggestedArea_NegativeY_ReturnsFalse()
        {
            bool result = GridUtility.IsInsideSuggestedArea(new Vector2Int(0, -1), 5, 5);

            Assert.IsFalse(result, "Negative Y position must be outside the suggested area.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // IsValidGridPosition (Obsolete — characterization tests to document behavior)
        // ─────────────────────────────────────────────────────────────────────

#pragma warning disable CS0618
        [Test]
        public void IsValidGridPosition_OriginPos_5x5Grid_ReturnsTrue()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(0, 0), 5, 5);

            Assert.IsTrue(result, "(0,0) should be valid in a 5x5 grid.");
        }

        [Test]
        public void IsValidGridPosition_EdgePos4x4_5x5Grid_ReturnsTrue()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(4, 4), 5, 5);

            Assert.IsTrue(result, "(4,4) is the last valid position in a 5x5 grid.");
        }

        [Test]
        public void IsValidGridPosition_XEqualsWidth_ReturnsFalse()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(5, 0), 5, 5);

            Assert.IsFalse(result, "(5,0) is out-of-bounds on X in a 5x5 grid.");
        }

        [Test]
        public void IsValidGridPosition_YEqualsHeight_ReturnsFalse()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(0, 5), 5, 5);

            Assert.IsFalse(result, "(0,5) is out-of-bounds on Y in a 5x5 grid.");
        }

        [Test]
        public void IsValidGridPosition_NegativeX_ReturnsFalse()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(-1, 0), 5, 5);

            Assert.IsFalse(result, "Negative X must be out-of-bounds.");
        }

        [Test]
        public void IsValidGridPosition_NegativeY_ReturnsFalse()
        {
            bool result = GridUtility.IsValidGridPosition(new Vector2Int(0, -1), 5, 5);

            Assert.IsFalse(result, "Negative Y must be out-of-bounds.");
        }
#pragma warning restore CS0618
    }
}
