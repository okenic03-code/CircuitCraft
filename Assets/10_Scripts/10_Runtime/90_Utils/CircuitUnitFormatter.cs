using System;

namespace CircuitCraft.Utils
{
    public static class CircuitUnitFormatter
    {
        public static string FormatResistance(float ohms)
        {
            if (ohms >= 1_000_000)
            {
                return $"{ohms / 1_000_000:0.##}MΩ";
            }

            if (ohms >= 1_000)
            {
                return $"{ohms / 1_000:0.##}kΩ";
            }

            return $"{ohms:0.##}Ω";
        }

        public static string FormatCapacitance(float farads)
        {
            if (farads >= 1)
            {
                return $"{farads:0.##}F";
            }

            if (farads >= 0.001)
            {
                return $"{farads * 1_000:0.##}mF";
            }

            if (farads >= 0.000_001)
            {
                return $"{farads * 1_000_000:0.##}µF";
            }

            if (farads >= 0.000_000_001)
            {
                return $"{farads * 1_000_000_000:0.##}nF";
            }

            return $"{farads * 1_000_000_000_000:0.##}pF";
        }

        public static string FormatInductance(float henrys)
        {
            if (henrys >= 1)
            {
                return $"{henrys:0.##}H";
            }

            if (henrys >= 0.001)
            {
                return $"{henrys * 1_000:0.##}mH";
            }

            if (henrys >= 0.000_001)
            {
                return $"{henrys * 1_000_000:0.##}µH";
            }

            return $"{henrys * 1_000_000_000:0.##}nH";
        }

        public static string FormatVoltage(double value)
        {
            return value >= 0
                ? $"{value:0.000} V"
                : $"{value:0.000} V";
        }

        public static string FormatCurrent(double value)
        {
            var absValue = Math.Abs(value);
            if (absValue >= 1e3)
            {
                return $"{value / 1e3:0.###} kA";
            }

            if (absValue >= 1)
            {
                return $"{value:0.###} A";
            }

            if (absValue >= 1e-3)
            {
                return $"{value * 1e3:0.###} mA";
            }

            if (absValue >= 1e-6)
            {
                return $"{value * 1e6:0.###} µA";
            }

            return $"{value * 1e9:0.###} nA";
        }
    }
}
