namespace CircuitCraft.Data
{
    /// <summary>
    /// Categorizes the type of electronic component for simulation and categorization.
    /// </summary>
    public enum ComponentKind
    {
        Resistor = 0,
        Capacitor = 1,
        Inductor = 2,
        Diode = 3,
        LED = 4,
        BJT = 5,
        MOSFET = 6,
        VoltageSource = 7,
        CurrentSource = 8,
        Ground = 9,
        Probe = 10,
        ZenerDiode = 11
    }
}
