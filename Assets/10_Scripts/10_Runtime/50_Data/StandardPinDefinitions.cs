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
            new PinDefinition { Name = "A", LocalPosition = new Vector2Int(0, 0) },
            new PinDefinition { Name = "B", LocalPosition = new Vector2Int(1, 0) }
        };
        
        /// <summary>
        /// BJT transistor pins (NPN/PNP).
        /// Pin 0: Collector (C), Pin 1: Base (B), Pin 2: Emitter (E)
        /// </summary>
        public static PinDefinition[] BJT => new[]
        {
            new PinDefinition { Name = "C", LocalPosition = new Vector2Int(0, 1) },
            new PinDefinition { Name = "B", LocalPosition = new Vector2Int(0, 0) },
            new PinDefinition { Name = "E", LocalPosition = new Vector2Int(0, -1) }
        };
        
        /// <summary>
        /// MOSFET pins (3-pin: Drain, Gate, Source).
        /// Pin 0: Drain (D), Pin 1: Gate (G), Pin 2: Source (S)
        /// Note: Bulk is internally connected to Source in discrete MOSFETs.
        /// </summary>
        public static PinDefinition[] MOSFET => new[]
        {
            new PinDefinition { Name = "D", LocalPosition = new Vector2Int(1, 1) },
            new PinDefinition { Name = "G", LocalPosition = new Vector2Int(0, 0) },
            new PinDefinition { Name = "S", LocalPosition = new Vector2Int(1, -1) }
        };
        
        /// <summary>
        /// Diode pins (including LED).
        /// Pin 0: Anode (+), Pin 1: Cathode (-)
        /// </summary>
        public static PinDefinition[] Diode => new[]
        {
            new PinDefinition { Name = "Anode", LocalPosition = new Vector2Int(0, 0) },
            new PinDefinition { Name = "Cathode", LocalPosition = new Vector2Int(1, 0) }
        };
    }
}
