using NUnit.Framework;
using Cysharp.Threading.Tasks;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Simulation
{
    /// <summary>
    /// NUnit tests for verifying MOSFET transistor circuit simulation functionality.
    /// Focuses on DC operating point analysis of a common source NMOS configuration.
    /// </summary>
    [TestFixture]
    public class MOSFETTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            // Initialize the simulation service implementation
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Verifies N-Channel MOSFET operates in saturation region.
        /// Circuit: Common source configuration with 2N7000 NMOS
        /// VDD = 5V, RD = 1kΩ, RG = 10kΩ, Vgs = 3V
        /// Expected: Gate at ~3V, drain between 0V and VDD (current flowing),
        /// Vgs > Vth (2V) indicates MOSFET is ON.
        /// </summary>
        [Test]
        public async UniTask NMOS_CommonSource_OperatesInSaturation()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "NMOS Test" };

            // VDD: 5V power supply
            netlist.AddElement(NetlistElement.VoltageSource("VDD", "vdd", "0", 5.0));

            // VGS: 3V gate bias
            netlist.AddElement(NetlistElement.VoltageSource("VGS", "gate", "0", 3.0));

            // RD: 1kΩ drain resistor
            netlist.AddElement(NetlistElement.Resistor("RD", "vdd", "drain", 1000.0));

            // RG: 10kΩ gate resistor (for stability)
            netlist.AddElement(NetlistElement.Resistor("RG", "gate", "gate_mosfet", 10000.0));

            // M1: 2N7000 N-Channel MOSFET (D, G, S, S for Bulk=Source)
            netlist.AddElement(NetlistElement.MOSFET("M1", "drain", "gate_mosfet", "0", "0", "2N7000", true, 2.0, 0.3));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_gate", "gate_mosfet"));
            netlist.AddProbe(ProbeDefinition.Voltage("V_drain", "drain"));
            netlist.AddProbe(ProbeDefinition.Current("I_D", "RD"));

            var request = SimulationRequest.DCOperatingPoint(netlist);

            // Act
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");

            var vGate = result.GetVoltage("gate_mosfet");
            var vDrain = result.GetVoltage("drain");

            Assert.IsNotNull(vGate, "Gate voltage not found");
            Assert.IsNotNull(vDrain, "Drain voltage not found");

            // Gate voltage should be ~3V
            Assert.AreEqual(3.0, vGate.Value, 0.1, "Gate voltage should be 3V");

            // Drain voltage should be less than VDD (current flowing)
            Assert.Less(vDrain.Value, 5.0, "Drain voltage should be < VDD");
            Assert.Greater(vDrain.Value, 0.0, "Drain voltage should be > 0V");

            // Vgs > Vth indicates MOSFET is ON
            Assert.Greater(vGate.Value, 2.0, "Vgs should be > Vth (2V) for 2N7000");
        }
    }
}
