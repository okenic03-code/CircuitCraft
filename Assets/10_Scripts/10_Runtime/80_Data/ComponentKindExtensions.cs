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
        public static bool SupportsCustomValue(this ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                case ComponentKind.Capacitor:
                case ComponentKind.Inductor:
                case ComponentKind.VoltageSource:
                case ComponentKind.CurrentSource:
                    return true;
                case ComponentKind.Diode:
                case ComponentKind.LED:
                case ComponentKind.ZenerDiode:
                case ComponentKind.BJT:
                case ComponentKind.MOSFET:
                case ComponentKind.Ground:
                case ComponentKind.Probe:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the unit symbol for the component's value (e.g., "Ω" for resistors).
        /// </summary>
        /// <param name="kind">The component kind.</param>
        /// <returns>The unit symbol, or an empty string if the component has no value unit.</returns>
        public static string GetValueUnit(this ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    return "Ω";
                case ComponentKind.Capacitor:
                    return "F";
                case ComponentKind.Inductor:
                    return "H";
                case ComponentKind.VoltageSource:
                    return "V";
                case ComponentKind.CurrentSource:
                    return "A";
                case ComponentKind.Diode:
                case ComponentKind.LED:
                case ComponentKind.ZenerDiode:
                case ComponentKind.BJT:
                case ComponentKind.MOSFET:
                case ComponentKind.Ground:
                case ComponentKind.Probe:
                    return "";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Gets the human-readable label for the component's value (e.g., "Resistance" for resistors).
        /// </summary>
        /// <param name="kind">The component kind.</param>
        /// <returns>The value label, or an empty string if the component has no value label.</returns>
        public static string GetValueLabel(this ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    return "Resistance";
                case ComponentKind.Capacitor:
                    return "Capacitance";
                case ComponentKind.Inductor:
                    return "Inductance";
                case ComponentKind.VoltageSource:
                    return "Voltage";
                case ComponentKind.CurrentSource:
                    return "Current";
                case ComponentKind.Diode:
                case ComponentKind.LED:
                case ComponentKind.ZenerDiode:
                case ComponentKind.BJT:
                case ComponentKind.MOSFET:
                case ComponentKind.Ground:
                case ComponentKind.Probe:
                    return "";
                default:
                    return "";
            }
        }
    }
}
