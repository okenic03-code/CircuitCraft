using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Views;
using NUnit.Framework;
using UnityEngine;

namespace CircuitCraft.Tests.Views
{
    [TestFixture]
    public class TraceGeometryBuilderTests
    {
        [TestCase(0f, 0f, 10f, 0f)]
        [TestCase(5f, 0f, 10f, 0.5f)]
        [TestCase(10f, 0f, 10f, 1f)]
        [TestCase(-5f, 0f, 10f, 0f)]
        [TestCase(15f, 0f, 10f, 1f)]
        public void NormalizeVoltage_NormalRangeAndClamping_ReturnsExpected(float voltage, float min, float max, float expected)
        {
            var normalized = TraceGeometryBuilder.NormalizeVoltage(voltage, min, max);

            Assert.AreEqual(expected, normalized, 0.000001f);
        }

        [Test]
        public void NormalizeVoltage_ZeroRange_ReturnsZero()
        {
            var normalized = TraceGeometryBuilder.NormalizeVoltage(4f, 4f, 4f);

            Assert.AreEqual(0f, normalized, 0.000001f);
        }

        [Test]
        public void NormalizeVoltage_EpsilonRange_ReturnsClampedValue()
        {
            float min = 2f;
            float max = min + (float.Epsilon * 2f);

            var normalized = TraceGeometryBuilder.NormalizeVoltage(min + float.Epsilon, min, max);

            Assert.AreEqual(0.5f, normalized, 0.0001f);
        }

        [Test]
        public void GenerateFlowTexturePixels_WidthTimesHeight_ReturnsExpectedLength()
        {
            const int width = 64;
            const int height = 8;
            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(width, height);

            Assert.AreEqual(width * height, pixels.Length);
        }

        [Test]
        public void GenerateFlowTexturePixels_EdgeRows_HaveZeroAlpha()
        {
            const int width = 64;
            const int height = 8;
            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(width, height);

            var top = GetPixel(pixels, width, 16, 0);
            var bottom = GetPixel(pixels, width, 16, height - 1);

            Assert.AreEqual(0f, top.a, 0.000001f);
            Assert.AreEqual(0f, bottom.a, 0.000001f);
        }

        [Test]
        public void GenerateFlowTexturePixels_PeakColumn_CenterRowHasExpectedAlpha()
        {
            const int width = 64;
            const int height = 9;
            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(width, height);

            var center = GetPixel(pixels, width, 16, 4);

            Assert.AreEqual(1f, center.a, 0.000001f);
            Assert.AreEqual(1f, center.r, 0.000001f);
            Assert.AreEqual(1f, center.g, 0.000001f);
            Assert.AreEqual(1f, center.b, 0.000001f);
        }

        [Test]
        public void GenerateFlowTexturePixels_ValleyColumn_CenterRowHasZeroAlpha()
        {
            const int width = 64;
            const int height = 9;
            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(width, height);

            var center = GetPixel(pixels, width, 0, 4);

            Assert.AreEqual(0f, center.a, 0.000001f);
        }

        [Test]
        public void GenerateFlowTexturePixels_PeaksRepeatAcrossTexture()
        {
            const int width = 64;
            const int height = 9;
            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(width, height);

            var firstPeak = GetPixel(pixels, width, 16, 4);
            var secondPeak = GetPixel(pixels, width, 48, 4);

            Assert.AreEqual(firstPeak.a, secondPeak.a, 0.000001f);
        }

        [Test]
        public void ComputeVoltageColors_EmptyTraces_ReturnsEmptyMap()
        {
            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                Array.Empty<TraceSegment>(),
                _ => null,
                new Dictionary<string, double>(),
                Color.blue,
                Color.yellow,
                Color.white);

            Assert.AreEqual(0, colors.Count);
        }

        [Test]
        public void ComputeVoltageColors_NullVoltages_UsesDefaultColor()
        {
            var traces = new[]
            {
                new TraceSegment(1, 1, new GridPosition(0, 0), new GridPosition(0, 1))
            };

            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                traces,
                _ => new Net(1, "N1"),
                null,
                Color.blue,
                Color.yellow,
                Color.cyan);

            AssertColorEqual(Color.cyan, colors[1]);
        }

        [Test]
        public void ComputeVoltageColors_MissingNet_UsesDefaultColor()
        {
            var traces = new[]
            {
                new TraceSegment(1, 1, new GridPosition(0, 0), new GridPosition(0, 1))
            };

            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                traces,
                _ => null,
                new Dictionary<string, double> { ["N1"] = 5.0 },
                Color.blue,
                Color.yellow,
                Color.white);

            AssertColorEqual(Color.white, colors[1]);
        }

        [Test]
        public void ComputeVoltageColors_MissingNodeVoltage_UsesDefaultColor()
        {
            var traces = new[]
            {
                new TraceSegment(1, 1, new GridPosition(0, 0), new GridPosition(0, 1))
            };

            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                traces,
                _ => new Net(1, "N1"),
                new Dictionary<string, double> { ["N2"] = 5.0 },
                Color.blue,
                Color.yellow,
                Color.magenta);

            AssertColorEqual(Color.magenta, colors[1]);
        }

        [Test]
        public void ComputeVoltageColors_ValidVoltages_MapsMinAndMaxColors()
        {
            var traces = new[]
            {
                new TraceSegment(1, 1, new GridPosition(0, 0), new GridPosition(0, 1)),
                new TraceSegment(2, 2, new GridPosition(1, 0), new GridPosition(1, 1))
            };
            var netMap = new Dictionary<int, Net>
            {
                [1] = new Net(1, "LOW"),
                [2] = new Net(2, "HIGH")
            };
            var nodeVoltages = new Dictionary<string, double>
            {
                ["LOW"] = 0.0,
                ["HIGH"] = 10.0
            };

            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                traces,
                netId => netMap[netId],
                nodeVoltages,
                Color.blue,
                Color.yellow,
                Color.white);

            AssertColorEqual(Color.blue, colors[1]);
            AssertColorEqual(Color.yellow, colors[2]);
        }

        [Test]
        public void ComputeVoltageColors_ValidMidVoltage_MapsLerpedColor()
        {
            var traces = new[]
            {
                new TraceSegment(1, 1, new GridPosition(0, 0), new GridPosition(0, 1))
            };

            var colors = TraceGeometryBuilder.ComputeVoltageColors(
                traces,
                _ => new Net(1, "MID"),
                new Dictionary<string, double>
                {
                    ["LOW"] = 0.0,
                    ["MID"] = 5.0,
                    ["HIGH"] = 10.0
                },
                Color.blue,
                Color.yellow,
                Color.white);

            var expected = Color.Lerp(Color.blue, Color.yellow, 0.5f);
            AssertColorEqual(expected, colors[1]);
        }

        [Test]
        public void CalculateFlowOffset_ZeroCurrent_OnlyWrapsCurrentOffset()
        {
            var offset = TraceGeometryBuilder.CalculateFlowOffset(
                currentOffset: 1.2f,
                current: 0f,
                baseSpeed: 0.2f,
                speedScale: 2f,
                maxSpeed: 2.2f,
                deltaTime: 1f);

            Assert.AreEqual(0.2f, offset, 0.000001f);
        }

        [Test]
        public void CalculateFlowOffset_PositiveCurrent_IncreasesOffset()
        {
            var offset = TraceGeometryBuilder.CalculateFlowOffset(
                currentOffset: 0.1f,
                current: 0.5f,
                baseSpeed: 0.2f,
                speedScale: 2f,
                maxSpeed: 2.2f,
                deltaTime: 1f);

            Assert.AreEqual(0.3f, offset, 0.000001f);
        }

        [Test]
        public void CalculateFlowOffset_NegativeCurrent_DecreasesAndWrapsOffset()
        {
            var offset = TraceGeometryBuilder.CalculateFlowOffset(
                currentOffset: 0.1f,
                current: -0.5f,
                baseSpeed: 0.2f,
                speedScale: 2f,
                maxSpeed: 2.2f,
                deltaTime: 1f);

            Assert.AreEqual(0.9f, offset, 0.000001f);
        }

        [Test]
        public void CalculateFlowOffset_HighCurrent_ClampsSpeedToMax()
        {
            var offset = TraceGeometryBuilder.CalculateFlowOffset(
                currentOffset: 0f,
                current: 100f,
                baseSpeed: 0.2f,
                speedScale: 2f,
                maxSpeed: 1.5f,
                deltaTime: 1f);

            Assert.AreEqual(0.5f, offset, 0.000001f);
        }

        [Test]
        public void CalculateFlowOffset_LargeDeltaTime_WrapsUsingRepeat()
        {
            var offset = TraceGeometryBuilder.CalculateFlowOffset(
                currentOffset: 0.3f,
                current: 1f,
                baseSpeed: 0.2f,
                speedScale: 2f,
                maxSpeed: 2.2f,
                deltaTime: 2f);

            Assert.AreEqual(0.7f, offset, 0.000001f);
        }

        private static Color GetPixel(IReadOnlyList<Color> pixels, int width, int x, int y)
        {
            return pixels[(y * width) + x];
        }

        private static void AssertColorEqual(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.000001f);
            Assert.AreEqual(expected.g, actual.g, 0.000001f);
            Assert.AreEqual(expected.b, actual.b, 0.000001f);
            Assert.AreEqual(expected.a, actual.a, 0.000001f);
        }
    }
}
