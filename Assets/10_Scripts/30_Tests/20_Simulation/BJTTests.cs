using NUnit.Framework;
using Cysharp.Threading.Tasks;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Simulation
{
    /// <summary>
    /// NUnit tests for verifying BJT transistor circuit simulation functionality.
    /// Focuses on DC operating point analysis of a common emitter NPN configuration.
    /// </summary>
    [TestFixture]
    public class BJTTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            // Initialize the simulation service implementation
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Verifies NPN transistor operates in active region.
        /// Circuit: Common emitter configuration with 2N2222 NPN
        /// VCC = 5V, RB = 10kΩ, RC = 1kΩ, RE = 100Ω
        /// Expected: VBE ~0.7V (silicon), collector above emitter (active region).
        /// </summary>
        [Test]
        public async UniTask NPN_CommonEmitter_OperatesInActiveRegion()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "NPN BJT Test" };

            // VCC: 5V power supply
            netlist.AddElement(NetlistElement.VoltageSource("VCC", "vcc", "0", 5.0));

            // RB: 10kΩ base resistor
            netlist.AddElement(NetlistElement.Resistor("RB", "vcc", "base", 10000.0));

            // RC: 1kΩ collector resistor
            netlist.AddElement(NetlistElement.Resistor("RC", "vcc", "coll", 1000.0));

            // RE: 100Ω emitter resistor
            netlist.AddElement(NetlistElement.Resistor("RE", "emit", "0", 100.0));

            // Q1: 2N2222 NPN transistor (collector, base, emitter)
            netlist.AddElement(NetlistElement.BJT("Q1", "coll", "base", "emit", "2N2222", true, 100.0, 75.0));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_base", "base"));
            netlist.AddProbe(ProbeDefinition.Voltage("V_coll", "coll"));
            netlist.AddProbe(ProbeDefinition.Voltage("V_emit", "emit"));
            netlist.AddProbe(ProbeDefinition.Current("I_C", "RC"));

            var request = SimulationRequest.DCOperatingPoint(netlist);

            // Act
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");

            var vBase = result.GetVoltage("base");
            var vColl = result.GetVoltage("coll");
            var vEmit = result.GetVoltage("emit");

            Assert.IsNotNull(vBase, "Base voltage not found");
            Assert.IsNotNull(vColl, "Collector voltage not found");
            Assert.IsNotNull(vEmit, "Emitter voltage not found");

            // Base-Emitter voltage should be ~0.7V for silicon NPN
            double vBE = vBase.Value - vEmit.Value;
            Assert.Greater(vBE, 0.5, "VBE should be > 0.5V");
            Assert.Less(vBE, 0.9, "VBE should be < 0.9V");

            // Collector should be above emitter (active region)
            Assert.Greater(vColl.Value, vEmit.Value, "Collector voltage should be > emitter voltage in active region");
        }
    }
}
