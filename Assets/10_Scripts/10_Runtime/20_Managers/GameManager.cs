using System;
using System.Collections.Generic;
using UnityEngine;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using CircuitCraft.Simulation.SpiceSharp;
using CircuitCraft.Systems;

namespace CircuitCraft.Managers
{
    /// <summary>
    /// Main game controller for CircuitCraft gameplay.
    /// Manages BoardState lifecycle and coordinates simulation execution.
    /// Bridges the game domain (BoardState) with the simulation engine (SpiceSharp).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Board Configuration")]
        [SerializeField] private int _boardWidth = 20;
        [SerializeField] private int _boardHeight = 20;

        [Header("Dependencies")]
        [SerializeField] private BoardState _boardState;

        [Header("Component Definitions")]
        [Tooltip("All available component definitions for netlist conversion.")]
        [SerializeField] private ComponentDefinition[] _componentDefinitions;

        private ISimulationService _simulationService;
        private BoardToNetlistConverter _netlistConverter;

        /// <summary>
        /// Whether a simulation is currently running.
        /// </summary>
        private bool _isSimulating;

        /// <summary>
        /// The result of the most recent simulation run, or null if none.
        /// </summary>
        private SimulationResult _lastSimulationResult;

        private void Awake()
        {
            // Initialize services
            _simulationService = new SpiceSharpSimulationService();

            // Initialize netlist converter with component definitions
            var definitionProvider = new ComponentDefinitionLookup(_componentDefinitions);
            _netlistConverter = new BoardToNetlistConverter(definitionProvider);

            // Initialize BoardState with configured grid size
            if (_boardState == null)
            {
                _boardState = new BoardState(_boardWidth, _boardHeight);
                Debug.Log($"GameManager: BoardState initialized ({_boardWidth}x{_boardHeight})");
            }
        }

        /// <summary>
        /// Gets the current BoardState instance.
        /// </summary>
        public BoardState BoardState => _boardState;

        /// <summary>
        /// Gets the simulation service instance.
        /// </summary>
        public ISimulationService SimulationService => _simulationService;

        /// <summary>
        /// Whether a simulation is currently in progress.
        /// </summary>
        public bool IsSimulating => _isSimulating;

        /// <summary>
        /// The result of the most recent simulation, or null if no simulation has been run.
        /// </summary>
        public SimulationResult LastSimulationResult => _lastSimulationResult;

        /// <summary>
        /// Event raised when a simulation completes (success or failure).
        /// </summary>
        public event Action<SimulationResult> OnSimulationCompleted;

        /// <summary>
        /// Runs a DC operating point simulation on the current BoardState.
        /// Converts the board to a netlist, executes the simulation, and logs results.
        /// Called by UI "Simulate" button.
        /// </summary>
        public void RunSimulation()
        {
            if (_isSimulating)
            {
                Debug.LogWarning("GameManager: Simulation already in progress.");
                return;
            }

            // 1. Validate BoardState has components
            if (_boardState == null)
            {
                Debug.LogError("GameManager: No BoardState available.");
                return;
            }

            if (_boardState.Components.Count == 0)
            {
                Debug.LogWarning("GameManager: Cannot simulate - no components on the board.");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.InvalidCircuit,
                    "No components placed on the board.");
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
                return;
            }

            _isSimulating = true;
            Debug.Log($"GameManager: Starting simulation with {_boardState.Components.Count} component(s)...");

            try
            {
                // 2. Convert BoardState to netlist
                CircuitNetlist netlist = _netlistConverter.Convert(_boardState);

                if (netlist.Elements.Count == 0)
                {
                    Debug.LogWarning("GameManager: Netlist conversion produced no elements.");
                    _lastSimulationResult = SimulationResult.Failure(
                        SimulationType.DCOperatingPoint,
                        SimulationStatus.InvalidCircuit,
                        "No simulatable elements in circuit (check connections).");
                    OnSimulationCompleted?.Invoke(_lastSimulationResult);
                    return;
                }

                Debug.Log($"GameManager: Netlist created - {netlist.Elements.Count} element(s), {netlist.GetNodes().Count} node(s).");

                // 3. Validate circuit before simulation
                var validationResult = _simulationService.Validate(netlist);
                if (validationResult.HasErrors)
                {
                    Debug.LogWarning("GameManager: Circuit validation failed.");
                    foreach (var issue in validationResult.Issues)
                    {
                        LogIssue(issue);
                    }
                    _lastSimulationResult = validationResult;
                    OnSimulationCompleted?.Invoke(_lastSimulationResult);
                    return;
                }

                // 4. Run DC operating point simulation
                var request = SimulationRequest.DCOperatingPoint(netlist);
                _lastSimulationResult = _simulationService.Run(request);

                // 5. Handle results
                HandleSimulationResult(_lastSimulationResult);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"GameManager: Circuit error - {ex.Message}");
                _lastSimulationResult = SimulationResult.Failure(
                    SimulationType.DCOperatingPoint,
                    SimulationStatus.InvalidCircuit,
                    ex.Message);
                OnSimulationCompleted?.Invoke(_lastSimulationResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager: Simulation failed unexpectedly - {ex.Message}");
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

        /// <summary>
        /// Processes and logs simulation results.
        /// </summary>
        private void HandleSimulationResult(SimulationResult result)
        {
            if (result.IsSuccess)
            {
                Debug.Log($"GameManager: Simulation completed successfully in {result.ElapsedMilliseconds:F1}ms.");

                // Log probe results
                foreach (var probe in result.ProbeResults)
                {
                    Debug.Log($"  [{probe.Type}] {probe.Target}: {probe.GetFormattedValue()}");
                }

                // Log any warnings
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
                Debug.LogError($"GameManager: Simulation failed - {result.StatusMessage} (Status: {result.Status})");

                foreach (var issue in result.Issues)
                {
                    LogIssue(issue);
                }
            }

            OnSimulationCompleted?.Invoke(result);
        }

        /// <summary>
        /// Logs a simulation issue with appropriate Unity log level.
        /// </summary>
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

        /// <summary>
        /// Simple lookup that maps component definition IDs to their definitions.
        /// Wraps a serialized array of ComponentDefinition ScriptableObjects.
        /// </summary>
        private class ComponentDefinitionLookup : IComponentDefinitionProvider
        {
            private readonly Dictionary<string, ComponentDefinition> _definitions;

            public ComponentDefinitionLookup(ComponentDefinition[] definitions)
            {
                _definitions = new Dictionary<string, ComponentDefinition>();
                if (definitions == null) return;

                foreach (var def in definitions)
                {
                    if (def != null && !string.IsNullOrEmpty(def.Id))
                    {
                        _definitions[def.Id] = def;
                    }
                }
            }

            public ComponentDefinition GetDefinition(string componentDefId)
            {
                if (string.IsNullOrEmpty(componentDefId)) return null;
                _definitions.TryGetValue(componentDefId, out var definition);
                return definition;
            }
        }
    }
}
