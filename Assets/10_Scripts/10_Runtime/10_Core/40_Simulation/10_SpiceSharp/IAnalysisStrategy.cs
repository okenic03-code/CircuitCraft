using System.Threading;
using SpiceSharp;

namespace CircuitCraft.Simulation.SpiceSharp
{
    public interface IAnalysisStrategy
    {
        SimulationResult Execute(Circuit circuit, CircuitNetlist netlist, CancellationToken cancellationToken);
    }
}
