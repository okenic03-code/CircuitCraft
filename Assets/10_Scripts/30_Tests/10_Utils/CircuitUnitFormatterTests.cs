using NUnit.Framework;
using CircuitCraft.Utils;

namespace CircuitCraft.Tests.Utils
{
    /// <summary>
    /// NUnit EditMode tests for CircuitUnitFormatter utility class.
    /// Covers all five public formatting methods with boundary values and edge cases.
    /// </summary>
    [TestFixture]
    public class CircuitUnitFormatterTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // FormatResistance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FormatResistance_Below1k_ReturnsOhms()
        {
            Assert.AreEqual("100Ω", CircuitUnitFormatter.FormatResistance(100f));
        }

        [Test]
        public void FormatResistance_ExactlyAt1k_ReturnskOhms()
        {
            // threshold is >= 1000, so 1000 should produce kΩ
            Assert.AreEqual("1kΩ", CircuitUnitFormatter.FormatResistance(1000f));
        }

        [Test]
        public void FormatResistance_Just_Below1k_ReturnsOhms()
        {
            Assert.AreEqual("999Ω", CircuitUnitFormatter.FormatResistance(999f));
        }

        [Test]
        public void FormatResistance_4700_Returns4Point7kOhms()
        {
            Assert.AreEqual("4.7kΩ", CircuitUnitFormatter.FormatResistance(4700f));
        }

        [Test]
        public void FormatResistance_ExactlyAt1M_ReturnsMOhms()
        {
            // threshold is >= 1_000_000, so 1_000_000 should produce MΩ
            Assert.AreEqual("1MΩ", CircuitUnitFormatter.FormatResistance(1_000_000f));
        }

        [Test]
        public void FormatResistance_2200000_Returns2Point2MOhms()
        {
            Assert.AreEqual("2.2MΩ", CircuitUnitFormatter.FormatResistance(2_200_000f));
        }

        [Test]
        public void FormatResistance_Zero_ReturnsZeroOhms()
        {
            Assert.AreEqual("0Ω", CircuitUnitFormatter.FormatResistance(0f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // FormatCapacitance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FormatCapacitance_1Microfarad_ReturnsMicrofarads()
        {
            // 0.000001 F = 1 µF
            Assert.AreEqual("1µF", CircuitUnitFormatter.FormatCapacitance(0.000001f));
        }

        [Test]
        public void FormatCapacitance_100Nanofarad_ReturnsNanofarads()
        {
            // 100 nF = 0.0000001 F — between nF and µF threshold
            Assert.AreEqual("100nF", CircuitUnitFormatter.FormatCapacitance(0.0000001f));
        }

        [Test]
        public void FormatCapacitance_1Nanofarad_ReturnsNanofarads()
        {
            // 0.000000001 F = 1 nF (exact threshold)
            Assert.AreEqual("1nF", CircuitUnitFormatter.FormatCapacitance(0.000000001f));
        }

        [Test]
        public void FormatCapacitance_1Picofarad_ReturnsPicofarads()
        {
            // 0.000000000001 F = 1 pF (below nF threshold)
            Assert.AreEqual("1pF", CircuitUnitFormatter.FormatCapacitance(0.000000000001f));
        }

        [Test]
        public void FormatCapacitance_1Farad_ReturnsFarads()
        {
            Assert.AreEqual("1F", CircuitUnitFormatter.FormatCapacitance(1f));
        }

        [Test]
        public void FormatCapacitance_1Millifarad_ReturnsMillifarads()
        {
            // 0.001 F = 1 mF (exact mF threshold)
            Assert.AreEqual("1mF", CircuitUnitFormatter.FormatCapacitance(0.001f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // FormatInductance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FormatInductance_1Millihenry_ReturnsMillihenrys()
        {
            // 0.001 H = 1 mH (exact mH threshold)
            Assert.AreEqual("1mH", CircuitUnitFormatter.FormatInductance(0.001f));
        }

        [Test]
        public void FormatInductance_1Microhenry_ReturnsMicrohenrys()
        {
            // 0.000001 H = 1 µH (exact µH threshold)
            Assert.AreEqual("1µH", CircuitUnitFormatter.FormatInductance(0.000001f));
        }

        [Test]
        public void FormatInductance_1Henry_ReturnsHenrys()
        {
            Assert.AreEqual("1H", CircuitUnitFormatter.FormatInductance(1f));
        }

        [Test]
        public void FormatInductance_BelowMicrohenry_ReturnsNanohenrys()
        {
            // 0.000000001 H = 1 nH
            Assert.AreEqual("1nH", CircuitUnitFormatter.FormatInductance(0.000000001f));
        }

        [Test]
        public void FormatInductance_100Millihenry_ReturnsMillihenrys()
        {
            // 0.1 H = 100 mH
            Assert.AreEqual("100mH", CircuitUnitFormatter.FormatInductance(0.1f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // FormatVoltage
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FormatVoltage_PositiveValue_ReturnsThreeDecimalPlaces()
        {
            Assert.AreEqual("5.000 V", CircuitUnitFormatter.FormatVoltage(5.0));
        }

        [Test]
        public void FormatVoltage_NegativeValue_ReturnsThreeDecimalPlaces()
        {
            // FormatVoltage is intentionally identical for positive and negative
            Assert.AreEqual("-3.300 V", CircuitUnitFormatter.FormatVoltage(-3.3));
        }

        [Test]
        public void FormatVoltage_Zero_ReturnsZeroVolts()
        {
            Assert.AreEqual("0.000 V", CircuitUnitFormatter.FormatVoltage(0.0));
        }

        // ─────────────────────────────────────────────────────────────────────
        // FormatCurrent
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FormatCurrent_1Milliamp_ReturnsMilliamps()
        {
            // 0.001 A = 1 mA (exact mA threshold)
            Assert.AreEqual("1 mA", CircuitUnitFormatter.FormatCurrent(0.001));
        }

        [Test]
        public void FormatCurrent_1Microamp_ReturnsMicroamps()
        {
            // 0.000001 A = 1 µA (exact µA threshold)
            Assert.AreEqual("1 µA", CircuitUnitFormatter.FormatCurrent(0.000001));
        }

        [Test]
        public void FormatCurrent_1Amp_ReturnsAmps()
        {
            Assert.AreEqual("1 A", CircuitUnitFormatter.FormatCurrent(1.0));
        }

        [Test]
        public void FormatCurrent_1Kiloamp_ReturnsKiloamps()
        {
            // 1000 A = 1 kA (exact kA threshold)
            Assert.AreEqual("1 kA", CircuitUnitFormatter.FormatCurrent(1000.0));
        }

        [Test]
        public void FormatCurrent_NegativeMilliamp_PreservesSign()
        {
            // FormatCurrent uses Math.Abs for scale, but preserves sign in output
            Assert.AreEqual("-1 mA", CircuitUnitFormatter.FormatCurrent(-0.001));
        }

        [Test]
        public void FormatCurrent_BelowMicroamp_ReturnsNanoamps()
        {
            // 0.000000001 A = 1 nA
            Assert.AreEqual("1 nA", CircuitUnitFormatter.FormatCurrent(0.000000001));
        }

        [Test]
        public void FormatCurrent_Zero_ReturnsZeroNanoamps()
        {
            // 0 A → abs is 0 → falls through all thresholds → nA range
            Assert.AreEqual("0 nA", CircuitUnitFormatter.FormatCurrent(0.0));
        }
    }
}
