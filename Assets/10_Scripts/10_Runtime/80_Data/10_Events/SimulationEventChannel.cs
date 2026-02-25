using UnityEngine;
using CircuitCraft.Simulation;

namespace CircuitCraft.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for simulation result payloads.
    /// </summary>
    [CreateAssetMenu(fileName = "SimulationEventChannel", menuName = "CircuitCraft/Events/Simulation Event Channel")]
    public class SimulationEventChannel : EventChannel<SimulationResult>
    {
    }
}
