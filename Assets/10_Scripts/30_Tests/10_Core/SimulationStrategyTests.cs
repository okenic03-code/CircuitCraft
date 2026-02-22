using System.Threading;
using NUnit.Framework;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Tests.Core
{
    [TestFixture]
    public class SimulationStrategyTests
    {
        [Test]
        public void DCOperatingPointStrategy_Execute_ReturnsVoltageDividerResult()
        {
            var netlist = new CircuitNetlist { Title = "Strategy DC Test" };
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "out", 1000.0));
            netlist.AddElement(NetlistElement.Resistor("R2", "out", "0", 2000.0));
            netlist.AddProbe(ProbeDefinition.Voltage("V_out", "out"));

            var circuit = new NetlistBuilder().Build(netlist);
            var strategy = new DCOperatingPointStrategy();

            var result = strategy.Execute(circuit, netlist, CancellationToken.None);

            Assert.IsTrue(result.IsSuccess, result.StatusMessage);
            var vOut = result.GetVoltage("out");
            Assert.IsNotNull(vOut);
            Assert.AreEqual(3.3333333333, vOut.Value, 0.01);
        }

        [Test]
        public void TransientAnalysisStrategy_Execute_ReturnsRCWaveform()
        {
            var netlist = new CircuitNetlist { Title = "Strategy Transient Test" };
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "cap", 1e3));
            netlist.AddElement(NetlistElement.Capacitor("C1", "cap", "0", 1e-6));
            netlist.AddProbe(ProbeDefinition.Voltage("V_cap", "cap"));

            var circuit = new NetlistBuilder().Build(netlist);
            var strategy = new TransientAnalysisStrategy(new TransientConfig(stopTime: 5e-3, maxStep: 5e-5));

            var result = strategy.Execute(circuit, netlist, CancellationToken.None);

            Assert.IsTrue(result.IsSuccess, result.StatusMessage);
            var probe = result.GetProbe("V_cap");
            Assert.IsNotNull(probe);
            Assert.Greater(probe.TimePoints.Count, 1);
            Assert.AreEqual(probe.TimePoints.Count, probe.Values.Count);
            Assert.AreEqual(0.0, probe.Values[0], 1e-6);
            Assert.Greater(probe.Values[probe.Values.Count - 1], 4.8);
        }
    }
}
