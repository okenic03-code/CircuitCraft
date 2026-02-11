using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;

namespace CircuitCraft.Utils
{
    /// <summary>
    /// Test script for verifying SpiceSharp integration in Unity.
    /// Provides context menu commands to run test simulations.
    /// </summary>
    public class SpiceSharpTestRunner : MonoBehaviour
    {
        [Header("Test Results")]
        [FormerlySerializedAs("_lastTestPassed")]
        [SerializeField] private bool _isLastTestPassed;
        [SerializeField] private string _lastTestMessage;
        [SerializeField] private double _lastVoutValue;
        [SerializeField] private double _lastElapsedMs;

        private ISimulationService _simulationService;

        private void Awake()
        {
            _simulationService = new SpiceSharpSimulationService();
        }

        /// <summary>
        /// Runs a simple voltage divider test.
        /// Circuit: 5V source -> 1kΩ -> output -> 2kΩ -> GND
        /// Expected Vout: 5V * 2kΩ / (1kΩ + 2kΩ) = 3.333V
        /// </summary>
        [ContextMenu("Run Voltage Divider Test")]
        public void RunVoltageDividerTest()
        {
            RunVoltageDividerTestAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask RunVoltageDividerTestAsync(CancellationToken cancellationToken)
        {
            Debug.Log("=== SpiceSharp Voltage Divider Test ===");

            // Create netlist
            var netlist = new CircuitNetlist { Title = "Voltage Divider Test" };

            // 5V voltage source from "in" to ground
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));

            // R1: 1kΩ from "in" to "out"
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "out", 1e3));

            // R2: 2kΩ from "out" to ground
            netlist.AddElement(NetlistElement.Resistor("R2", "out", "0", 2e3));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_in", "in"));
            netlist.AddProbe(ProbeDefinition.Voltage("V_out", "out"));
            netlist.AddProbe(ProbeDefinition.Current("I_V1", "V1"));

            // Create request
            var request = SimulationRequest.DCOperatingPoint(netlist);
            request.IsSafetyChecksEnabled = true;

            // Run simulation
            if (_simulationService == null)
            {
                _simulationService = new SpiceSharpSimulationService();
            }

            var result = await _simulationService.RunAsync(request, cancellationToken);

            // Log results
            Debug.Log($"Status: {result.Status} - {result.StatusMessage}");
            Debug.Log($"Elapsed: {result.ElapsedMilliseconds:F2}ms");

            _lastElapsedMs = result.ElapsedMilliseconds;
            _lastTestMessage = result.StatusMessage;

            if (result.IsSuccess)
            {
                foreach (var probe in result.ProbeResults)
                {
                    Debug.Log($"  {probe.ProbeId}: {probe.GetFormattedValue()}");
                }

                // Verify expected output
                var vOut = result.GetVoltage("out");
                if (vOut.HasValue)
                {
                    _lastVoutValue = vOut.Value;
                    var expected = 5.0 * 2e3 / (1e3 + 2e3); // ~3.333V
                    var error = System.Math.Abs(vOut.Value - expected);
                    
                    if (error < 0.001)
                    {
                        Debug.Log($"<color=green>TEST PASSED!</color> Vout = {vOut.Value:F4}V (expected {expected:F4}V)");
                        _isLastTestPassed = true;
                    }
                    else
                    {
                        Debug.LogError($"TEST FAILED! Vout = {vOut.Value:F4}V (expected {expected:F4}V, error = {error:F4}V)");
                        _isLastTestPassed = false;
                    }
                }
            }
            else
            {
                Debug.LogError($"Simulation failed: {result.StatusMessage}");
                _isLastTestPassed = false;

                foreach (var issue in result.Issues)
                {
                    Debug.LogWarning($"  {issue}");
                }
            }
        }

        /// <summary>
        /// Runs a more complex LED current limiting circuit test.
        /// Circuit: 9V source -> 470Ω resistor -> LED (modeled as 2V drop) -> GND
        /// Expected current: (9V - 2V) / 470Ω ≈ 14.9mA
        /// </summary>
        [ContextMenu("Run LED Circuit Test")]
        public void RunLEDCircuitTest()
        {
            RunLEDCircuitTestAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask RunLEDCircuitTestAsync(CancellationToken cancellationToken)
        {
            Debug.Log("=== SpiceSharp LED Circuit Test ===");

            // Create netlist - simplified LED model using voltage source for forward drop
            var netlist = new CircuitNetlist { Title = "LED Current Limiter Test" };

            // 9V supply
            netlist.AddElement(NetlistElement.VoltageSource("Vsupply", "vcc", "0", 9.0));

            // Current limiting resistor: 470Ω (1/4W rated = 0.25W max)
            var resistor = NetlistElement.Resistor("R1", "vcc", "led_anode", 470.0, maxPowerWatts: 0.25);
            netlist.AddElement(resistor);

            // LED modeled as 2V voltage source (simplified)
            netlist.AddElement(NetlistElement.VoltageSource("Vled", "led_anode", "0", 2.0));

            // Add probes
            netlist.AddProbe(ProbeDefinition.Voltage("V_anode", "led_anode"));
            netlist.AddProbe(ProbeDefinition.Current("I_led", "Vled"));
            netlist.AddProbe(ProbeDefinition.Current("I_R1", "R1"));

            // Create request with safety checks
            var request = SimulationRequest.DCOperatingPoint(netlist);
            request.IsSafetyChecksEnabled = true;

            if (_simulationService == null)
            {
                _simulationService = new SpiceSharpSimulationService();
            }

            var result = await _simulationService.RunAsync(request, cancellationToken);

            Debug.Log($"Status: {result.Status} - {result.StatusMessage}");
            Debug.Log($"Elapsed: {result.ElapsedMilliseconds:F2}ms");

            _lastElapsedMs = result.ElapsedMilliseconds;
            _lastTestMessage = result.StatusMessage;

            if (result.IsSuccess)
            {
                foreach (var probe in result.ProbeResults)
                {
                    Debug.Log($"  {probe.ProbeId}: {probe.GetFormattedValue()}");
                }

                // Verify LED current
                var iLed = result.GetCurrent("Vled");
                if (iLed.HasValue)
                {
                    var currentMa = System.Math.Abs(iLed.Value) * 1000;
                    var expectedMa = (9.0 - 2.0) / 470.0 * 1000; // ~14.9mA
                    var errorMa = System.Math.Abs(currentMa - expectedMa);

                    if (errorMa < 0.1)
                    {
                        Debug.Log($"<color=green>TEST PASSED!</color> LED current = {currentMa:F2}mA (expected {expectedMa:F2}mA)");
                        _isLastTestPassed = true;
                    }
                    else
                    {
                        Debug.LogError($"TEST FAILED! LED current = {currentMa:F2}mA (expected {expectedMa:F2}mA)");
                        _isLastTestPassed = false;
                    }
                }

                // Check for any issues (like overpower)
                if (result.Issues.Count > 0)
                {
                    Debug.LogWarning("Issues detected:");
                    foreach (var issue in result.Issues)
                    {
                        Debug.LogWarning($"  {issue}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Simulation failed: {result.StatusMessage}");
                _isLastTestPassed = false;
            }
        }

        /// <summary>
        /// Runs a transient RC circuit test.
        /// Circuit: Step voltage source -> 1kΩ -> capacitor (1µF) -> GND
        /// Time constant τ = RC = 1ms
        /// </summary>
        [ContextMenu("Run RC Transient Test")]
        public void RunRCTransientTest()
        {
            RunRCTransientTestAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask RunRCTransientTestAsync(CancellationToken cancellationToken)
        {
            Debug.Log("=== SpiceSharp RC Transient Test ===");

            var netlist = new CircuitNetlist { Title = "RC Transient Test" };

            // 5V step voltage source
            netlist.AddElement(NetlistElement.VoltageSource("V1", "in", "0", 5.0));

            // 1kΩ resistor
            netlist.AddElement(NetlistElement.Resistor("R1", "in", "cap", 1e3));

            // 1µF capacitor
            netlist.AddElement(NetlistElement.Capacitor("C1", "cap", "0", 1e-6));

            // Probe the capacitor voltage
            netlist.AddProbe(ProbeDefinition.Voltage("V_cap", "cap"));
            netlist.AddProbe(ProbeDefinition.Current("I_cap", "R1"));

            // Time constant τ = 1kΩ * 1µF = 1ms
            // Run for 5τ = 5ms to see full charge
            var request = SimulationRequest.Transient(netlist, stopTime: 5e-3, maxStep: 50e-6);

            if (_simulationService == null)
            {
                _simulationService = new SpiceSharpSimulationService();
            }

            var result = await _simulationService.RunAsync(request, cancellationToken);

            Debug.Log($"Status: {result.Status} - {result.StatusMessage}");
            Debug.Log($"Elapsed: {result.ElapsedMilliseconds:F2}ms");

            _lastElapsedMs = result.ElapsedMilliseconds;
            _lastTestMessage = result.StatusMessage;

            if (result.IsSuccess)
            {
                var vCapProbe = result.GetProbe("V_cap");
                if (vCapProbe != null)
                {
                    Debug.Log($"  V_cap: Min={vCapProbe.MinValue:F3}V, Max={vCapProbe.MaxValue:F3}V, Final={vCapProbe.Value:F3}V");
                    Debug.Log($"  Points collected: {vCapProbe.TimePoints.Count}");

                    _lastVoutValue = vCapProbe.Value;

                    // After 5τ, capacitor should be at ~99.3% of 5V = ~4.97V
                    var expected = 5.0 * (1 - System.Math.Exp(-5)); // ~4.966V
                    var error = System.Math.Abs(vCapProbe.Value - expected);

                    if (error < 0.1)
                    {
                        Debug.Log($"<color=green>TEST PASSED!</color> Final V_cap = {vCapProbe.Value:F3}V (expected ~{expected:F3}V)");
                        _isLastTestPassed = true;
                    }
                    else
                    {
                        Debug.LogError($"TEST FAILED! Final V_cap = {vCapProbe.Value:F3}V (expected ~{expected:F3}V)");
                        _isLastTestPassed = false;
                    }
                }
            }
            else
            {
                Debug.LogError($"Simulation failed: {result.StatusMessage}");
                _isLastTestPassed = false;

                foreach (var issue in result.Issues)
                {
                    Debug.LogWarning($"  {issue}");
                }
            }
        }

        /// <summary>
        /// Runs all tests sequentially.
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            RunAllTestsAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunAllTestsAsync(CancellationToken cancellationToken)
        {
            Debug.Log("=== Running All SpiceSharp Tests ===\n");

            try
            {
                await RunVoltageDividerTestAsync(cancellationToken);
                Debug.Log("");

                await RunLEDCircuitTestAsync(cancellationToken);
                Debug.Log("");

                await RunRCTransientTestAsync(cancellationToken);
                Debug.Log($"Last test passed: {_isLastTestPassed}");

                Debug.Log("\n=== All Tests Complete ===");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("SpiceSharp tests cancelled.");
            }
        }
    }
}
