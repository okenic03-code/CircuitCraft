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
            if (boardState is null)
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
                if (definition is null)
                {
                    throw new InvalidOperationException(
                        $"Component definition '{component.ComponentDefinitionId}' not found.");
                }

                // Map component to netlist element
                var element = ConvertComponent(component, definition, boardState);
                if (element is not null)
                {
                    netlist.AddElement(element);
                }
            }

            // Add probes if provided
            if (probes is not null)
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

            return definition.Kind switch
            {
                ComponentKind.Resistor when nodes.Length >= 2 => NetlistElement.Resistor(elementId, nodes[0], nodes[1], GetResistance(definition, component)),
                ComponentKind.Capacitor when nodes.Length >= 2 => NetlistElement.Capacitor(elementId, nodes[0], nodes[1], GetCapacitance(definition, component)),
                ComponentKind.Inductor when nodes.Length >= 2 => CreateInductor(elementId, nodes[0], nodes[1], GetInductance(definition, component)),
                ComponentKind.VoltageSource when nodes.Length >= 2 => CreateVoltageSourceElement(component, elementId, nodes, GetVoltage(definition, component)),
                ComponentKind.CurrentSource when nodes.Length >= 2 => CreateCurrentSourceElement(component, elementId, nodes, GetCurrent(definition, component)),
                ComponentKind.Diode when nodes.Length >= 2 => NetlistElement.Diode(elementId, nodes[0], nodes[1],
                    GetDiodeModelName(definition),
                    definition.SaturationCurrent,
                    definition.EmissionCoefficient,
                    definition.BreakdownVoltage,
                    definition.BreakdownCurrent),
                ComponentKind.LED when nodes.Length >= 2 => NetlistElement.Diode(elementId, nodes[0], nodes[1],
                    GetDiodeModelName(definition),
                    definition.SaturationCurrent,
                    definition.EmissionCoefficient,
                    definition.BreakdownVoltage,
                    definition.BreakdownCurrent),
                ComponentKind.ZenerDiode when nodes.Length >= 2 => NetlistElement.Diode(elementId, nodes[0], nodes[1],
                    GetDiodeModelName(definition),
                    definition.SaturationCurrent,
                    definition.EmissionCoefficient,
                    definition.BreakdownVoltage,
                    definition.BreakdownCurrent),
                ComponentKind.BJT when nodes.Length >= 3 => NetlistElement.BJT(elementId, nodes[0], nodes[1], nodes[2],
                    GetBJTModelName(definition),
                    definition.BJTPolarity == BJTPolarity.NPN,
                    definition.Beta,
                    definition.EarlyVoltage),
                ComponentKind.MOSFET when nodes.Length >= 3 => NetlistElement.MOSFET(elementId, nodes[0], nodes[1], nodes[2], nodes[2],
                    GetMOSFETModelName(definition),
                    definition.FETPolarity == FETPolarity.NChannel,
                    definition.ThresholdVoltage,
                    definition.Transconductance),
                ComponentKind.Resistor or ComponentKind.Capacitor or ComponentKind.Inductor or ComponentKind.VoltageSource or ComponentKind.CurrentSource or ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode or ComponentKind.BJT or ComponentKind.MOSFET => null,
                ComponentKind.Ground => null,
                ComponentKind.Probe => null,
                _ => throw new NotSupportedException($"Component kind '{definition.Kind}' not yet supported in netlist conversion.")
            };
        }

        /// <summary>
        /// Creates an inductor element (no factory method exists in NetlistElement).
        /// </summary>
        private NetlistElement CreateInductor(string id, string nodeA, string nodeB, double henrys)
        {
            return new()
            {
                Id = id,
                Type = ElementType.Inductor,
                Value = henrys,
                Nodes = new() { nodeA, nodeB }
            };
        }

        private static NetlistElement CreateVoltageSourceElement(
            PlacedComponent component,
            string elementId,
            string[] nodes,
            double volts)
        {
            GetSourceNodeOrder(component, out int positiveIndex, out int negativeIndex);
            return NetlistElement.VoltageSource(elementId, nodes[positiveIndex], nodes[negativeIndex], volts);
        }

        private static NetlistElement CreateCurrentSourceElement(
            PlacedComponent component,
            string elementId,
            string[] nodes,
            double amps)
        {
            GetSourceNodeOrder(component, out int positiveIndex, out int negativeIndex);
            return NetlistElement.CurrentSource(elementId, nodes[positiveIndex], nodes[negativeIndex], amps);
        }

        private static void GetSourceNodeOrder(PlacedComponent component, out int positiveIndex, out int negativeIndex)
        {
            positiveIndex = 0;
            negativeIndex = 1;

            if (component?.Pins == null || component.Pins.Count < 2)
            {
                return;
            }

            string pin0 = component.Pins[0].PinName ?? string.Empty;
            string pin1 = component.Pins[1].PinName ?? string.Empty;

            bool pin0Positive = IsPositivePinName(pin0);
            bool pin0Negative = IsNegativePinName(pin0);
            bool pin1Positive = IsPositivePinName(pin1);
            bool pin1Negative = IsNegativePinName(pin1);

            if (pin0Negative && pin1Positive)
            {
                positiveIndex = 1;
                negativeIndex = 0;
                return;
            }

            if (pin0Positive && pin1Negative)
            {
                positiveIndex = 0;
                negativeIndex = 1;
                return;
            }

            if (pin0Negative && !pin1Negative)
            {
                positiveIndex = 1;
                negativeIndex = 0;
                return;
            }

            if (pin1Negative && !pin0Negative)
            {
                positiveIndex = 0;
                negativeIndex = 1;
            }
        }

        private static bool IsPositivePinName(string pinName)
        {
            return pinName.Contains("+", StringComparison.Ordinal)
                   || pinName.Contains("pos", StringComparison.OrdinalIgnoreCase)
                   || pinName.Contains("positive", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNegativePinName(string pinName)
        {
            return pinName.Contains("-", StringComparison.Ordinal)
                   || pinName.Contains("neg", StringComparison.OrdinalIgnoreCase)
                   || pinName.Contains("negative", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the node names for a component based on its pin connections.
        /// </summary>
        private string[] GetComponentNodes(PlacedComponent component, BoardState boardState)
        {
            List<string> nodes = new();

            foreach (var pin in component.Pins)
            {
                if (pin.ConnectedNetId.HasValue)
                {
                    var net = boardState.GetNet(pin.ConnectedNetId.Value);
                    if (net is not null)
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
            return kind switch
            {
                ComponentKind.Resistor => ElementType.Resistor,
                ComponentKind.Capacitor => ElementType.Capacitor,
                ComponentKind.Inductor => ElementType.Inductor,
                ComponentKind.VoltageSource => ElementType.VoltageSource,
                ComponentKind.CurrentSource => ElementType.CurrentSource,
                ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode => ElementType.Diode,
                ComponentKind.BJT => ElementType.BJT,
                ComponentKind.MOSFET => ElementType.MOSFET,
                _ => throw new NotSupportedException($"Component kind '{kind}' not mapped to ElementType.")
            };
        }

        /// <summary>
        /// Gets the SPICE element prefix for a component kind.
        /// </summary>
        public static string GetElementPrefix(ComponentKind kind)
        {
            return kind switch
            {
                ComponentKind.Resistor => "R",
                ComponentKind.Capacitor => "C",
                ComponentKind.Inductor => "L",
                ComponentKind.VoltageSource => "V",
                ComponentKind.CurrentSource => "I",
                ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode => "D",
                ComponentKind.BJT => "Q",
                ComponentKind.MOSFET => "M",
                _ => "X"
            };
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
            return definition.BJTModel switch
            {
                BJTModel._2N2222 => "2N2222",
                BJTModel._2N3904 => "2N3904",
                BJTModel._2N3906 => "2N3906",
                BJTModel.BC547 => "BC547",
                BJTModel.BC557 => "BC557",
                BJTModel.BC548 => "BC548",
                BJTModel.BC558 => "BC558",
                BJTModel.BC556 => "BC556",
                BJTModel.BC337 => "BC337",
                BJTModel.TIP31 => "TIP31",
                BJTModel.TIP32 => "TIP32",
                BJTModel._2N696 => "2N696",
                BJTModel.Generic_NPN => "Generic_NPN",
                BJTModel.Generic_PNP => "Generic_PNP",
                BJTModel.Custom => "Custom_BJT",
                _ => "2N2222"
            };
        }

        /// <summary>
        /// Gets the SPICE model name from MOSFET model enum.
        /// </summary>
        private static string GetMOSFETModelName(ComponentDefinition definition)
        {
            return definition.MOSFETModel switch
            {
                MOSFETModel._2N7000 => "2N7000",
                MOSFETModel.BS170 => "BS170",
                MOSFETModel.IRF540 => "IRF540",
                MOSFETModel.IRF9540 => "IRF9540",
                MOSFETModel.BS250 => "BS250",
                MOSFETModel.IRF3205 => "IRF3205",
                MOSFETModel.IRF530 => "IRF530",
                MOSFETModel.IRLZ44N => "IRLZ44N",
                MOSFETModel.IRF520 => "IRF520",
                MOSFETModel.BSS84 => "BSS84",
                MOSFETModel.TP0610L => "TP0610L",
                MOSFETModel.FQP27P06 => "FQP27P06",
                MOSFETModel.Generic_NMOS => "Generic_NMOS",
                MOSFETModel.Generic_PMOS => "Generic_PMOS",
                MOSFETModel.Custom => "Custom_MOSFET",
                _ => "NMOS"
            };
        }

        /// <summary>
        /// Gets the SPICE model name from Diode model enum.
        /// </summary>
        private static string GetDiodeModelName(ComponentDefinition definition)
        {
            return definition.DiodeModel switch
            {
                DiodeModel._1N4148 => "1N4148",
                DiodeModel._1N4001 => "1N4001",
                DiodeModel._1N5819 => "1N5819",
                DiodeModel._1N4007 => "1N4007",
                DiodeModel._1N914 => "1N914",
                DiodeModel._1N4002 => "1N4002",
                DiodeModel._1N4004 => "1N4004",
                DiodeModel._1N5408 => "1N5408",
                DiodeModel.BAS40 => "BAS40",
                DiodeModel.BAT85 => "BAT85",
                DiodeModel.LED_Red => "LED_Red",
                DiodeModel.LED_Green => "LED_Green",
                DiodeModel.LED_Blue => "LED_Blue",
                DiodeModel.LED_White => "LED_White",
                DiodeModel.LED_Yellow => "LED_Yellow",
                DiodeModel.Zener_3V3 => "1N4728A",
                DiodeModel.Zener_3V6 => "1N4729A",
                DiodeModel.Zener_3V9 => "1N4730A",
                DiodeModel.Zener_4V3 => "1N4731A",
                DiodeModel.Zener_4V7 => "1N4732A",
                DiodeModel.Zener_5V1 => "1N4733A",
                DiodeModel.Zener_5V6 => "1N4734A",
                DiodeModel.Zener_6V8 => "1N4736A",
                DiodeModel.Zener_8V2 => "1N4738A",
                DiodeModel.Zener_9V1 => "1N4739A",
                DiodeModel.Zener_12V => "1N4742A",
                DiodeModel.Zener_15V => "1N4744A",
                DiodeModel.Zener_27V => "1N4750A",
                DiodeModel.Generic => "Generic_Diode",
                DiodeModel.Custom => "Custom_Diode",
                _ => "1N4148"
            };
        }
    }
}
