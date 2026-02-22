using System;
using System.Collections.Generic;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    internal static class AnalysisStrategyUtilities
    {
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
