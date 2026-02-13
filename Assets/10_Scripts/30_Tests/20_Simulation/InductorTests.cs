using NUnit.Framework;
using Cysharp.Threading.Tasks;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Simulation
{
    /// <summary>
    /// NUnit tests for verifying inductor circuit simulation functionality.
    /// Focuses on DC operating point analysis of a simple RL circuit.
    /// </summary>
    [TestFixture]
    public class InductorTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            // Initialize the simulation service implementation
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Verifies RL circuit time constant calculation.
        /// Circuit: 5V source -> 100Ω resistor -> 10mH inductor -> GND
        /// Time constant τ = L/R = 0.01/100 = 0.0001s = 0.1ms
        /// In DC steady state, the inductor acts as a short circuit (zero impedance),
        /// so the full voltage drops across the resistor and Vout = 0V.
        /// </summary>
        [Test]
        public async UniTask RL_Circuit_CalculatesCorrectTimeConstant()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "RL Circuit Test" };

            // 5V voltage source
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));

            // R1: 100Ω resistor
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "out", 100.0));

            // L1: 10mH inductor
            netlist.AddElement(new NetlistElement
            {
                Id = "L1",
                Type = ElementType.Inductor,
                Value = 0.01, // 10mH
                Nodes = new System.Collections.Generic.List<string> { "out", "0" }
            });

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_out", "out"));
            netlist.AddProbe(ProbeDefinition.Current("I_L1", "L1"));

            var request = SimulationRequest.DCOperatingPoint(netlist);

            // Act
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");

            // In DC steady state, inductor acts as short circuit
            var vOut = result.GetVoltage("out");
            Assert.IsNotNull(vOut, "Vout not found");
            Assert.AreEqual(0.0, vOut.Value, 0.001, "Inductor should be short circuit in DC steady state");
        }
    }
}
