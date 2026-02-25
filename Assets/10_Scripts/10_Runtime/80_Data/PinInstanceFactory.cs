using System.Collections.Generic;
using CircuitCraft.Core;
using UnityEngine;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Creates PinInstance lists from ComponentDefinition data.
    /// Extracted from PlacementController to allow reuse by StageManager for fixed placements.
    /// </summary>
    public static class PinInstanceFactory
    {
        /// <summary>
        /// Creates PinInstance list from a ComponentDefinition.
        /// Falls back to StandardPinDefinitions when the component has no explicit pin data.
        /// </summary>
        public static List<PinInstance> CreatePinInstances(ComponentDefinition definition)
        {
            List<PinInstance> pinInstances = new();
            var pins = definition.Pins;

            // Fallback to standard pin definitions for components without explicit pins.
            if (pins is null || pins.Length == 0)
            {
                pins = GetStandardPins(definition.Kind);
            }

            if (pins is not null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    var pinDef = pins[i];
                    GridPosition pinLocalPos = new GridPosition(pinDef.LocalPosition.x, pinDef.LocalPosition.y);
                    PinInstance pinInstance = new PinInstance(
                        pinIndex: i,
                        pinName: pinDef.PinName,
                        localPosition: pinLocalPos
                    );
                    pinInstances.Add(pinInstance);
                }
            }

            return pinInstances;
        }

        private static PinDefinition[] GetStandardPins(ComponentKind kind)
        {
            return kind switch
            {
                ComponentKind.BJT => StandardPinDefinitions.BJT,
                ComponentKind.MOSFET => StandardPinDefinitions.MOSFET,
                ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode => StandardPinDefinitions.Diode,
                ComponentKind.VoltageSource or ComponentKind.CurrentSource => StandardPinDefinitions.VerticalTwoPin,
                _ => StandardPinDefinitions.TwoPin
            };
        }
    }
}
