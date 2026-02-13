using System.Threading;
using Cysharp.Threading.Tasks;

namespace CircuitCraft.Simulation
{
    /// <summary>
    /// Service interface for running circuit simulations.
    /// Domain interface - implementations are in the infrastructure layer.
    /// </summary>
    public interface ISimulationService
    {
        /// <summary>
        /// Runs a circuit simulation asynchronously.
        /// Safe to call from Unity main thread.
        /// </summary>
        /// <param name="request">The simulation request containing circuit and configuration.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The simulation result with measurements and issues.</returns>
        UniTask<SimulationResult> RunAsync(SimulationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a circuit netlist without running a full simulation.
        /// Checks for topology errors, floating nodes, invalid parameters, etc.
        /// </summary>
        /// <param name="netlist">The circuit netlist to validate.</param>
        /// <returns>Result containing any validation issues found.</returns>
        SimulationResult Validate(CircuitNetlist netlist);
    }
}
