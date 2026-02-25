using NUnit.Framework;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;
using System;
using Cysharp.Threading.Tasks;

namespace CircuitCraft.Tests.Simulation
{
    [TestFixture]
    public class LEDCircuitTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            _simulationService = new SpiceSharpSimulationService();
        }

        [Test]
        public async UniTask LEDCircuit_9VSource_470OhmResistor_2VLED_HasCorrectCurrent()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "LED Current Limiter Test" };

            // 9V supply
            netlist.AddElement(NetlistElement.VoltageSource("Vsupply", "vcc", "0", 9.0));

            // Current limiting resistor: 470Ω
            netlist.AddElement(NetlistElement.Resistor("R1", "vcc", "led_anode", 470.0));

            // LED: Is=1e-12A, N=2.0 yields Vf≈2V at ~15mA
            netlist.AddElement(NetlistElement.Diode("Dled", "led_anode", "0", 
                modelName: "LED_RED", 
                saturationCurrent: 1e-12, 
                emissionCoefficient: 2.0));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Current("I_led", "Dled"));

            // Act
            var request = SimulationRequest.DCOperatingPoint(netlist);
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");
            
            var iLed = result.GetCurrent("Dled");
            Assert.IsNotNull(iLed, "Could not find current probe for Dled");

            double currentAmps = Math.Abs(iLed.Value);
            double expectedAmps = (9.0 - 2.0) / 470.0; // ≈ 0.0148936 Amps
            
            // Tolerance ±0.0001A (0.1mA) as per requirement
            Assert.AreEqual(expectedAmps, currentAmps, 0.0001, $"Expected ≈ {expectedAmps}A, but got {currentAmps}A");
        }
    }
}
