using System;
using System.Collections.Generic;

namespace CircuitCraft.Simulation
{
    /// <summary>
    /// Result of a single probe measurement.
    /// </summary>
    [Serializable]
    public class ProbeResult
    {
        /// <summary>Probe identifier matching the request.</summary>
        public string ProbeId { get; set; }

        /// <summary>Type of measurement.</summary>
        public ProbeType Type { get; set; }

        /// <summary>Target element or node.</summary>
        public string Target { get; set; }

        /// <summary>
        /// Final/steady-state value (for DC operating point).
        /// For transient: the last value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Time series data for transient simulations.
        /// Empty for DC operating point.
        /// </summary>
        public List<double> TimePoints { get; set; } = new List<double>();

        /// <summary>
        /// Value series data for transient simulations.
        /// Empty for DC operating point.
        /// </summary>
        public List<double> Values { get; set; } = new List<double>();

        /// <summary>Minimum value observed (for transient).</summary>
        public double MinValue { get; set; }

        /// <summary>Maximum value observed (for transient).</summary>
        public double MaxValue { get; set; }

        /// <summary>Average/RMS value (for transient).</summary>
        public double AverageValue { get; set; }

        public ProbeResult() { }

        public ProbeResult(string probeId, ProbeType type, string target, double value)
        {
            ProbeId = probeId;
            Type = type;
            Target = target;
            Value = value;
            MinValue = value;
            MaxValue = value;
            AverageValue = value;
        }

        /// <summary>
        /// Gets a formatted string of the result with units.
        /// </summary>
        public string GetFormattedValue()
        {
            var unit = Type switch
            {
                ProbeType.Voltage => "V",
                ProbeType.Current => "A",
                ProbeType.Power => "W",
                _ => ""
            };

            return FormatEngineering(Value, unit);
        }

        private static string FormatEngineering(double value, string unit)
        {
            var absValue = Math.Abs(value);
            if (absValue >= 1e6) return $"{value / 1e6:F3} M{unit}";
            if (absValue >= 1e3) return $"{value / 1e3:F3} k{unit}";
            if (absValue >= 1) return $"{value:F3} {unit}";
            if (absValue >= 1e-3) return $"{value * 1e3:F3} m{unit}";
            if (absValue >= 1e-6) return $"{value * 1e6:F3} µ{unit}";
            if (absValue >= 1e-9) return $"{value * 1e9:F3} n{unit}";
            return $"{value * 1e12:F3} p{unit}";
        }
    }

    /// <summary>
    /// Overall status of a simulation run.
    /// </summary>
    public enum SimulationStatus
    {
        /// <summary>Simulation completed successfully.</summary>
        Success,
        
        /// <summary>Simulation completed but with warnings.</summary>
        CompletedWithWarnings,
        
        /// <summary>Simulation failed to converge.</summary>
        ConvergenceFailure,
        
        /// <summary>Simulation timed out.</summary>
        Timeout,
        
        /// <summary>Invalid circuit or configuration.</summary>
        InvalidCircuit,
        
        /// <summary>Unknown error occurred.</summary>
        Error
    }

    /// <summary>
    /// Contains the results of a circuit simulation.
    /// Domain DTO - no SpiceSharp or Unity dependencies.
    /// </summary>
    [Serializable]
    public class SimulationResult
    {
        /// <summary>
        /// Whether the simulation ran at all (even if with errors).
        /// </summary>
        public bool Ran { get; set; }

        /// <summary>
        /// Overall status of the simulation.
        /// </summary>
        public SimulationStatus Status { get; set; }

        /// <summary>
        /// Human-readable status message.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Type of simulation that was run.
        /// </summary>
        public SimulationType SimulationType { get; set; }

        /// <summary>
        /// Time taken to run the simulation in milliseconds.
        /// </summary>
        public double ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Probe measurement results.
        /// </summary>
        public List<ProbeResult> ProbeResults { get; set; } = new List<ProbeResult>();

        /// <summary>
        /// Issues, warnings, and errors encountered.
        /// </summary>
        public List<SimulationIssue> Issues { get; set; } = new List<SimulationIssue>();

        /// <summary>
        /// Optional tag from the request.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets whether the simulation was successful (ran without errors).
        /// </summary>
        public bool IsSuccess => Status == SimulationStatus.Success || Status == SimulationStatus.CompletedWithWarnings;

        /// <summary>
        /// Gets whether there are any error-level issues.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                foreach (var issue in Issues)
                {
                    if (issue.Severity == IssueSeverity.Error) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a probe result by ID.
        /// </summary>
        public ProbeResult GetProbe(string probeId)
        {
            foreach (var probe in ProbeResults)
            {
                if (probe.ProbeId == probeId) return probe;
            }
            return null;
        }

        /// <summary>
        /// Gets voltage probe result by target node.
        /// </summary>
        public double? GetVoltage(string node)
        {
            foreach (var probe in ProbeResults)
            {
                if (probe.Type == ProbeType.Voltage && probe.Target == node)
                    return probe.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets current probe result by target element.
        /// </summary>
        public double? GetCurrent(string elementId)
        {
            foreach (var probe in ProbeResults)
            {
                if (probe.Type == ProbeType.Current && probe.Target == elementId)
                    return probe.Value;
            }
            return null;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static SimulationResult Success(SimulationType simType, double elapsedMs)
        {
            return new SimulationResult
            {
                Ran = true,
                Status = SimulationStatus.Success,
                StatusMessage = "Simulation completed successfully",
                SimulationType = simType,
                ElapsedMilliseconds = elapsedMs
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static SimulationResult Failure(SimulationType simType, SimulationStatus status, string message)
        {
            return new SimulationResult
            {
                Ran = false,
                Status = status,
                StatusMessage = message,
                SimulationType = simType
            };
        }
    }
}
