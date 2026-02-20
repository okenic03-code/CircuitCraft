using System.Collections.Generic;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Types of design rule violations.
    /// </summary>
    public enum DRCViolationType
    {
        /// <summary>Two different nets overlap at the same grid position.</summary>
        Short,

        /// <summary>A pin is not connected to any net.</summary>
        UnconnectedPin
    }

    /// <summary>
    /// A single design rule violation with type, location, and description.
    /// </summary>
    public class DRCViolationItem
    {
        /// <summary>Gets the type of violation.</summary>
        public DRCViolationType ViolationType { get; }

        /// <summary>Gets the grid position where the violation occurs.</summary>
        public GridPosition Location { get; }

        /// <summary>Gets a human-readable description of the violation.</summary>
        public string Message { get; }

        /// <summary>
        /// Creates a new DRC violation item.
        /// </summary>
        /// <param name="violationType">Type of violation.</param>
        /// <param name="location">Grid position of the violation.</param>
        /// <param name="message">Description of the violation.</param>
        public DRCViolationItem(DRCViolationType violationType, GridPosition location, string message)
        {
            ViolationType = violationType;
            Location = location;
            Message = message;
        }

        /// <summary>
        /// Returns a string representation of this violation.
        /// </summary>
        public override string ToString()
        {
            return $"[{ViolationType}] {Location}: {Message}";
        }
    }

    /// <summary>
    /// Result of a design rule check containing all detected violations.
    /// </summary>
    public class DRCResult
    {
        private readonly List<DRCViolationItem> _violations;

        /// <summary>Gets the read-only list of all violations.</summary>
        public IReadOnlyList<DRCViolationItem> Violations { get; }

        /// <summary>Gets whether any violations were detected.</summary>
        public bool HasViolations => _violations.Count > 0;

        /// <summary>Gets the number of short violations.</summary>
        public int ShortCount { get; }

        /// <summary>Gets the number of unconnected pin violations.</summary>
        public int UnconnectedCount { get; }

        /// <summary>
        /// Creates a new DRC result.
        /// </summary>
        /// <param name="violations">List of detected violations.</param>
        public DRCResult(List<DRCViolationItem> violations)
        {
            _violations = violations ?? new List<DRCViolationItem>();
            Violations = _violations.AsReadOnly();

            int shortCount = 0;
            int unconnectedCount = 0;
            for (int i = 0; i < _violations.Count; i++)
            {
                if (_violations[i].ViolationType == DRCViolationType.Short)
                    shortCount++;
                else if (_violations[i].ViolationType == DRCViolationType.UnconnectedPin)
                    unconnectedCount++;
            }

            ShortCount = shortCount;
            UnconnectedCount = unconnectedCount;
        }

        /// <summary>
        /// Returns a string summary of the DRC result.
        /// </summary>
        public override string ToString()
        {
            if (!HasViolations)
                return "DRC: No violations";

            return $"DRC: {_violations.Count} violation(s) ({ShortCount} shorts, {UnconnectedCount} unconnected pins)";
        }
    }
}
