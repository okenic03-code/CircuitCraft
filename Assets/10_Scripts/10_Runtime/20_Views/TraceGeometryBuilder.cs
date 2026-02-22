using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using UnityEngine;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Provides pure geometry and color calculations for trace rendering.
    /// </summary>
    public static class TraceGeometryBuilder
    {
        /// <summary>
        /// Normalizes a voltage value into a 0..1 range.
        /// </summary>
        /// <param name="voltage">The voltage value to normalize.</param>
        /// <param name="min">The minimum voltage bound.</param>
        /// <param name="max">The maximum voltage bound.</param>
        /// <returns>A clamped normalized value in the range 0..1.</returns>
        public static float NormalizeVoltage(float voltage, float min, float max)
        {
            float range = max - min;
            if (range <= float.Epsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01((voltage - min) / range);
        }

        /// <summary>
        /// Generates flow-texture pixels using the sawtooth profile used for animated current overlays.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <returns>Pixel array in row-major order (y * width + x).</returns>
        public static Color[] GenerateFlowTexturePixels(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
            }

            var pixels = new Color[width * height];
            float halfHeight = (height - 1f) * 0.5f;

            for (int x = 0; x < width; x++)
            {
                float cycle = (x / (float)width) * 4f;
                float saw = Mathf.Abs((cycle % 2f) - 1f);
                float alpha = Mathf.Max(0f, 1f - (saw * 2f));

                for (int y = 0; y < height; y++)
                {
                    float vertical = halfHeight <= float.Epsilon
                        ? 1f
                        : Mathf.Clamp01(1f - (Mathf.Abs(y - halfHeight) / halfHeight));

                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha * vertical);
                }
            }

            return pixels;
        }

        /// <summary>
        /// Computes per-segment voltage colors from traces, net lookup, and node voltages.
        /// </summary>
        /// <param name="traces">Trace segments to evaluate.</param>
        /// <param name="getNet">Net resolver by net id.</param>
        /// <param name="nodeVoltages">Node voltage map keyed by net name.</param>
        /// <param name="minColor">Color used for the minimum voltage.</param>
        /// <param name="maxColor">Color used for the maximum voltage.</param>
        /// <param name="defaultColor">Fallback color when voltage data is unavailable.</param>
        /// <returns>Map of segment id to computed color.</returns>
        public static Dictionary<int, Color> ComputeVoltageColors(
            IReadOnlyList<TraceSegment> traces,
            Func<int, Net> getNet,
            Dictionary<string, double> nodeVoltages,
            Color minColor,
            Color maxColor,
            Color defaultColor)
        {
            float minVoltage = 0f;
            float maxVoltage = 0f;
            if (nodeVoltages is not null && nodeVoltages.Count > 0)
            {
                minVoltage = float.MaxValue;
                maxVoltage = float.MinValue;

                foreach (var voltage in nodeVoltages.Values)
                {
                    float value = (float)voltage;
                    minVoltage = Mathf.Min(minVoltage, value);
                    maxVoltage = Mathf.Max(maxVoltage, value);
                }
            }

            return ComputeVoltageColors(
                traces,
                getNet,
                nodeVoltages,
                minVoltage,
                maxVoltage,
                minColor,
                maxColor,
                defaultColor);
        }

        /// <summary>
        /// Computes per-segment voltage colors from traces, net lookup, and node voltages using explicit normalization bounds.
        /// </summary>
        /// <param name="traces">Trace segments to evaluate.</param>
        /// <param name="getNet">Net resolver by net id.</param>
        /// <param name="nodeVoltages">Node voltage map keyed by net name.</param>
        /// <param name="minVoltage">Minimum voltage bound used for normalization.</param>
        /// <param name="maxVoltage">Maximum voltage bound used for normalization.</param>
        /// <param name="minColor">Color used for the minimum voltage.</param>
        /// <param name="maxColor">Color used for the maximum voltage.</param>
        /// <param name="defaultColor">Fallback color when voltage data is unavailable.</param>
        /// <returns>Map of segment id to computed color.</returns>
        public static Dictionary<int, Color> ComputeVoltageColors(
            IReadOnlyList<TraceSegment> traces,
            Func<int, Net> getNet,
            Dictionary<string, double> nodeVoltages,
            float minVoltage,
            float maxVoltage,
            Color minColor,
            Color maxColor,
            Color defaultColor)
        {
            var colorsBySegment = new Dictionary<int, Color>();

            if (traces is null || getNet is null)
            {
                return colorsBySegment;
            }

            foreach (var trace in traces)
            {
                if (trace is null)
                {
                    continue;
                }

                var color = defaultColor;
                if (nodeVoltages is not null && nodeVoltages.Count > 0)
                {
                    var net = getNet(trace.NetId);
                    if (net is not null
                        && !string.IsNullOrWhiteSpace(net.NetName)
                        && nodeVoltages.TryGetValue(net.NetName, out var voltage))
                    {
                        float normalized = NormalizeVoltage((float)voltage, minVoltage, maxVoltage);
                        color = Color.Lerp(minColor, maxColor, normalized);
                    }
                }

                colorsBySegment[trace.SegmentId] = color;
            }

            return colorsBySegment;
        }

        /// <summary>
        /// Calculates the next wrapped flow texture offset for one segment.
        /// </summary>
        /// <param name="currentOffset">Current offset in 0..1 range (or any float that will be wrapped).</param>
        /// <param name="current">Segment current in amps.</param>
        /// <param name="baseSpeed">Base animation speed.</param>
        /// <param name="speedScale">Current-to-speed scale factor.</param>
        /// <param name="maxSpeed">Maximum animation speed cap.</param>
        /// <param name="deltaTime">Frame delta time.</param>
        /// <returns>Wrapped offset in 0..1 range.</returns>
        public static float CalculateFlowOffset(
            float currentOffset,
            float current,
            float baseSpeed,
            float speedScale,
            float maxSpeed,
            float deltaTime)
        {
            float direction = Mathf.Sign(current);
            float speed = Mathf.Min((Mathf.Abs(current) * speedScale) + baseSpeed, maxSpeed);
            float offset = currentOffset + (direction * speed * deltaTime);
            return Mathf.Repeat(offset, 1f);
        }
    }
}
