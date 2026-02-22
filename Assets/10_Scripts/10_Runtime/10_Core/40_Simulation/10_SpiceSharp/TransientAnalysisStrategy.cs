using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SpiceSharp;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    public class TransientAnalysisStrategy : IAnalysisStrategy
    {
        private readonly TransientConfig _config;

        public TransientAnalysisStrategy(TransientConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public SimulationResult Execute(Circuit circuit, CircuitNetlist netlist, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.Transient, 0);

            try
            {
                var step = _config.MaxStep > 0 ? _config.MaxStep : _config.StopTime / 100;
                var tran = new Transient("tran", step, _config.StopTime);
                var exports = AnalysisStrategyUtilities.CreateExports(tran, netlist);

                Dictionary<string, List<double>> probeData = new();
                List<double> timePoints = new();
                foreach (var probe in netlist.Probes)
                    probeData[probe.Id] = new();

                tran.ExportSimulationData += (sender, args) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    timePoints.Add(args.Time);
                    AnalysisStrategyUtilities.CollectSeriesPoint(exports, netlist.Probes, probeData);
                };

                cancellationToken.ThrowIfCancellationRequested();
                tran.Run(circuit);
                cancellationToken.ThrowIfCancellationRequested();

                AnalysisStrategyUtilities.BuildSeriesResults(netlist.Probes, probeData, timePoints, result);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"Transient completed in {result.ElapsedMilliseconds:F1}ms ({timePoints.Count} points)";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(
                    SimulationType.Transient,
                    SimulationStatus.Error,
                    $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }
    }
}
