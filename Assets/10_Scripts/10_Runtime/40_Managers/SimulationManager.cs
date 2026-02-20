using System;
using System.Collections.Generic;
using System.Threading;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;
using CircuitCraft.Systems;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CircuitCraft.Managers
{
    /// <summary>
    /// Owns circuit simulation flow and execution state.
    /// Converts BoardState to netlist, validates circuit, and runs SpiceSharp simulation.
    /// </summary>
    public class SimulationManager : MonoBehaviour, IComponentDefinitionProvider
    {
        [Header("Component Definitions")]
        [Tooltip("All available component definitions for netlist conversion.")]
        [SerializeField] private ComponentDefinition[] _componentDefinitions;

        private readonly Dictionary<string, ComponentDefinition> _componentDefinitionLookup =
            new Dictionary<string, ComponentDefinition>();

        private ISimulationService _simulationService;
        private BoardToNetlistConverter _netlistConverter;
        private bool _isSimulating;
        private SimulationResult _lastSimulationResult;

        public bool IsSimulating => _isSimulating;
        public SimulationResult LastSimulationResult => _lastSimulationResult;

        public event Action<SimulationResult> OnSimulationCompleted;

        private void Awake() => Init();

        private void OnDestroy()
        {
            ServiceRegistry.Unregister(this);
        }

        private void Init()
        {
            InitializeSimulationService();
            InitializeComponentLookup();
            InitializeNetlistConverter();
            RegisterServices();
        }

        private void InitializeSimulationService()
        {
            _simulationService = new SpiceSharpSimulationService();
        }

        private void InitializeComponentLookup()
        {
            RebuildComponentDefinitionLookup();
        }

        private void InitializeNetlistConverter()
        {
            _netlistConverter = new BoardToNetlistConverter(this);
        }

        private void RegisterServices()
        {
            ServiceRegistry.Register(this);
        }

        /// <summary>
        /// Fire-and-forget simulation entry point.
        /// </summary>
        public void RunSimulation(BoardState boardState)
        {
            RunSimulationAsync(boardState, this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// Runs a DC operating point simulation on the provided board state.
        /// </summary>
        public async UniTask RunSimulationAsync(BoardState boardState, CancellationToken cancellationToken = default)
        {
            await RunSimulationAsync(boardState, null, false, false, cancellationToken);
        }

        /// <summary>
        /// Runs a DC operating point simulation and optionally captures node voltages and currents.
        /// </summary>
        public async UniTask RunSimulationAsync(
            BoardState boardState,
            bool captureAllNodeVoltages,
            bool captureAllComponentCurrents,
            CancellationToken cancellationToken = default)
        {
            await RunSimulationAsync(boardState, null, captureAllNodeVoltages, captureAllComponentCurrents, cancellationToken);
        }

        /// <summary>
        /// Runs a DC operating point simulation with optional auto probes while preserving
        /// caller-provided probes.
        /// </summary>
        public async UniTask RunSimulationAsync(
            BoardState boardState,
            IEnumerable<ProbeDefinition> probes,
            bool captureAllNodeVoltages,
            bool captureAllComponentCurrents = false,
            CancellationToken cancellationToken = default)
        {
            await RunSimulationAsync(
                boardState,
                AddVisualizationProbes(
                    boardState,
                    probes,
                    captureAllNodeVoltages,
                    captureAllComponentCurrents),
                cancellationToken);
        }

        /// <summary>
        /// Runs a DC operating point simulation on the provided board state.
        /// </summary>
        public async UniTask RunSimulationAsync(
            BoardState boardState,
            IEnumerable<ProbeDefinition> probes = null,
            CancellationToken cancellationToken = default)
        {
            if (_isSimulating)
            {
                Debug.LogWarning("SimulationManager: Simulation already in progress.");
                return;
            }

            if (boardState == null)
            {
                Debug.LogError("SimulationManager: No BoardState available.");
                return;
            }

            if (boardState.Components.Count == 0)
            {
                Debug.LogWarning("SimulationManager: Cannot simulate - no components on the board.");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.InvalidCircuit,
                    "No components placed on the board.");
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
                return;
            }

            _isSimulating = true;
            Debug.Log($"SimulationManager: Starting simulation with {boardState.Components.Count} component(s)...");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var allProbes = AddVisualizationProbes(boardState, probes, false, false);
                CircuitNetlist netlist = _netlistConverter.Convert(boardState, allProbes);

                if (netlist.Elements.Count == 0)
                {
                    Debug.LogWarning("SimulationManager: Netlist conversion produced no elements.");
                    _lastSimulationResult = SimulationResult.Failure(
                        SimulationType.DCOperatingPoint,
                        SimulationStatus.InvalidCircuit,
                        "No simulatable elements in circuit (check connections).");
                    OnSimulationCompleted?.Invoke(_lastSimulationResult);
                    return;
                }

                Debug.Log($"SimulationManager: Netlist created - {netlist.Elements.Count} element(s), {netlist.GetNodes().Count} node(s).");

                var validationResult = _simulationService.Validate(netlist);
                if (validationResult.HasErrors)
                {
                    Debug.LogWarning("SimulationManager: Circuit validation failed.");
                    foreach (var issue in validationResult.Issues)
                    {
                        LogIssue(issue);
                    }

                    _lastSimulationResult = validationResult;
                    OnSimulationCompleted?.Invoke(_lastSimulationResult);
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var request = SimulationRequest.DCOperatingPoint(netlist);
                _lastSimulationResult = await _simulationService.RunAsync(request, cancellationToken);

                HandleSimulationResult(_lastSimulationResult);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("SimulationManager: Simulation cancelled.");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.Error,
                    "Simulation cancelled.");
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"SimulationManager: Circuit error - {ex.Message}");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.InvalidCircuit,
                    ex.Message);
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SimulationManager: Simulation failed unexpectedly - {ex.Message}");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.Error,
                    $"Unexpected error: {ex.Message}");
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
            }
            finally
            {
                _isSimulating = false;
            }
        }

        public ComponentDefinition GetComponentDefinition(string componentDefId)
        {
            if (string.IsNullOrEmpty(componentDefId))
            {
                return null;
            }

            _componentDefinitionLookup.TryGetValue(componentDefId, out var definition);
            return definition;
        }

        public ComponentDefinition GetDefinition(string componentDefId)
        {
            return GetComponentDefinition(componentDefId);
        }

        private IEnumerable<ProbeDefinition> AddVisualizationProbes(
            BoardState boardState,
            IEnumerable<ProbeDefinition> additionalProbes,
            bool addAllNodeVoltages,
            bool addAllComponentCurrents)
        {
            if (boardState == null)
            {
                return additionalProbes != null ? new List<ProbeDefinition>(additionalProbes) : new List<ProbeDefinition>();
            }

            var probes = new List<ProbeDefinition>();
            if (additionalProbes != null)
            {
                probes.AddRange(additionalProbes);
            }

            if (!addAllNodeVoltages && !addAllComponentCurrents)
            {
                return probes;
            }

            if (addAllNodeVoltages)
            {
                foreach (var net in boardState.Nets)
                {
                    if (net == null || string.IsNullOrWhiteSpace(net.NetName))
                    {
                        continue;
                    }

                    var safeNetName = SanitizeIdentifier(net.NetName);
                    probes.Add(ProbeDefinition.Voltage($"V_NET_{net.NetId}_{safeNetName}", net.NetName));
                }
            }

            if (addAllComponentCurrents)
            {
                foreach (var component in boardState.Components)
                {
                    var definition = GetComponentDefinition(component.ComponentDefinitionId);
                    if (definition == null)
                    {
                        continue;
                    }

                    if (definition.Kind == ComponentKind.Ground || definition.Kind == ComponentKind.Probe)
                    {
                        continue;
                    }

                    var elementId = GetNetlistElementId(definition.Kind, component.InstanceId);
                    if (!string.IsNullOrEmpty(elementId))
                    {
                        probes.Add(ProbeDefinition.Current($"I_COMP_{component.InstanceId}", elementId));
                    }
                }
            }

            return probes;
        }

        private static string GetNetlistElementId(ComponentKind kind, int instanceId)
        {
            try
            {
                return $"{BoardToNetlistConverter.GetElementPrefix(kind)}{instanceId}";
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string SanitizeIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var output = new char[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                output[i] = char.IsLetterOrDigit(c) || c == '_' ? c : '_';
            }

            return new string(output);
        }

        private void RebuildComponentDefinitionLookup()
        {
            _componentDefinitionLookup.Clear();

            if (_componentDefinitions == null)
            {
                return;
            }

            foreach (var definition in _componentDefinitions)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.Id))
                {
                    _componentDefinitionLookup[definition.Id] = definition;
                }
            }
        }

        private void HandleSimulationResult(SimulationResult result)
        {
            if (result.IsSuccess)
            {
                Debug.Log($"SimulationManager: Simulation completed successfully in {result.ElapsedMilliseconds:F1}ms.");

                foreach (var probe in result.ProbeResults)
                {
                    Debug.Log($"  [{probe.Type}] {probe.Target}: {probe.GetFormattedValue()}");
                }

                foreach (var issue in result.Issues)
                {
                    if (issue.Severity == IssueSeverity.Warning)
                    {
                        Debug.LogWarning($"  {issue}");
                    }
                }
            }
            else
            {
                Debug.LogError($"SimulationManager: Simulation failed - {result.StatusMessage} (Status: {result.Status})");

                foreach (var issue in result.Issues)
                {
                    LogIssue(issue);
                }
            }

            OnSimulationCompleted?.Invoke(result);
        }

        private static void LogIssue(SimulationIssue issue)
        {
            switch (issue.Severity)
            {
                case IssueSeverity.Error:
                    Debug.LogError($"  {issue}");
                    break;
                case IssueSeverity.Warning:
                    Debug.LogWarning($"  {issue}");
                    break;
                default:
                    Debug.Log($"  {issue}");
                    break;
            }
        }
    }
}
