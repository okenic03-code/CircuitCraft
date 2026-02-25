using System;
using System.Collections.Generic;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using CircuitCraft.Systems;
using UnityEngine;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Provides pure mapping helpers from simulation results to view-friendly data.
    /// </summary>
    public static class SimulationDataMapper
    {
        /// <summary>
        /// Extracts voltage probe values into a node-to-voltage dictionary.
        /// </summary>
        /// <param name="result">Simulation result containing probe values.</param>
        /// <returns>Dictionary of node name to measured voltage.</returns>
        public static Dictionary<string, double> ExtractNodeVoltages(SimulationResult result)
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

        /// <summary>
        /// Resolves current probe value for a component instance.
        /// </summary>
        /// <param name="definition">Component definition.</param>
        /// <param name="instanceId">Placed component instance ID.</param>
        /// <param name="result">Simulation result containing current probes.</param>
        /// <returns>Current value when available; otherwise null.</returns>
        public static double? GetComponentCurrent(ComponentDefinition definition, int instanceId, SimulationResult result)
        {
            if (result is null || definition == null)
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

        /// <summary>
        /// Converts component-level current into signed contribution by pin index.
        /// </summary>
        /// <param name="current">Component current.</param>
        /// <param name="pinIndex">Pin index on component.</param>
        /// <returns>Signed contribution for the pin.</returns>
        public static float GetPinSignedCurrentContribution(float current, int pinIndex)
        {
            return pinIndex switch
            {
                0 => current,
                1 => -current,
                _ => 0f
            };
        }

        /// <summary>
        /// Resolves resistance value from placed-component override or definition fallback.
        /// </summary>
        /// <param name="definition">Resistor definition.</param>
        /// <param name="component">Placed component instance.</param>
        /// <returns>Resolved resistance in ohms.</returns>
        public static float ResolveResistorValue(ComponentDefinition definition, PlacedComponent component)
        {
            if (component?.CustomValue.HasValue == true)
            {
                return component.CustomValue.Value;
            }

            if (definition == null)
            {
                return 0f;
            }

            if (definition.ResistanceOhms > 0f)
            {
                return definition.ResistanceOhms;
            }

            return 1000f;
        }

        /// <summary>
        /// Builds a trace segment current map keyed by segment ID.
        /// </summary>
        /// <param name="traces">Trace segments to evaluate.</param>
        /// <param name="componentViews">Placed component views by instance ID.</param>
        /// <param name="boardState">Board state used for pin/net connectivity.</param>
        /// <param name="result">Simulation result containing current probes.</param>
        /// <param name="currentThreshold">Minimum absolute current to include.</param>
        /// <returns>Segment current map for flow visualization.</returns>
        public static Dictionary<int, float> BuildTraceSegmentCurrentMap(
            IReadOnlyList<TraceSegment> traces,
            IReadOnlyDictionary<int, ComponentView> componentViews,
            BoardState boardState,
            SimulationResult result,
            float currentThreshold)
        {
            var segmentCurrentMap = new Dictionary<int, float>();
            if (result is null
                || boardState is null
                || componentViews == null
                || traces is null
                || traces.Count == 0)
            {
                return segmentCurrentMap;
            }

            var netCurrentMap = new Dictionary<int, float>();
            var fallbackNetCurrentMap = new Dictionary<int, float>();
            var fallbackNetAbsMap = new Dictionary<int, float>();

            foreach (var componentPair in componentViews)
            {
                var instanceId = componentPair.Key;
                var componentView = componentPair.Value;
                var placedComponent = boardState.GetComponent(instanceId);
                var definition = componentView != null ? componentView.Definition : null;
                if (placedComponent is null || definition == null)
                {
                    continue;
                }

                var componentCurrent = GetComponentCurrent(definition, instanceId, result);
                if (!componentCurrent.HasValue)
                {
                    continue;
                }

                var current = (float)componentCurrent.Value;
                var absCurrent = Mathf.Abs(current);
                if (absCurrent < currentThreshold)
                {
                    continue;
                }

                foreach (var pin in placedComponent.Pins)
                {
                    if (!pin.ConnectedNetId.HasValue)
                    {
                        continue;
                    }

                    int netId = pin.ConnectedNetId.Value;
                    var signedContribution = GetPinSignedCurrentContribution(current, pin.PinIndex);
                    if (!Mathf.Approximately(signedContribution, 0f))
                    {
                        netCurrentMap.TryGetValue(netId, out var aggregateCurrent);
                        netCurrentMap[netId] = aggregateCurrent + signedContribution;
                    }

                    if (!fallbackNetAbsMap.TryGetValue(netId, out var trackedAbs) || absCurrent > trackedAbs)
                    {
                        fallbackNetCurrentMap[netId] = current;
                        fallbackNetAbsMap[netId] = absCurrent;
                    }
                }
            }

            foreach (var trace in traces)
            {
                if (!netCurrentMap.TryGetValue(trace.NetId, out var netCurrent))
                {
                    if (!fallbackNetCurrentMap.TryGetValue(trace.NetId, out netCurrent))
                    {
                        continue;
                    }
                }

                if (Mathf.Abs(netCurrent) < currentThreshold)
                {
                    continue;
                }

                segmentCurrentMap[trace.SegmentId] = netCurrent;
            }

            return segmentCurrentMap;
        }

        /// <summary>
        /// Computes resistor power values keyed by component instance ID.
        /// </summary>
        /// <param name="componentViews">Placed component views by instance ID.</param>
        /// <param name="boardState">Board state used to resolve placed components.</param>
        /// <param name="result">Simulation result containing current probes.</param>
        /// <returns>Instance ID to resistor power map.</returns>
        public static Dictionary<int, double> GetResistorPowerMap(
            IReadOnlyDictionary<int, ComponentView> componentViews,
            BoardState boardState,
            SimulationResult result)
        {
            var resistorPowerByInstanceId = new Dictionary<int, double>();

            if (componentViews == null || boardState is null || result is null)
            {
                return resistorPowerByInstanceId;
            }

            foreach (var pair in componentViews)
            {
                int instanceId = pair.Key;
                var componentView = pair.Value;
                var definition = componentView != null ? componentView.Definition : null;
                var placedComponent = boardState.GetComponent(instanceId);

                if (definition == null || placedComponent is null || definition.Kind != ComponentKind.Resistor)
                {
                    continue;
                }

                var current = GetComponentCurrent(definition, instanceId, result);
                if (!current.HasValue)
                {
                    continue;
                }

                float resistance = ResolveResistorValue(definition, placedComponent);
                if (resistance <= 0f)
                {
                    continue;
                }

                var power = current.Value * current.Value * resistance;
                resistorPowerByInstanceId[instanceId] = power;
            }

            return resistorPowerByInstanceId;
        }
    }
}
