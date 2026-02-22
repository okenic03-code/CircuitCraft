namespace CircuitCraft.Data
{
    /// <summary>
    /// Categorizes the type of electronic component for simulation and categorization.
    /// </summary>
    public enum ComponentKind
    {
        /// <summary>Two-terminal resistive component.</summary>
        Resistor = 0,

        /// <summary>Two-terminal capacitive component.</summary>
        Capacitor = 1,

        /// <summary>Two-terminal inductive component.</summary>
        Inductor = 2,

        /// <summary>Two-terminal diode component.</summary>
        Diode = 3,

        /// <summary>Light-emitting diode component.</summary>
        LED = 4,

        /// <summary>Bipolar junction transistor component.</summary>
        BJT = 5,

        /// <summary>Metal-oxide-semiconductor field-effect transistor component.</summary>
        MOSFET = 6,

        /// <summary>Ideal voltage source component.</summary>
        VoltageSource = 7,

        /// <summary>Ideal current source component.</summary>
        CurrentSource = 8,

        /// <summary>Reference ground component.</summary>
        Ground = 9,

        /// <summary>Probe-only measurement component.</summary>
        Probe = 10,

        /// <summary>Zener diode component.</summary>
        ZenerDiode = 11
    }
}
