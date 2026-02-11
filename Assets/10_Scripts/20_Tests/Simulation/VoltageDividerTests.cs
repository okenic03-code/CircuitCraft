using NUnit.Framework;
using System.Threading.Tasks;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Simulation
{
    /// <summary>
    /// NUnit tests for verifying basic circuit simulation functionality.
    /// Focuses on DC operating point analysis of a simple voltage divider.
    /// </summary>
    [TestFixture]
    public class VoltageDividerTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            // Initialize the simulation service implementation
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Verifies that a 5V source with 1k/2k resistors calculates Vout = 3.333V correctly.
        /// Circuit:
        /// V1 (5V) -> Node "in"
        /// R1 (1k) -> Node "in" to "out"
        /// R2 (2k) -> Node "out" to "0" (GND)
        /// Vout = V_in * R2 / (R1 + R2) = 5 * 2000 / (1000 + 2000) = 3.333V
        /// </summary>
        [Test]
        public async Task RunVoltageDividerTest_CalculatesCorrectOutputVoltage()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "Voltage Divider Test" };

            // 5V voltage source from "in" to ground "0"
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));

            // R1: 1kΩ from "in" to "out"
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "out", 1000.0));

            // R2: 2kΩ from "out" to ground "0"
            netlist.AddElement(NetlistElement.Resistor("R2", "out", "0", 2000.0));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_out", "out"));

            var request = SimulationRequest.DCOperatingPoint(netlist);
            request.IsSafetyChecksEnabled = true;

            // Act
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");
            
            var vOut = result.GetVoltage("out");
            Assert.IsNotNull(vOut, "Vout value was not found in results");

            double expectedVout = 5.0 * 2000.0 / (1000.0 + 2000.0); // 3.3333...
            Assert.AreEqual(expectedVout, vOut.Value, 0.001, $"Vout = {vOut.Value}V (expected {expectedVout}V)");
        }
    }
}
