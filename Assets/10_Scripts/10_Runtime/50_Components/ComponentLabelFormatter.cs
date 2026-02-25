using CircuitCraft.Data;
using CircuitCraft.Utils;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Formats user-facing labels for component values.
    /// </summary>
    internal static class ComponentLabelFormatter
    {
        /// <summary>
        /// Returns a formatted label based on component kind and value.
        /// </summary>
        /// <param name="definition">Component definition containing display values.</param>
        /// <returns>Formatted label text.</returns>
        public static string FormatLabel(ComponentDefinition definition)
        {
            return definition.Kind switch
            {
                ComponentKind.Resistor => CircuitUnitFormatter.FormatResistance(definition.ResistanceOhms),
                ComponentKind.Capacitor => CircuitUnitFormatter.FormatCapacitance(definition.CapacitanceFarads),
                ComponentKind.Inductor => CircuitUnitFormatter.FormatInductance(definition.InductanceHenrys),
                ComponentKind.VoltageSource => $"{definition.VoltageVolts}V",
                ComponentKind.CurrentSource => $"{definition.CurrentAmps}A",
                ComponentKind.Ground => "GND",
                _ => definition.DisplayName
            };
        }
    }
}
