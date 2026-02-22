using System.Threading;
using SpiceSharp;

namespace CircuitCraft.Simulation.SpiceSharp
{
    /// <summary>
    /// Executes a specific simulation analysis strategy against a built circuit.
    /// </summary>
    public interface IAnalysisStrategy
    {
        /// <summary>
        /// Runs the analysis and returns a mapped simulation result.
        /// </summary>
        /// <param name="circuit">SpiceSharp circuit instance to simulate.</param>
        /// <param name="netlist">Source netlist metadata and probes.</param>
        /// <param name="cancellationToken">Cancellation token for cooperative abort.</param>
        /// <returns>Simulation result containing probe values and issues.</returns>
        SimulationResult Execute(Circuit circuit, CircuitNetlist netlist, CancellationToken cancellationToken);
    }
}
