using System;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Simulation;
using CircuitCraft.Systems;
using UnityEngine;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Applies post-simulation visual feedback for power flow, including trace colors,
    /// component overlays, and LED glow effects.
    /// </summary>
    public class PowerFlowVisualizer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private TraceRenderer _traceRenderer;

        [Header("LED Glow")]
        [SerializeField] private float _ledGlowCurrentThresholdAmps = 1e-6f;
        [SerializeField] private Color _ledGlowColor = new Color(1f, 0.65f, 0.1f, 1f);

        private BoardState _boardState;

        private void Start()
        {
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_stageManager == null)
            {
                _stageManager = FindFirstObjectByType<StageManager>();
            }

            if (_boardView == null)
            {
                _boardView = FindFirstObjectByType<BoardView>();
            }

            if (_traceRenderer == null)
            {
                _traceRenderer = FindFirstObjectByType<TraceRenderer>();
            }

            if (_gameManager == null)
            {
                Debug.LogError("PowerFlowVisualizer: GameManager reference is missing.");
                return;
            }

            _gameManager.OnSimulationCompleted += HandleSimulationCompleted;
            _gameManager.OnBoardLoaded += HandleBoardStateReplaced;

            if (_stageManager != null)
            {
                _stageManager.OnStageLoaded += HandleBoardStateReplaced;
            }

            HandleBoardStateReplaced();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBoardState();

            if (_gameManager != null)
            {
                _gameManager.OnSimulationCompleted -= HandleSimulationCompleted;
                _gameManager.OnBoardLoaded -= HandleBoardStateReplaced;
            }

            if (_stageManager != null)
            {
                _stageManager.OnStageLoaded -= HandleBoardStateReplaced;
            }
        }

        private void HandleBoardStateReplaced()
        {
            SubscribeToBoardState(_gameManager != null ? _gameManager.BoardState : null);
            ClearVisualization();
        }

        private void HandleBoardStateReplaced(string stageId)
        {
            _ = stageId;
            HandleBoardStateReplaced();
        }

        private void HandleSimulationCompleted(SimulationResult result)
        {
            if (_boardState == null)
            {
                ClearVisualization();
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                ClearVisualization();
                return;
            }

            var nodeVoltages = ExtractNodeVoltages(result);
            ApplyTraceColors(nodeVoltages);
            ApplyComponentOverlays(nodeVoltages, result);
        }

        private void ApplyTraceColors(Dictionary<string, double> nodeVoltages)
        {
            if (_traceRenderer == null)
            {
                return;
            }

            if (nodeVoltages == null || nodeVoltages.Count == 0)
            {
                _traceRenderer.ResetColors();
                return;
            }

            float minVoltage = (float)nodeVoltages.Values.Min();
            float maxVoltage = (float)nodeVoltages.Values.Max();
            _traceRenderer.ApplyVoltageColors(nodeVoltages, minVoltage, maxVoltage);
        }

        private void ApplyComponentOverlays(Dictionary<string, double> nodeVoltages, SimulationResult result)
        {
            if (_boardView == null)
            {
                return;
            }

            foreach (var pair in _boardView.ComponentViews)
            {
                ComponentView componentView = pair.Value;
                if (componentView == null)
                {
                    continue;
                }

                var placedComponent = _boardState.GetComponent(pair.Key);
                if (placedComponent == null)
                {
                    componentView.HideSimulationOverlay();
                    componentView.ShowLEDGlow(false, _ledGlowColor);
                    continue;
                }

                var definition = componentView.Definition;
                if (definition == null)
                {
                    componentView.HideSimulationOverlay();
                    componentView.ShowLEDGlow(false, _ledGlowColor);
                    continue;
                }

                var measuredPinVoltages = new List<double>();
                foreach (var pin in placedComponent.Pins)
                {
                    if (!pin.ConnectedNetId.HasValue)
                    {
                        continue;
                    }

                    var net = _boardState.GetNet(pin.ConnectedNetId.Value);
                    if (net == null)
                    {
                        continue;
                    }

                    if (nodeVoltages.TryGetValue(net.NetName, out var pinVoltage))
                    {
                        measuredPinVoltages.Add(pinVoltage);
                    }
                }

                if (measuredPinVoltages.Count == 0)
                {
                    componentView.ShowSimulationOverlay("V: n/a\nI: n/a");
                    componentView.ShowLEDGlow(false, _ledGlowColor);
                    continue;
                }

                var averageVoltage = measuredPinVoltages.Average();
                var currentProbeValue = GetComponentCurrent(componentView.Definition, placedComponent.InstanceId, result);
                var currentText = currentProbeValue.HasValue
                    ? $"I: {FormatCurrent(currentProbeValue.Value)}"
                    : "I: n/a";

                componentView.ShowSimulationOverlay(
                    $"V: {FormatVoltage(averageVoltage)}\n{currentText}");

                var shouldGlow = definition.Kind == ComponentKind.LED
                    && currentProbeValue.HasValue
                    && Math.Abs(currentProbeValue.Value) >= _ledGlowCurrentThresholdAmps;

                componentView.ShowLEDGlow(shouldGlow, _ledGlowColor);
            }
        }

        private double? GetComponentCurrent(ComponentDefinition definition, int instanceId, SimulationResult result)
        {
            if (result == null || definition == null)
            {
                return null;
            }

            if (definition.Kind == ComponentKind.Ground || definition.Kind == ComponentKind.Probe)
            {
                return null;
            }

            try
            {
                var elementId = $"{BoardToNetlistConverter.GetElementPrefix(definition.Kind)}{instanceId}";
                return result.GetCurrent(elementId);
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string FormatVoltage(double value)
        {
            return value >= 0
                ? $"{value:0.000} V"
                : $"{value:0.000} V";
        }

        private static string FormatCurrent(double value)
        {
            var absValue = Math.Abs(value);
            if (absValue >= 1e3)
            {
                return $"{value / 1e3:0.###} kA";
            }

            if (absValue >= 1)
            {
                return $"{value:0.###} A";
            }

            if (absValue >= 1e-3)
            {
                return $"{value * 1e3:0.###} mA";
            }

            if (absValue >= 1e-6)
            {
                return $"{value * 1e6:0.###} µA";
            }

            return $"{value * 1e9:0.###} nA";
        }

        private void ClearVisualization()
        {
            if (_traceRenderer != null)
            {
                _traceRenderer.ResetColors();
            }

            if (_boardView == null)
            {
                return;
            }

            foreach (var view in _boardView.ComponentViews.Values)
            {
                if (view == null)
                {
                    continue;
                }

                view.HideSimulationOverlay();
                view.ShowLEDGlow(false, _ledGlowColor);
            }
        }

        private static Dictionary<string, double> ExtractNodeVoltages(SimulationResult result)
        {
            var nodeVoltages = new Dictionary<string, double>(StringComparer.Ordinal);
            if (result?.ProbeResults == null)
            {
                return nodeVoltages;
            }

            for (int i = 0; i < result.ProbeResults.Count; i++)
            {
                var probe = result.ProbeResults[i];
                if (probe == null || probe.Type != ProbeType.Voltage)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(probe.Target))
                {
                    continue;
                }

                nodeVoltages[probe.Target] = probe.Value;
            }

            return nodeVoltages;
        }

        private void SubscribeToBoardState(BoardState boardState)
        {
            if (_boardState == boardState)
            {
                return;
            }

            UnsubscribeFromBoardState();
            _boardState = boardState;

            if (_boardState == null)
            {
                return;
            }

            _boardState.OnComponentPlaced += HandleBoardEdited;
            _boardState.OnComponentRemoved += HandleBoardEdited;
            _boardState.OnTraceAdded += HandleBoardTraceEdited;
            _boardState.OnTraceRemoved += HandleBoardTraceRemoved;
        }

        private void UnsubscribeFromBoardState()
        {
            if (_boardState == null)
            {
                return;
            }

            _boardState.OnComponentPlaced -= HandleBoardEdited;
            _boardState.OnComponentRemoved -= HandleBoardEdited;
            _boardState.OnTraceAdded -= HandleBoardTraceEdited;
            _boardState.OnTraceRemoved -= HandleBoardTraceRemoved;
        }

        private void HandleBoardEdited(PlacedComponent _)
        {
            ClearVisualization();
        }

        private void HandleBoardEdited(int _)
        {
            ClearVisualization();
        }

        private void HandleBoardTraceEdited(TraceSegment _)
        {
            ClearVisualization();
        }

        private void HandleBoardTraceRemoved(int _)
        {
            ClearVisualization();
        }
    }
}
