using System;
using System.Diagnostics;
using System.Threading;
using SpiceSharp;
using SpiceSharp.Simulations;

namespace CircuitCraft.Simulation.SpiceSharp
{
    public class DCOperatingPointStrategy : IAnalysisStrategy
    {
        public SimulationResult Execute(Circuit circuit, CircuitNetlist netlist, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);

            try
            {
                var op = new OP("op");
                var exports = AnalysisStrategyUtilities.CreateExports(op, netlist);

                op.ExportSimulationData += (sender, args) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AnalysisStrategyUtilities.CollectDCResults(exports, netlist.Probes, result);
                };

                cancellationToken.ThrowIfCancellationRequested();
                op.Run(circuit);

                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.StatusMessage = $"DC operating point completed in {result.ElapsedMilliseconds:F1}ms";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.Error,
                    $"Simulation error: {ex.Message}");
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Issues.Add(SimulationIssue.ConvergenceFailure(ex.Message));
            }

            return result;
        }
    }
}
