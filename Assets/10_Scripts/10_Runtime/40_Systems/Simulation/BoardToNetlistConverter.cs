using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;

namespace CircuitCraft.Systems
{
    /// <summary>
    /// Interface for providing component definitions by ID.
    /// </summary>
    public interface IComponentDefinitionProvider
    {
        /// <summary>
        /// Gets a component definition by its ID.
        /// </summary>
        /// <param name="componentDefId">Component definition ID.</param>
        /// <returns>The component definition, or null if not found.</returns>
        ComponentDefinition GetDefinition(string componentDefId);
    }

    /// <summary>
    /// Converts a BoardState into a CircuitNetlist for simulation.
    /// This is the bridge between the game's domain model and the SpiceSharp simulation engine.
    /// </summary>
    public class BoardToNetlistConverter
    {
        private readonly IComponentDefinitionProvider _componentProvider;

        /// <summary>
        /// Creates a new board to netlist converter.
        /// </summary>
        /// <param name="componentProvider">Provider for component definitions.</param>
        public BoardToNetlistConverter(IComponentDefinitionProvider componentProvider)
        {
            _componentProvider = componentProvider ?? throw new ArgumentNullException(nameof(componentProvider));
        }

        /// <summary>
        /// Converts a BoardState into a CircuitNetlist.
        /// </summary>
        /// <param name="boardState">The board state to convert.</param>
        /// <param name="probes">Optional probes to add to the netlist.</param>
        /// <returns>A circuit netlist ready for simulation.</returns>
        public CircuitNetlist Convert(BoardState boardState, IEnumerable<ProbeDefinition> probes = null)
        {
            if (boardState == null)
                throw new ArgumentNullException(nameof(boardState));

            var netlist = new CircuitNetlist
            {
                Title = "BoardState Circuit",
                GroundNode = "GND"
            };

            // Convert each placed component to netlist elements
            foreach (var component in boardState.Components)
            {
                var definition = _componentProvider.GetDefinition(component.ComponentDefinitionId);
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Component definition '{component.ComponentDefinitionId}' not found.");
                }

                // Map component to netlist element
                var element = ConvertComponent(component, definition, boardState);
                if (element != null)
                {
                    netlist.AddElement(element);
                }
            }

            // Add probes if provided
            if (probes != null)
            {
                foreach (var probe in probes)
                {
                    netlist.AddProbe(probe);
                }
            }

            return netlist;
        }

        /// <summary>
        /// Converts a placed component to a netlist element.
        /// </summary>
        private NetlistElement ConvertComponent(PlacedComponent component, ComponentDefinition definition, 
                                                 BoardState boardState)
        {
            var nodes = GetComponentNodes(component, boardState);
            var elementId = $"{GetElementPrefix(definition.Kind)}{component.InstanceId}";

            switch (definition.Kind)
            {
                case ComponentKind.Resistor:
                    if (nodes.Length >= 2)
                        return NetlistElement.Resistor(elementId, nodes[0], nodes[1], GetResistance(definition));
                    break;

                case ComponentKind.Capacitor:
                    if (nodes.Length >= 2)
                        return NetlistElement.Capacitor(elementId, nodes[0], nodes[1], GetCapacitance(definition));
                    break;

                case ComponentKind.Inductor:
                    if (nodes.Length >= 2)
                        return CreateInductor(elementId, nodes[0], nodes[1], GetInductance(definition));
                    break;

                case ComponentKind.VoltageSource:
                    if (nodes.Length >= 2)
                        return NetlistElement.VoltageSource(elementId, nodes[0], nodes[1], GetVoltage(definition));
                    break;

                case ComponentKind.CurrentSource:
                    if (nodes.Length >= 2)
                        return NetlistElement.CurrentSource(elementId, nodes[0], nodes[1], GetCurrent(definition));
                    break;

                case ComponentKind.Diode:
                case ComponentKind.LED:
                    if (nodes.Length >= 2)
                        return NetlistElement.Diode(elementId, nodes[0], nodes[1]);
                    break;

                case ComponentKind.BJT:
                    if (nodes.Length >= 3)
                        return NetlistElement.BJT(elementId, nodes[0], nodes[1], nodes[2]);
                    break;

                case ComponentKind.MOSFET:
                    if (nodes.Length >= 4)
                        return NetlistElement.MOSFET(elementId, nodes[0], nodes[1], nodes[2], nodes[3]);
                    break;

                case ComponentKind.Ground:
                    // Ground is handled via net names, not as a component
                    return null;

                case ComponentKind.Probe:
                    // Probes are handled separately, not as circuit elements
                    return null;

                default:
                    throw new NotSupportedException($"Component kind '{definition.Kind}' not yet supported in netlist conversion.");
            }

            return null;
        }

        /// <summary>
        /// Creates an inductor element (no factory method exists in NetlistElement).
        /// </summary>
        private NetlistElement CreateInductor(string id, string nodeA, string nodeB, double henrys)
        {
            return new NetlistElement
            {
                Id = id,
                Type = ElementType.Inductor,
                Value = henrys,
                Nodes = new List<string> { nodeA, nodeB }
            };
        }

        /// <summary>
        /// Gets the node names for a component based on its pin connections.
        /// </summary>
        private string[] GetComponentNodes(PlacedComponent component, BoardState boardState)
        {
            var nodes = new List<string>();

            foreach (var pin in component.Pins)
            {
                if (pin.ConnectedNetId.HasValue)
                {
                    var net = boardState.GetNet(pin.ConnectedNetId.Value);
                    if (net != null)
                    {
                        nodes.Add(net.NetName);
                    }
                    else
                    {
                        // Pin connected to invalid net - use placeholder
                        nodes.Add($"NC_{component.InstanceId}_{pin.PinIndex}");
                    }
                }
                else
                {
                    // Unconnected pin - use placeholder
                    nodes.Add($"NC_{component.InstanceId}_{pin.PinIndex}");
                }
            }

            return nodes.ToArray();
        }

        /// <summary>
        /// Maps ComponentKind to ElementType.
        /// </summary>
        public static ElementType MapComponentKind(ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    return ElementType.Resistor;
                case ComponentKind.Capacitor:
                    return ElementType.Capacitor;
                case ComponentKind.Inductor:
                    return ElementType.Inductor;
                case ComponentKind.VoltageSource:
                    return ElementType.VoltageSource;
                case ComponentKind.CurrentSource:
                    return ElementType.CurrentSource;
                case ComponentKind.Diode:
                case ComponentKind.LED:
                    return ElementType.Diode;
                case ComponentKind.BJT:
                    return ElementType.BJT;
                case ComponentKind.MOSFET:
                    return ElementType.MOSFET;
                default:
                    throw new NotSupportedException($"Component kind '{kind}' not mapped to ElementType.");
            }
        }

        /// <summary>
        /// Gets the SPICE element prefix for a component kind.
        /// </summary>
        public static string GetElementPrefix(ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    return "R";
                case ComponentKind.Capacitor:
                    return "C";
                case ComponentKind.Inductor:
                    return "L";
                case ComponentKind.VoltageSource:
                    return "V";
                case ComponentKind.CurrentSource:
                    return "I";
                case ComponentKind.Diode:
                case ComponentKind.LED:
                    return "D";
                case ComponentKind.BJT:
                    return "Q";
                case ComponentKind.MOSFET:
                    return "M";
                default:
                    return "X";
            }
        }

        // Helper methods to extract component values from ComponentDefinition
        private double GetResistance(ComponentDefinition def) => def.ResistanceOhms > 0 ? def.ResistanceOhms : 1000.0;
        private double GetCapacitance(ComponentDefinition def) => def.CapacitanceFarads > 0 ? def.CapacitanceFarads : 1e-6;
        private double GetInductance(ComponentDefinition def) => def.InductanceHenrys > 0 ? def.InductanceHenrys : 1e-3;
        private double GetVoltage(ComponentDefinition def) => def.VoltageVolts != 0 ? def.VoltageVolts : 5.0;
        private double GetCurrent(ComponentDefinition def) => def.CurrentAmps > 0 ? def.CurrentAmps : 0.001;
    }
}
