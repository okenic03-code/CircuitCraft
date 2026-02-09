using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    /// <summary>
    /// Runs SpiceSharp simulations and collects probe data.
    /// Infrastructure component - handles SpiceSharp simulation execution.
    /// </summary>
    public class SimulationRunner
    {
        private readonly NetlistBuilder _netlistBuilder;

        /// <summary>
        /// Creates a new simulation runner with a netlist builder.
        /// </summary>
        public SimulationRunner(NetlistBuilder netlistBuilder = null)
        {
            _netlistBuilder = netlistBuilder ?? new NetlistBuilder();
        }

        /// <summary>
        /// Runs a DC operating point simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <returns>Simulation result with probe measurements.</returns>
        public SimulationResult RunDCOperatingPoint(Circuit circuit, CircuitNetlist netlist)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);

            try
            {
                var op = new OP("op");
                var exports = CreateExports(op, netlist);

                op.ExportSimulationData += (sender, args) =>
                {
                    CollectDCResults(exports, netlist.Probes, result);
                };

                op.Run(circuit);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"DC operating point completed in {result.ElapsedMilliseconds:F1}ms";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(SimulationType.DCOperatingPoint, 
                    SimulationStatus.Error, $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }

        /// <summary>
        /// Runs a transient simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <param name="config">Transient simulation configuration.</param>
        /// <returns>Simulation result with probe measurements over time.</returns>
        public SimulationResult RunTransient(Circuit circuit, CircuitNetlist netlist, TransientConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.Transient, 0);

            try
            {
                var step = config.MaxStep > 0 ? config.MaxStep : config.StopTime / 100;
                var tran = new Transient("tran", step, config.StopTime);
                var exports = CreateExports(tran, netlist);

                // Create temporary storage for time series data
                var probeData = new Dictionary<string, List<double>>();
                var timePoints = new List<double>();
                
                foreach (var probe in netlist.Probes)
                {
                    probeData[probe.Id] = new List<double>();
                }

                tran.ExportSimulationData += (sender, args) =>
                {
                    timePoints.Add(args.Time);
                    CollectTransientPoint(exports, netlist.Probes, probeData);
                };

                tran.Run(circuit);

                // Build final results
                BuildTransientResults(netlist.Probes, probeData, timePoints, result);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"Transient completed in {result.ElapsedMilliseconds:F1}ms ({timePoints.Count} points)";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(SimulationType.Transient, 
                    SimulationStatus.Error, $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }

        /// <summary>
        /// Runs a DC sweep simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <param name="config">DC sweep configuration.</param>
        /// <returns>Simulation result with probe measurements at each sweep point.</returns>
        public SimulationResult RunDCSweep(Circuit circuit, CircuitNetlist netlist, DCSweepConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.DCSweep, 0);

            try
            {
                var dc = new DC("dc", config.SourceId, config.StartValue, config.StopValue, config.StepValue);
                var exports = CreateExports(dc, netlist);

                // Create temporary storage for sweep data
                var probeData = new Dictionary<string, List<double>>();
                var sweepPoints = new List<double>();
                
                foreach (var probe in netlist.Probes)
                {
                    probeData[probe.Id] = new List<double>();
                }

                dc.ExportSimulationData += (sender, args) =>
                {
                    sweepPoints.Add(dc.GetCurrentSweepValue().ToArray()[0]);
                    CollectTransientPoint(exports, netlist.Probes, probeData);
                };

                dc.Run(circuit);

                // Build final results
                foreach (var probe in netlist.Probes)
                {
                    if (!probeData.TryGetValue(probe.Id, out var data)) continue;

                    result.ProbeResults.Add(new ProbeResult
                    {
                        ProbeId = probe.Id,
                        Type = probe.Type,
                        Target = probe.Target,
                        MinValue = data.Count > 0 ? data.Min() : 0,
                        MaxValue = data.Count > 0 ? data.Max() : 0,
                        AverageValue = data.Count > 0 ? data.Average() : 0,
                        Value = data.Count > 0 ? data[data.Count - 1] : 0,
                        TimePoints = new List<double>(sweepPoints),
                        Values = new List<double>(data)
                    });
                }

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"DC sweep completed in {result.ElapsedMilliseconds:F1}ms ({sweepPoints.Count} points)";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(SimulationType.DCSweep, 
                    SimulationStatus.Error, $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }

        /// <summary>
        /// Creates SpiceSharp export objects for each probe.
        /// </summary>
        private Dictionary<string, IExport<double>> CreateExports(ISimulation simulation, CircuitNetlist netlist)
        {
            var exports = new Dictionary<string, IExport<double>>();
            var biasingSimulation = simulation as IBiasingSimulation;
            var eventfulSimulation = simulation as IEventfulSimulation;

            if (biasingSimulation == null)
            {
                throw new InvalidOperationException("Simulation does not support biasing exports.");
            }

            foreach (var probe in netlist.Probes)
            {
                IExport<double> export = null;

                switch (probe.Type)
                {
                    case ProbeType.Voltage:
                        var refNode = probe.ReferenceNode ?? "0";
                        if (refNode == "0" || refNode == netlist.GroundNode)
                        {
                            export = new RealVoltageExport(biasingSimulation, probe.Target);
                        }
                        else
                        {
                            export = new RealVoltageExport(biasingSimulation, probe.Target, refNode);
                        }
                        break;

                    case ProbeType.Current:
                        export = new RealCurrentExport(biasingSimulation, probe.Target);
                        break;

                    case ProbeType.Power:
                        // Power requires separate voltage and current exports
                        // For now, use the property export if available
                        if (eventfulSimulation == null)
                        {
                            throw new InvalidOperationException("Simulation does not support property exports.");
                        }
                        export = new RealPropertyExport(eventfulSimulation, probe.Target, "p");
                        break;
                }

                if (export != null)
                {
                    exports[probe.Id] = export;
                }
            }

            return exports;
        }

        /// <summary>
        /// Collects DC operating point results from exports.
        /// </summary>
        private void CollectDCResults(Dictionary<string, IExport<double>> exports, 
            List<ProbeDefinition> probes, SimulationResult result)
        {
            foreach (var probe in probes)
            {
                if (exports.TryGetValue(probe.Id, out var export))
                {
                    try
                    {
                        var value = export.Value;
                        var probeResult = new ProbeResult(probe.Id, probe.Type, probe.Target, value);
                        result.ProbeResults.Add(probeResult);
                    }
                    catch (Exception)
                    {
                        // Export may fail for some probes (e.g., power on non-supported elements)
                        result.Issues.Add(new SimulationIssue(IssueSeverity.Warning, IssueCategory.General,
                            $"Could not read probe '{probe.Id}' - export failed"));
                    }
                }
            }
        }

        /// <summary>
        /// Collects a single transient data point.
        /// </summary>
        private void CollectTransientPoint(Dictionary<string, IExport<double>> exports,
            List<ProbeDefinition> probes, Dictionary<string, List<double>> probeData)
        {
            foreach (var probe in probes)
            {
                if (exports.TryGetValue(probe.Id, out var export))
                {
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
        }

        /// <summary>
        /// Builds final transient results with statistics.
        /// </summary>
        private void BuildTransientResults(List<ProbeDefinition> probes, 
            Dictionary<string, List<double>> probeData, List<double> timePoints, SimulationResult result)
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
                    TimePoints = new List<double>(timePoints),
                    Values = values
                };

                // Calculate statistics
                double min = double.MaxValue;
                double max = double.MinValue;
                double sum = 0;
                int validCount = 0;

                foreach (var v in values)
                {
                    if (!double.IsNaN(v) && !double.IsInfinity(v))
                    {
                        if (v < min) min = v;
                        if (v > max) max = v;
                        sum += v;
                        validCount++;
                    }
                }

                if (validCount > 0)
                {
                    probeResult.MinValue = min;
                    probeResult.MaxValue = max;
                    probeResult.AverageValue = sum / validCount;
                    probeResult.Value = values[values.Count - 1]; // Last value
                }

                result.ProbeResults.Add(probeResult);
            }
        }

        /// <summary>
        /// Checks for overcurrent and overpower conditions.
        /// </summary>
        /// <param name="netlist">The circuit netlist with element limits.</param>
        /// <param name="result">The simulation result to check and add issues to.</param>
        public void CheckSafetyLimits(CircuitNetlist netlist, SimulationResult result)
        {
            foreach (var element in netlist.Elements)
            {
                // Check overcurrent
                if (element.MaxCurrentAmps.HasValue)
                {
                    var current = result.GetCurrent(element.Id);
                    if (current.HasValue && Math.Abs(current.Value) > element.MaxCurrentAmps.Value)
                    {
                        result.Issues.Add(SimulationIssue.Overcurrent(
                            element.Id, Math.Abs(current.Value), element.MaxCurrentAmps.Value));
                    }
                }

                // Check overpower for resistors
                if (element.Type == ElementType.Resistor && element.MaxPowerWatts.HasValue)
                {
                    var current = result.GetCurrent(element.Id);
                    if (current.HasValue)
                    {
                        var power = current.Value * current.Value * element.Value; // P = I²R
                        if (power > element.MaxPowerWatts.Value)
                        {
                            result.Issues.Add(SimulationIssue.Overpower(
                                element.Id, power, element.MaxPowerWatts.Value));
                        }
                    }
                }
            }

            // Update status if we found errors
            if (result.HasErrors && result.Status == SimulationStatus.Success)
            {
                result.Status = SimulationStatus.CompletedWithWarnings;
            }
        }
    }
}
