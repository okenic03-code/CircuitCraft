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
        public static PinDefinition[] TwoPin => new[]
        {
            new("A", new(0, 0)),
            new("B", new(1, 0))
        };

        /// <summary>
        /// Standard vertical 2-pin definition for voltage/current sources.
        /// Pin 0: Negative (-) bottom, Pin 1: Positive (+) top
        /// </summary>
        public static PinDefinition[] VerticalTwoPin => new[]
        {
            new("-", new(0, 0)),
            new("+", new(0, 1))
        };
        
        /// <summary>
        /// BJT transistor pins (NPN/PNP).
        /// Pin 0: Collector (C), Pin 1: Base (B), Pin 2: Emitter (E)
        /// </summary>
        public static PinDefinition[] BJT => new[]
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
        public static PinDefinition[] MOSFET => new[]
        {
            new("D", new(1, 1)),
            new("G", new(0, 0)),
            new("S", new(1, -1))
        };
        
        /// <summary>
        /// Diode pins (including LED).
        /// Pin 0: Anode (+), Pin 1: Cathode (-)
        /// </summary>
        public static PinDefinition[] Diode => new[]
        {
            new("Anode", new(0, 0)),
            new("Cathode", new(1, 0))
        };
    }
}
