using System;
using System.Collections.Generic;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Simulation;
using CircuitCraft.Utils;
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

        private const float TraceCurrentThresholdAmps = 1e-6f;

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

            var nodeVoltages = SimulationDataMapper.ExtractNodeVoltages(result);
            ApplyTraceColors(nodeVoltages);
            ApplyTraceCurrentFlow(result);
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

            float minVoltage = float.MaxValue;
            float maxVoltage = float.MinValue;
            foreach (var v in nodeVoltages.Values)
            {
                float fv = (float)v;
                if (fv < minVoltage) minVoltage = fv;
                if (fv > maxVoltage) maxVoltage = fv;
            }
            _traceRenderer.ApplyVoltageColors(nodeVoltages, minVoltage, maxVoltage);
        }

        private void ApplyTraceCurrentFlow(SimulationResult result)
        {
            if (_traceRenderer == null || _boardState == null)
            {
                return;
            }

            var segmentCurrents = SimulationDataMapper.BuildTraceSegmentCurrentMap(
                _boardState.Traces,
                _boardView != null ? _boardView.ComponentViews : null,
                _boardState,
                result,
                TraceCurrentThresholdAmps);
            _traceRenderer.ApplyCurrentFlow(segmentCurrents);
        }

        private void ApplyComponentOverlays(Dictionary<string, double> nodeVoltages, SimulationResult result)
        {
            if (_boardView == null)
            {
                return;
            }

            var resistorPowerByInstanceId = SimulationDataMapper.GetResistorPowerMap(
                _boardView.ComponentViews,
                _boardState,
                result);
            double maxPower = 0d;
            foreach (var power in resistorPowerByInstanceId.Values)
            {
                if (power > maxPower) maxPower = power;
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
                    componentView.HideResistorHeatGlow();
                    continue;
                }

                var definition = componentView.Definition;
                if (definition == null)
                {
                    componentView.HideSimulationOverlay();
                    componentView.ShowLEDGlow(false, _ledGlowColor);
                    componentView.HideResistorHeatGlow();
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
                    componentView.HideResistorHeatGlow();
                    continue;
                }

                double voltageSum = 0d;
                for (int i = 0; i < measuredPinVoltages.Count; i++)
                {
                    voltageSum += measuredPinVoltages[i];
                }

                var averageVoltage = voltageSum / measuredPinVoltages.Count;
                var currentProbeValue = SimulationDataMapper.GetComponentCurrent(componentView.Definition, placedComponent.InstanceId, result);
                var currentText = currentProbeValue.HasValue
                    ? $"I: {CircuitUnitFormatter.FormatCurrent(currentProbeValue.Value)}"
                    : "I: n/a";

                componentView.ShowSimulationOverlay(
                    $"V: {CircuitUnitFormatter.FormatVoltage(averageVoltage)}\n{currentText}");

                var shouldGlow = definition.Kind == ComponentKind.LED
                    && currentProbeValue.HasValue
                    && Math.Abs(currentProbeValue.Value) >= _ledGlowCurrentThresholdAmps;

                componentView.ShowLEDGlow(shouldGlow, _ledGlowColor);

                var isResistor = definition.Kind == ComponentKind.Resistor;
                if (isResistor
                    && resistorPowerByInstanceId.TryGetValue(pair.Key, out var power)
                    && maxPower > 0)
                {
                    componentView.ShowResistorHeatGlow(true, (float)(power / maxPower));
                }
                else
                {
                    componentView.HideResistorHeatGlow();
                }
            }
        }

        private void ClearVisualization()
        {
            if (_traceRenderer != null)
            {
                _traceRenderer.StopCurrentFlow();
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
                view.HideResistorHeatGlow();
            }
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
