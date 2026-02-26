using UnityEngine;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Standard pin definitions for common component types.
    /// </summary>
    public static class StandardPinDefinitions
    {
        /// <summary>
        /// Standard 2-pin definition (e.g., resistor, capacitor, inductor).
        /// Pin 0: Terminal A, Pin 1: Terminal B
        /// </summary>
        public static PinDefinition[] TwoPin => new PinDefinition[]
        {
            new("A", new(0, 0)),
            new("B", new(1, 0))
        };

        /// <summary>
        /// Standard vertical 2-pin definition for voltage/current sources.
        /// Pin 0: Negative (-) bottom, Pin 1: Positive (+) top
        /// </summary>
        public static PinDefinition[] VerticalTwoPin => new PinDefinition[]
        {
            new("-", new(0, 0)),
            new("+", new(0, 1))
        };
        
        /// <summary>
        /// BJT transistor pins (NPN/PNP).
        /// Pin 0: Collector (C), Pin 1: Base (B), Pin 2: Emitter (E)
        /// </summary>
        public static PinDefinition[] BJT => new PinDefinition[]
        {
            new("C", new(0, 1)),
            new("B", new(0, 0)),
            new("E", new(0, -1))
        };
        
        /// <summary>
        /// MOSFET pins (3-pin: Drain, Gate, Source).
        /// Pin 0: Drain (D), Pin 1: Gate (G), Pin 2: Source (S)
        /// Note: Bulk is internally connected to Source in discrete MOSFETs.
        /// </summary>
        public static PinDefinition[] MOSFET => new PinDefinition[]
        {
            new("D", new(1, 1)),
            new("G", new(0, 0)),
            new("S", new(1, -1))
        };
        
        /// <summary>
        /// Diode pins (including LED).
        /// Pin 0: Anode (+), Pin 1: Cathode (-)
        /// </summary>
        public static PinDefinition[] Diode => new PinDefinition[]
        {
            new("Anode", new(0, 0)),
            new("Cathode", new(1, 0))
        };

        /// <summary>
        /// Ground pin definition.
        /// Pin 0: Ground reference.
        /// </summary>
        public static PinDefinition[] Ground => new PinDefinition[]
        {
            new("GND", new(0, 0))
        };

        /// <summary>
        /// Probe pin definition.
        /// Pin 0: Probe node.
        /// </summary>
        public static PinDefinition[] Probe => new PinDefinition[]
        {
            new("P", new(0, 0))
        };

        /// <summary>
        /// Returns the default fallback pin definitions for a component kind.
        /// </summary>
        public static PinDefinition[] GetForKind(ComponentKind kind)
        {
            return kind switch
            {
                ComponentKind.BJT => BJT,
                ComponentKind.MOSFET => MOSFET,
                ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode => Diode,
                ComponentKind.VoltageSource or ComponentKind.CurrentSource => VerticalTwoPin,
                ComponentKind.Ground => Ground,
                ComponentKind.Probe => Probe,
                _ => TwoPin
            };
        }
    }
}
