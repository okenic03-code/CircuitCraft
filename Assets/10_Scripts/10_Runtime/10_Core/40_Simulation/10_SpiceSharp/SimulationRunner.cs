using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpiceSharp;

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
            _netlistBuilder = netlistBuilder ?? new();
        }

        /// <summary>
        /// Runs a DC operating point simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Simulation result with probe measurements.</returns>
        public async UniTask<SimulationResult> RunDCOperatingPointAsync(
            Circuit circuit,
            CircuitNetlist netlist,
            CancellationToken cancellationToken = default)
        {
            var strategy = new DCOperatingPointStrategy();
            return await UniTask.RunOnThreadPool(
                () => strategy.Execute(circuit, netlist, cancellationToken),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Runs a transient simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <param name="config">Transient simulation configuration.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Simulation result with probe measurements over time.</returns>
        public async UniTask<SimulationResult> RunTransientAsync(
            Circuit circuit,
            CircuitNetlist netlist,
            TransientConfig config,
            CancellationToken cancellationToken = default)
        {
            var strategy = new TransientAnalysisStrategy(config);
            return await UniTask.RunOnThreadPool(
                () => strategy.Execute(circuit, netlist, cancellationToken),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Runs a DC sweep simulation.
        /// </summary>
        /// <param name="circuit">The SpiceSharp circuit to simulate.</param>
        /// <param name="netlist">The domain netlist (for probe definitions).</param>
        /// <param name="config">DC sweep configuration.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Simulation result with probe measurements at each sweep point.</returns>
        public async UniTask<SimulationResult> RunDCSweepAsync(
            Circuit circuit,
            CircuitNetlist netlist,
            DCSweepConfig config,
            CancellationToken cancellationToken = default)
        {
            var strategy = new DCSweepAnalysisStrategy(config);
            return await UniTask.RunOnThreadPool(
                () => strategy.Execute(circuit, netlist, cancellationToken),
                cancellationToken: cancellationToken);
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
