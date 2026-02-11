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

        private void Awake()
        {
            _simulationService = new SpiceSharpSimulationService();
            RebuildComponentDefinitionLookup();
            _netlistConverter = new BoardToNetlistConverter(this);

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

                CircuitNetlist netlist = _netlistConverter.Convert(boardState);

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
