using NUnit.Framework;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace CircuitCraft.Tests.Simulation
{
    [TestFixture]
    public class RCTransientTests
    {
        private ISimulationService _simulationService;

        [SetUp]
        public void Setup()
        {
            _simulationService = new SpiceSharpSimulationService();
        }

        [Test]
        public async UniTask RC_Transient_CapacitorCharging_MatchesExponentialCurve()
        {
            // Arrange
            var netlist = new CircuitNetlist { Title = "RC Transient Test" };
            
            // 5V voltage source
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));
            // 1kΩ resistor
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "cap", 1e3));
            // 1µF capacitor
            netlist.AddElement(NetlistElement.Capacitor("C1", "cap", "0", 1e-6));

            netlist.AddProbe(ProbeDefinition.Voltage("V_cap", "cap"));

            // Time constant τ = RC = 1k * 1u = 1ms
            double R = 1e3;
            double C = 1e-6;
            double tau = R * C;
            double V_in = 5.0;

            // Act
            // Run for 5τ = 5ms
            var request = SimulationRequest.Transient(netlist, stopTime: 5 * tau, maxStep: tau / 20.0);
            var result = await _simulationService.RunAsync(request);

            // Assert
            Assert.IsTrue(result.IsSuccess, $"Simulation failed: {result.StatusMessage}");
            
            var vCapProbe = result.GetProbe("V_cap");
            Assert.IsNotNull(vCapProbe, "V_cap probe not found in results");
            Assert.Greater(vCapProbe.TimePoints.Count, 0, "No time points collected");
            Assert.AreEqual(vCapProbe.TimePoints.Count, vCapProbe.Values.Count, "TimePoints and Values count mismatch");

            // 1. Initial condition (t=0): Capacitor voltage should be near 0V
            double initialVoltage = vCapProbe.Values.First();
            double initialTime = vCapProbe.TimePoints.First();
            Assert.LessOrEqual(Math.Abs(initialVoltage), 1e-6, $"Initial voltage at t={initialTime} should be ~0V");

            // 2. Charging progress at t = τ: V(τ) = V_in * (1 - e^-1) ≈ 0.6321 * 5V = 3.1605V
            int tauIndex = 0;
            double minTauDiff = double.MaxValue;
            for (int i = 0; i < vCapProbe.TimePoints.Count; i++)
            {
                double diff = Math.Abs(vCapProbe.TimePoints[i] - tau);
                if (diff < minTauDiff)
                {
                    minTauDiff = diff;
                    tauIndex = i;
                }
            }
            
            double tauTime = vCapProbe.TimePoints[tauIndex];
            double tauValue = vCapProbe.Values[tauIndex];
            double expectedTau = V_in * (1.0 - Math.Exp(-1.0));
            Assert.AreEqual(expectedTau, tauValue, 0.05, $"Voltage at t={tauTime:F4}s (closest to τ) should be ~{expectedTau:F3}V");

            // 3. Final charging state (t=5τ): V(5τ) = V_in * (1 - e^-5) ≈ 0.9933 * 5V = 4.966V
            double finalValue = vCapProbe.Values.Last();
            double finalTime = vCapProbe.TimePoints.Last();
            double expectedFinal = V_in * (1.0 - Math.Exp(-5.0));
            Assert.AreEqual(expectedFinal, finalValue, 0.05, $"Final voltage at t={finalTime:F4}s should be ~{expectedFinal:F3}V");
        }
    }
}
