namespace CircuitCraft.Utils
{
    /// <summary>
    /// Formats electrical quantities into compact human-readable strings.
    /// </summary>
    public static class CircuitUnitFormatter
    {
        private const float One = 1f;
        private const float Mega = 1_000_000f;
        private const float Kilo = 1_000f;
        private const float Milli = 0.001f;
        private const float Micro = 0.000_001f;
        private const float Nano = 0.000_000_001f;
        private const float Giga = 1_000_000_000f;
        private const float Pico = 1_000_000_000_000f;

        private const double OneDouble = 1d;
        private const double ZeroDouble = 0d;
        private const double KiloDouble = 1e3;
        private const double MilliDouble = 1e-3;
        private const double MicroDouble = 1e-6;
        private const double NanoDouble = 1e-9;

        /// <summary>
        /// Formats resistance with ohm prefixes.
        /// </summary>
        /// <param name="ohms">Resistance value in ohms.</param>
        /// <returns>Formatted resistance string such as "4.7kΩ".</returns>
        public static string FormatResistance(float ohms)
        {
            if (ohms >= Mega)
            {
                return $"{ohms / Mega:0.##}MΩ";
            }

            if (ohms >= Kilo)
            {
                return $"{ohms / Kilo:0.##}kΩ";
            }

            return $"{ohms:0.##}Ω";
        }

        /// <summary>
        /// Formats capacitance with SI prefixes.
        /// </summary>
        /// <param name="farads">Capacitance value in farads.</param>
        /// <returns>Formatted capacitance string such as "100nF".</returns>
        public static string FormatCapacitance(float farads)
        {
            if (farads >= One)
            {
                return $"{farads:0.##}F";
            }

            if (farads >= Milli)
            {
                return $"{farads * Kilo:0.##}mF";
            }

            if (farads >= Micro)
            {
                return $"{farads * Mega:0.##}µF";
            }

            if (farads >= Nano)
            {
                return $"{farads * Giga:0.##}nF";
            }

            return $"{farads * Pico:0.##}pF";
        }

        /// <summary>
        /// Formats inductance with SI prefixes.
        /// </summary>
        /// <param name="henrys">Inductance value in henrys.</param>
        /// <returns>Formatted inductance string such as "10mH".</returns>
        public static string FormatInductance(float henrys)
        {
            if (henrys >= One)
            {
                return $"{henrys:0.##}H";
            }

            if (henrys >= Milli)
            {
                return $"{henrys * Kilo:0.##}mH";
            }

            if (henrys >= Micro)
            {
                return $"{henrys * Mega:0.##}µH";
            }

            return $"{henrys * Giga:0.##}nH";
        }

        /// <summary>
        /// Formats voltage using fixed precision.
        /// </summary>
        /// <param name="value">Voltage value in volts.</param>
        /// <returns>Formatted voltage string with a volt unit suffix.</returns>
        public static string FormatVoltage(double value)
        {
            return value >= 0
                ? $"{value:0.000} V"
                : $"{value:0.000} V";
        }

        /// <summary>
        /// Formats current with engineering prefixes.
        /// </summary>
        /// <param name="value">Current value in amps.</param>
        /// <returns>Formatted current string with an ampere unit suffix.</returns>
        public static string FormatCurrent(double value)
        {
            var absValue = value >= ZeroDouble ? value : -value;
            if (absValue >= KiloDouble)
            {
                return $"{value / KiloDouble:0.###} kA";
            }

            if (absValue >= OneDouble)
            {
                return $"{value:0.###} A";
            }

            if (absValue >= MilliDouble)
            {
                return $"{value * KiloDouble:0.###} mA";
            }

            if (absValue >= MicroDouble)
            {
                return $"{value / MicroDouble:0.###} µA";
            }

            return $"{value / NanoDouble:0.###} nA";
        }
    }
}
