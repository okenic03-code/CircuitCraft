using System;

namespace CircuitCraft.Simulation
{
    /// <summary>
    /// Type of simulation analysis to perform.
    /// </summary>
    public enum SimulationType
    {
        /// <summary>DC operating point analysis - finds steady-state voltages and currents.</summary>
        DCOperatingPoint,
        
        /// <summary>Transient analysis - time-domain simulation.</summary>
        Transient,
        
        /// <summary>AC small-signal analysis - frequency-domain response.</summary>
        AC,
        
        /// <summary>DC sweep - varies a parameter and measures response.</summary>
        DCSweep
    }

    /// <summary>
    /// Configuration for transient analysis.
    /// </summary>
    [Serializable]
    public class TransientConfig
    {
        /// <summary>Simulation stop time in seconds.</summary>
        public double StopTime { get; set; } = 1e-3;

        /// <summary>Maximum time step in seconds (0 for automatic).</summary>
        public double MaxStep { get; set; } = 0;

        /// <summary>Initial time step in seconds (0 for automatic).</summary>
        public double InitialStep { get; set; } = 0;

        /// <summary>Use initial conditions from DC operating point.</summary>
        public bool IsUsingInitialConditions { get; set; } = true;

        public TransientConfig() { }

        public TransientConfig(double stopTime, double maxStep = 0)
        {
            StopTime = stopTime;
            MaxStep = maxStep;
        }
    }

    /// <summary>
    /// Configuration for DC sweep analysis.
    /// </summary>
    [Serializable]
    public class DCSweepConfig
    {
        /// <summary>Source element ID to sweep.</summary>
        public string SourceId { get; set; }

        /// <summary>Start value for sweep.</summary>
        public double StartValue { get; set; }

        /// <summary>Stop value for sweep.</summary>
        public double StopValue { get; set; }

        /// <summary>Step increment for sweep.</summary>
        public double StepValue { get; set; }

        public DCSweepConfig() { }

        public DCSweepConfig(string sourceId, double start, double stop, double step)
        {
            SourceId = sourceId;
            StartValue = start;
            StopValue = stop;
            StepValue = step;
        }
    }

    /// <summary>
    /// Request for running a circuit simulation.
    /// Domain DTO - no SpiceSharp or Unity dependencies.
    /// </summary>
    [Serializable]
    public class SimulationRequest
    {
        /// <summary>
        /// The circuit to simulate.
        /// </summary>
        public CircuitNetlist Netlist { get; set; }

        /// <summary>
        /// Type of simulation analysis to perform.
        /// </summary>
        public SimulationType SimulationType { get; set; } = SimulationType.DCOperatingPoint;

        /// <summary>
        /// Configuration for transient analysis (when SimulationType is Transient).
        /// </summary>
        public TransientConfig TransientConfig { get; set; }

        /// <summary>
        /// Configuration for DC sweep analysis (when SimulationType is DCSweep).
        /// </summary>
        public DCSweepConfig DCSweepConfig { get; set; }

        /// <summary>
        /// Maximum simulation time in seconds before timeout (0 for no limit).
        /// </summary>
        public double TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to check for overcurrent/overpower conditions.
        /// </summary>
        public bool IsSafetyChecksEnabled { get; set; } = true;

        /// <summary>
        /// Optional tag/identifier for this simulation request.
        /// </summary>
        public string Tag { get; set; }

        public SimulationRequest() { }

        /// <summary>
        /// Creates a DC operating point simulation request.
        /// </summary>
        public static SimulationRequest DCOperatingPoint(CircuitNetlist netlist)
        {
            return new SimulationRequest
            {
                Netlist = netlist,
                SimulationType = SimulationType.DCOperatingPoint
            };
        }

        /// <summary>
        /// Creates a transient simulation request.
        /// </summary>
        public static SimulationRequest Transient(CircuitNetlist netlist, double stopTime, double maxStep = 0)
        {
            return new SimulationRequest
            {
                Netlist = netlist,
                SimulationType = SimulationType.Transient,
                TransientConfig = new TransientConfig(stopTime, maxStep)
            };
        }
    }
}
