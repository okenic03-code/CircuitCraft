using System;

namespace CircuitCraft.Simulation
{
    /// <summary>
    /// Severity level for simulation issues.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>Informational message, does not affect simulation validity.</summary>
        Info,
        
        /// <summary>Warning that may indicate a potential problem.</summary>
        Warning,
        
        /// <summary>Error that affects simulation results or indicates failure.</summary>
        Error
    }

    /// <summary>
    /// Category of simulation issue for filtering and handling.
    /// </summary>
    public enum IssueCategory
    {
        /// <summary>General simulation issue.</summary>
        General,
        
        /// <summary>Simulation convergence failure.</summary>
        Convergence,
        
        /// <summary>Component operating outside safe limits.</summary>
        Overcurrent,
        
        /// <summary>Component power dissipation exceeds rating.</summary>
        Overpower,
        
        /// <summary>Voltage exceeds component rating.</summary>
        Overvoltage,
        
        /// <summary>Invalid circuit topology (floating nodes, shorts).</summary>
        Topology,
        
        /// <summary>Component parameter out of valid range.</summary>
        Parameter
    }

    /// <summary>
    /// Represents an issue, warning, or error that occurred during simulation.
    /// Domain DTO - no SpiceSharp or Unity dependencies.
    /// </summary>
    [Serializable]
    public class SimulationIssue
    {
        /// <summary>
        /// Severity level of the issue.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Category of the issue for filtering and handling.
        /// </summary>
        public IssueCategory Category { get; set; }

        /// <summary>
        /// Human-readable description of the issue.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional element identifier (component or trace) associated with the issue.
        /// </summary>
        public string ElementId { get; set; }

        /// <summary>
        /// Optional measured value that caused the issue (e.g., current in amps).
        /// </summary>
        public double? MeasuredValue { get; set; }

        /// <summary>
        /// Optional limit value that was exceeded.
        /// </summary>
        public double? LimitValue { get; set; }

        /// <summary>
        /// Creates a new simulation issue.
        /// </summary>
        public SimulationIssue() { }

        /// <summary>
        /// Creates a new simulation issue with the specified parameters.
        /// </summary>
        public SimulationIssue(IssueSeverity severity, IssueCategory category, string message, string elementId = null)
        {
            Severity = severity;
            Category = category;
            Message = message;
            ElementId = elementId;
        }

        /// <summary>
        /// Creates an overcurrent issue for a specific element.
        /// </summary>
        public static SimulationIssue Overcurrent(string elementId, double measuredAmps, double limitAmps)
        {
            return new SimulationIssue
            {
                Severity = IssueSeverity.Error,
                Category = IssueCategory.Overcurrent,
                Message = $"Overcurrent on {elementId}: {measuredAmps:F3}A exceeds limit of {limitAmps:F3}A",
                ElementId = elementId,
                MeasuredValue = measuredAmps,
                LimitValue = limitAmps
            };
        }

        /// <summary>
        /// Creates an overpower issue for a specific element.
        /// </summary>
        public static SimulationIssue Overpower(string elementId, double measuredWatts, double limitWatts)
        {
            return new SimulationIssue
            {
                Severity = IssueSeverity.Error,
                Category = IssueCategory.Overpower,
                Message = $"Overpower on {elementId}: {measuredWatts:F3}W exceeds limit of {limitWatts:F3}W",
                ElementId = elementId,
                MeasuredValue = measuredWatts,
                LimitValue = limitWatts
            };
        }

        /// <summary>
        /// Creates a convergence failure issue.
        /// </summary>
        public static SimulationIssue ConvergenceFailure(string details = null)
        {
            return new SimulationIssue
            {
                Severity = IssueSeverity.Error,
                Category = IssueCategory.Convergence,
                Message = string.IsNullOrEmpty(details) 
                    ? "Simulation failed to converge" 
                    : $"Simulation failed to converge: {details}"
            };
        }

        public override string ToString()
        {
            var prefix = Severity == IssueSeverity.Error ? "[ERROR]" 
                : Severity == IssueSeverity.Warning ? "[WARN]" 
                : "[INFO]";
            return $"{prefix} {Message}";
        }
    }
}
