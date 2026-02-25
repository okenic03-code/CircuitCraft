using System;
using System.Collections.Generic;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    internal static class AnalysisStrategyUtilities
    {
        /// <summary>
        /// Creates SpiceSharp export bindings for all probes in the netlist.
        /// </summary>
        /// <param name="simulation">Simulation instance that owns export objects.</param>
        /// <param name="netlist">Netlist containing probe definitions.</param>
        /// <returns>Map of probe IDs to export handles.</returns>
        public static Dictionary<string, IExport<double>> CreateExports(ISimulation simulation, CircuitNetlist netlist)
        {
            Dictionary<string, IExport<double>> exports = new();
            var biasingSimulation = simulation as IBiasingSimulation;
            var eventfulSimulation = simulation as IEventfulSimulation;

            if (biasingSimulation is null)
                throw new InvalidOperationException("Simulation does not support biasing exports.");

            foreach (var probe in netlist.Probes)
            {
                IExport<double> export = null;
                switch (probe.Type)
                {
                    case ProbeType.Voltage:
                        var refNode = probe.ReferenceNode ?? "0";
                        export = refNode == "0" || refNode == netlist.GroundNode
                            ? new RealVoltageExport(biasingSimulation, probe.Target)
                            : new RealVoltageExport(biasingSimulation, probe.Target, refNode);
                        break;

                    case ProbeType.Current:
                        export = new RealCurrentExport(biasingSimulation, probe.Target);
                        break;

                    case ProbeType.Power:
                        if (eventfulSimulation is null)
                            throw new InvalidOperationException("Simulation does not support property exports.");
                        export = new RealPropertyExport(eventfulSimulation, probe.Target, "p");
                        break;
                }

                if (export is not null)
                    exports[probe.Id] = export;
            }

            return exports;
        }

        /// <summary>
        /// Reads scalar probe exports and appends DC results.
        /// </summary>
        /// <param name="exports">Lookup of export objects keyed by probe ID.</param>
        /// <param name="probes">Probe definitions to evaluate.</param>
        /// <param name="result">Result object receiving probe values and warnings.</param>
        public static void CollectDCResults(
            Dictionary<string, IExport<double>> exports,
            List<ProbeDefinition> probes,
            SimulationResult result)
        {
            foreach (var probe in probes)
            {
                if (!exports.TryGetValue(probe.Id, out var export))
                    continue;

                try
                {
                    var value = export.Value;
                    result.ProbeResults.Add(new ProbeResult(probe.Id, probe.Type, probe.Target, value));
                }
                catch
                {
                    result.Issues.Add(new SimulationIssue(
                        IssueSeverity.Warning,
                        IssueCategory.General,
                        $"Could not read probe '{probe.Id}' - export failed"));
                }
            }
        }

        /// <summary>
        /// Captures one sample point for transient or sweep probe series.
        /// </summary>
        /// <param name="exports">Lookup of export objects keyed by probe ID.</param>
        /// <param name="probes">Probe definitions to evaluate.</param>
        /// <param name="probeData">Per-probe sampled values collection.</param>
        public static void CollectSeriesPoint(
            Dictionary<string, IExport<double>> exports,
            List<ProbeDefinition> probes,
            Dictionary<string, List<double>> probeData)
        {
            foreach (var probe in probes)
            {
                if (!exports.TryGetValue(probe.Id, out var export))
                    continue;

                try
                {
                    probeData[probe.Id].Add(export.Value);
                }
                catch
                {
                    probeData[probe.Id].Add(double.NaN);
                }
            }
        }

        /// <summary>
        /// Builds final probe result objects from sampled series data.
        /// </summary>
        /// <param name="probes">Probe definitions that were sampled.</param>
        /// <param name="probeData">Per-probe sampled value lists.</param>
        /// <param name="xPoints">Shared X-axis points (time or sweep values).</param>
        /// <param name="result">Result object receiving aggregated probe outputs.</param>
        public static void BuildSeriesResults(
            List<ProbeDefinition> probes,
            Dictionary<string, List<double>> probeData,
            List<double> xPoints,
            SimulationResult result)
        {
            foreach (var probe in probes)
            {
                if (!probeData.TryGetValue(probe.Id, out var values) || values.Count == 0)
                    continue;

                var probeResult = new ProbeResult
                {
                    ProbeId = probe.Id,
                    Type = probe.Type,
                    Target = probe.Target,
                    TimePoints = new(xPoints),
                    Values = new(values)
                };

                double min = double.MaxValue;
                double max = double.MinValue;
                double sum = 0;
                int validCount = 0;

                foreach (var value in values)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        continue;

                    if (value < min) min = value;
                    if (value > max) max = value;
                    sum += value;
                    validCount++;
                }

                if (validCount > 0)
                {
                    probeResult.MinValue = min;
                    probeResult.MaxValue = max;
                    probeResult.AverageValue = sum / validCount;
                    probeResult.Value = values[values.Count - 1];
                }

                result.ProbeResults.Add(probeResult);
            }
        }
    }
}
