using UnityEngine;
using CircuitCraft.Simulation;

namespace CircuitCraft.Data.Events
{
    [CreateAssetMenu(fileName = "SimulationEventChannel", menuName = "CircuitCraft/Events/Simulation Event Channel")]
    public class SimulationEventChannel : EventChannel<SimulationResult>
    {
    }
}
