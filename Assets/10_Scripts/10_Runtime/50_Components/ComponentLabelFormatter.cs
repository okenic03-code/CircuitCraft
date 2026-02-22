using CircuitCraft.Data;
using CircuitCraft.Utils;

namespace CircuitCraft.Components
{
    internal static class ComponentLabelFormatter
    {
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
