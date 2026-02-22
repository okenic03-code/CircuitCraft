using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SpiceSharp;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    public class DCSweepAnalysisStrategy : IAnalysisStrategy
    {
        private readonly DCSweepConfig _config;

        public DCSweepAnalysisStrategy(DCSweepConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public SimulationResult Execute(Circuit circuit, CircuitNetlist netlist, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.DCSweep, 0);

            try
            {
                var dc = new DC("dc", _config.SourceId, _config.StartValue, _config.StopValue, _config.StepValue);
                var exports = AnalysisStrategyUtilities.CreateExports(dc, netlist);

                Dictionary<string, List<double>> probeData = new();
                List<double> sweepPoints = new();
                foreach (var probe in netlist.Probes)
                    probeData[probe.Id] = new();

                dc.ExportSimulationData += (sender, args) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sweepPoints.Add(dc.GetCurrentSweepValue().First());
                    AnalysisStrategyUtilities.CollectSeriesPoint(exports, netlist.Probes, probeData);
                };

                cancellationToken.ThrowIfCancellationRequested();
                dc.Run(circuit);
                cancellationToken.ThrowIfCancellationRequested();

                AnalysisStrategyUtilities.BuildSeriesResults(netlist.Probes, probeData, sweepPoints, result);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"DC sweep completed in {result.ElapsedMilliseconds:F1}ms ({sweepPoints.Count} points)";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(
                    SimulationType.DCSweep,
                    SimulationStatus.Error,
                    $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }
    }
}
