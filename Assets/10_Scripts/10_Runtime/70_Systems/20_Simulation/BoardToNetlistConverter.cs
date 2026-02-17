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
                GroundNode = "0"
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
                        return NetlistElement.Resistor(elementId, nodes[0], nodes[1], GetResistance(definition, component));
                    break;

                case ComponentKind.Capacitor:
                    if (nodes.Length >= 2)
                        return NetlistElement.Capacitor(elementId, nodes[0], nodes[1], GetCapacitance(definition, component));
                    break;

                case ComponentKind.Inductor:
                    if (nodes.Length >= 2)
                        return CreateInductor(elementId, nodes[0], nodes[1], GetInductance(definition, component));
                    break;

                case ComponentKind.VoltageSource:
                    if (nodes.Length >= 2)
                        return NetlistElement.VoltageSource(elementId, nodes[0], nodes[1], GetVoltage(definition, component));
                    break;

                case ComponentKind.CurrentSource:
                    if (nodes.Length >= 2)
                        return NetlistElement.CurrentSource(elementId, nodes[0], nodes[1], GetCurrent(definition, component));
                    break;

                case ComponentKind.Diode:
                case ComponentKind.LED:
                case ComponentKind.ZenerDiode:
                    if (nodes.Length >= 2)
                        return NetlistElement.Diode(elementId, nodes[0], nodes[1],
                            GetDiodeModelName(definition),
                            definition.SaturationCurrent,
                            definition.EmissionCoefficient,
                            definition.BreakdownVoltage,
                            definition.BreakdownCurrent);
                    break;

                case ComponentKind.BJT:
                    if (nodes.Length >= 3)
                        return NetlistElement.BJT(elementId, nodes[0], nodes[1], nodes[2],
                            GetBJTModelName(definition),
                            definition.BJTPolarity == BJTPolarity.NPN,
                            definition.Beta,
                            definition.EarlyVoltage);
                    break;

                case ComponentKind.MOSFET:
                    if (nodes.Length >= 3)
                        // Pass nodes[2] (Source) as Bulk to auto-connect
                        return NetlistElement.MOSFET(elementId, nodes[0], nodes[1], nodes[2], nodes[2],
                            GetMOSFETModelName(definition),
                            definition.FETPolarity == FETPolarity.NChannel,
                            definition.ThresholdVoltage,
                            definition.Transconductance);
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
                case ComponentKind.ZenerDiode:
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
                case ComponentKind.ZenerDiode:
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
        private double GetResistance(ComponentDefinition def, PlacedComponent component) =>
            component.CustomValue.HasValue ? component.CustomValue.Value : (def.ResistanceOhms > 0 ? def.ResistanceOhms : 1000.0);
        private double GetCapacitance(ComponentDefinition def, PlacedComponent component) =>
            component.CustomValue.HasValue ? component.CustomValue.Value : (def.CapacitanceFarads > 0 ? def.CapacitanceFarads : 1e-6);
        private double GetInductance(ComponentDefinition def, PlacedComponent component) =>
            component.CustomValue.HasValue ? component.CustomValue.Value : (def.InductanceHenrys > 0 ? def.InductanceHenrys : 1e-3);
        private double GetVoltage(ComponentDefinition def, PlacedComponent component) =>
            component.CustomValue.HasValue ? component.CustomValue.Value : (def.VoltageVolts != 0 ? def.VoltageVolts : 5.0);
        private double GetCurrent(ComponentDefinition def, PlacedComponent component) =>
            component.CustomValue.HasValue ? component.CustomValue.Value : (def.CurrentAmps > 0 ? def.CurrentAmps : 0.001);

        /// <summary>
        /// Gets the SPICE model name from BJT model enum.
        /// </summary>
        private static string GetBJTModelName(ComponentDefinition definition)
        {
            switch (definition.BJTModel)
            {
                case BJTModel._2N2222: return "2N2222";
                case BJTModel._2N3904: return "2N3904";
                case BJTModel._2N3906: return "2N3906";
                case BJTModel.BC547: return "BC547";
                case BJTModel.BC557: return "BC557";
                case BJTModel.BC548: return "BC548";
                case BJTModel.BC558: return "BC558";
                case BJTModel.BC556: return "BC556";
                case BJTModel.BC337: return "BC337";
                case BJTModel.TIP31: return "TIP31";
                case BJTModel.TIP32: return "TIP32";
                case BJTModel._2N696: return "2N696";
                case BJTModel.Generic_NPN: return "Generic_NPN";
                case BJTModel.Generic_PNP: return "Generic_PNP";
                case BJTModel.Custom: return "Custom_BJT";
                default: return "2N2222";
            }
        }

        /// <summary>
        /// Gets the SPICE model name from MOSFET model enum.
        /// </summary>
        private static string GetMOSFETModelName(ComponentDefinition definition)
        {
            switch (definition.MOSFETModel)
            {
                case MOSFETModel._2N7000: return "2N7000";
                case MOSFETModel.BS170: return "BS170";
                case MOSFETModel.IRF540: return "IRF540";
                case MOSFETModel.IRF9540: return "IRF9540";
                case MOSFETModel.BS250: return "BS250";
                case MOSFETModel.IRF3205: return "IRF3205";
                case MOSFETModel.IRF530: return "IRF530";
                case MOSFETModel.IRLZ44N: return "IRLZ44N";
                case MOSFETModel.IRF520: return "IRF520";
                case MOSFETModel.BSS84: return "BSS84";
                case MOSFETModel.TP0610L: return "TP0610L";
                case MOSFETModel.FQP27P06: return "FQP27P06";
                case MOSFETModel.Generic_NMOS: return "Generic_NMOS";
                case MOSFETModel.Generic_PMOS: return "Generic_PMOS";
                case MOSFETModel.Custom: return "Custom_MOSFET";
                default: return "NMOS";
            }
        }

        /// <summary>
        /// Gets the SPICE model name from Diode model enum.
        /// </summary>
        private static string GetDiodeModelName(ComponentDefinition definition)
        {
            switch (definition.DiodeModel)
            {
                case DiodeModel._1N4148: return "1N4148";
                case DiodeModel._1N4001: return "1N4001";
                case DiodeModel._1N5819: return "1N5819";
                case DiodeModel._1N4007: return "1N4007";
                case DiodeModel._1N914: return "1N914";
                case DiodeModel._1N4002: return "1N4002";
                case DiodeModel._1N4004: return "1N4004";
                case DiodeModel._1N5408: return "1N5408";
                case DiodeModel.BAS40: return "BAS40";
                case DiodeModel.BAT85: return "BAT85";
                case DiodeModel.LED_Red: return "LED_Red";
                case DiodeModel.LED_Green: return "LED_Green";
                case DiodeModel.LED_Blue: return "LED_Blue";
                case DiodeModel.LED_White: return "LED_White";
                case DiodeModel.LED_Yellow: return "LED_Yellow";
                case DiodeModel.Zener_3V3: return "1N4728A";
                case DiodeModel.Zener_3V6: return "1N4729A";
                case DiodeModel.Zener_3V9: return "1N4730A";
                case DiodeModel.Zener_4V3: return "1N4731A";
                case DiodeModel.Zener_4V7: return "1N4732A";
                case DiodeModel.Zener_5V1: return "1N4733A";
                case DiodeModel.Zener_5V6: return "1N4734A";
                case DiodeModel.Zener_6V8: return "1N4736A";
                case DiodeModel.Zener_8V2: return "1N4738A";
                case DiodeModel.Zener_9V1: return "1N4739A";
                case DiodeModel.Zener_12V: return "1N4742A";
                case DiodeModel.Zener_15V: return "1N4744A";
                case DiodeModel.Zener_27V: return "1N4750A";
                case DiodeModel.Generic: return "Generic_Diode";
                case DiodeModel.Custom: return "Custom_Diode";
                default: return "1N4148";
            }
        }
    }
}
