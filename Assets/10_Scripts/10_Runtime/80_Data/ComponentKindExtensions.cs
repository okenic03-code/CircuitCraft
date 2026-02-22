namespace CircuitCraft.Data
{
    /// <summary>
    /// Extension methods for the <see cref="ComponentKind"/> enum.
    /// </summary>
    public static class ComponentKindExtensions
    {
        /// <summary>
        /// Determines whether this component kind supports a custom value (e.g., resistance, capacitance).
        /// </summary>
        /// <param name="kind">The component kind to check.</param>
        /// <returns>
        /// <c>true</c> if the component supports custom values; otherwise, <c>false</c>.
        /// </returns>
        public static bool SupportsCustomValue(this ComponentKind kind) =>
            kind switch
            {
                ComponentKind.Resistor or ComponentKind.Capacitor or ComponentKind.Inductor or ComponentKind.VoltageSource or ComponentKind.CurrentSource => true,
                _ => false
            };

        /// <summary>
        /// Gets the unit symbol for the component's value (e.g., "Ω" for resistors).
        /// </summary>
        /// <param name="kind">The component kind.</param>
        /// <returns>The unit symbol, or an empty string if the component has no value unit.</returns>
        public static string GetValueUnit(this ComponentKind kind) =>
            kind switch
            {
                ComponentKind.Resistor => "Ω",
                ComponentKind.Capacitor => "F",
                ComponentKind.Inductor => "H",
                ComponentKind.VoltageSource => "V",
                ComponentKind.CurrentSource => "A",
                _ => string.Empty
            };

        /// <summary>
        /// Gets the human-readable label for the component's value (e.g., "Resistance" for resistors).
        /// </summary>
        /// <param name="kind">The component kind.</param>
        /// <returns>The value label, or an empty string if the component has no value label.</returns>
        public static string GetValueLabel(this ComponentKind kind) =>
            kind switch
            {
                ComponentKind.Resistor => "Resistance",
                ComponentKind.Capacitor => "Capacitance",
                ComponentKind.Inductor => "Inductance",
                ComponentKind.VoltageSource => "Voltage",
                ComponentKind.CurrentSource => "Current",
                _ => string.Empty
            };
    }
}
