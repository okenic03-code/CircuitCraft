namespace CircuitCraft.Data
{
    /// <summary>
    /// Categorizes the type of electronic component for simulation and categorization.
    /// </summary>
    public enum ComponentKind
    {
        Resistor,
        Capacitor,
        Inductor,
        Diode,
        LED,
        VoltageSource,
        CurrentSource,
        Ground,
        Probe
    }
}
