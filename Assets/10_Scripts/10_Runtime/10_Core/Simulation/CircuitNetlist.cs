using System;
using System.Collections.Generic;

namespace CircuitCraft.Simulation
{
    /// <summary>
    /// Type of circuit element for netlist generation.
    /// </summary>
    public enum ElementType
    {
        Resistor,
        Capacitor,
        Inductor,
        VoltageSource,
        CurrentSource,
        Diode,
        BJT,
        MOSFET,
        Subcircuit
    }

    /// <summary>
    /// Represents a circuit element in the netlist.
    /// </summary>
    [Serializable]
    public class NetlistElement
    {
        /// <summary>Unique identifier for this element (e.g., "R1", "V1").</summary>
        public string Id { get; set; }

        /// <summary>Type of circuit element.</summary>
        public ElementType Type { get; set; }

        /// <summary>Node names this element connects to.</summary>
        public List<string> Nodes { get; set; } = new List<string>();

        /// <summary>Primary value (resistance, capacitance, voltage, etc.).</summary>
        public double Value { get; set; }

        /// <summary>Optional model name for semiconductors.</summary>
        public string ModelName { get; set; }

        /// <summary>Optional additional parameters.</summary>
        public Dictionary<string, double> Parameters { get; set; } = new Dictionary<string, double>();

        /// <summary>Maximum rated current in amps (for safety checking).</summary>
        public double? MaxCurrentAmps { get; set; }

        /// <summary>Maximum rated power in watts (for safety checking).</summary>
        public double? MaxPowerWatts { get; set; }

        public NetlistElement() { }

        public NetlistElement(string id, ElementType type, double value, params string[] nodes)
        {
            Id = id;
            Type = type;
            Value = value;
            Nodes = new List<string>(nodes);
        }

        /// <summary>Creates a resistor element.</summary>
        public static NetlistElement Resistor(string id, string nodeA, string nodeB, double ohms, double? maxPowerWatts = null)
        {
            return new NetlistElement
            {
                Id = id,
                Type = ElementType.Resistor,
                Value = ohms,
                Nodes = new List<string> { nodeA, nodeB },
                MaxPowerWatts = maxPowerWatts
            };
        }

        /// <summary>Creates a capacitor element.</summary>
        public static NetlistElement Capacitor(string id, string nodeA, string nodeB, double farads)
        {
            return new NetlistElement
            {
                Id = id,
                Type = ElementType.Capacitor,
                Value = farads,
                Nodes = new List<string> { nodeA, nodeB }
            };
        }

        /// <summary>Creates a DC voltage source element.</summary>
        public static NetlistElement VoltageSource(string id, string nodePositive, string nodeNegative, double volts)
        {
            return new NetlistElement
            {
                Id = id,
                Type = ElementType.VoltageSource,
                Value = volts,
                Nodes = new List<string> { nodePositive, nodeNegative }
            };
        }

        /// <summary>Creates a DC current source element.</summary>
        public static NetlistElement CurrentSource(string id, string nodePositive, string nodeNegative, double amps)
        {
            return new NetlistElement
            {
                Id = id,
                Type = ElementType.CurrentSource,
                Value = amps,
                Nodes = new List<string> { nodePositive, nodeNegative }
            };
        }
    }

    /// <summary>
    /// Defines what to measure during simulation.
    /// </summary>
    [Serializable]
    public class ProbeDefinition
    {
        /// <summary>Unique identifier for this probe.</summary>
        public string Id { get; set; }

        /// <summary>Type of measurement.</summary>
        public ProbeType Type { get; set; }

        /// <summary>Target node or element ID.</summary>
        public string Target { get; set; }

        /// <summary>Optional reference node for voltage measurements.</summary>
        public string ReferenceNode { get; set; }

        public ProbeDefinition() { }

        public ProbeDefinition(string id, ProbeType type, string target, string referenceNode = null)
        {
            Id = id;
            Type = type;
            Target = target;
            ReferenceNode = referenceNode;
        }

        /// <summary>Creates a voltage probe at a node (referenced to ground).</summary>
        public static ProbeDefinition Voltage(string id, string node, string reference = "0")
        {
            return new ProbeDefinition(id, ProbeType.Voltage, node, reference);
        }

        /// <summary>Creates a current probe for an element.</summary>
        public static ProbeDefinition Current(string id, string elementId)
        {
            return new ProbeDefinition(id, ProbeType.Current, elementId);
        }

        /// <summary>Creates a power probe for an element.</summary>
        public static ProbeDefinition Power(string id, string elementId)
        {
            return new ProbeDefinition(id, ProbeType.Power, elementId);
        }
    }

    /// <summary>
    /// Type of probe measurement.
    /// </summary>
    public enum ProbeType
    {
        /// <summary>Voltage at a node (relative to reference).</summary>
        Voltage,
        
        /// <summary>Current through an element.</summary>
        Current,
        
        /// <summary>Power dissipated in an element.</summary>
        Power
    }

    /// <summary>
    /// Represents a complete circuit description for simulation.
    /// Domain DTO - no SpiceSharp or Unity dependencies.
    /// </summary>
    [Serializable]
    public class CircuitNetlist
    {
        /// <summary>
        /// Optional title/name for the circuit.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// List of circuit elements (resistors, sources, etc.).
        /// </summary>
        public List<NetlistElement> Elements { get; set; } = new List<NetlistElement>();

        /// <summary>
        /// List of probes defining what to measure.
        /// </summary>
        public List<ProbeDefinition> Probes { get; set; } = new List<ProbeDefinition>();

        /// <summary>
        /// Ground node name (default is "0").
        /// </summary>
        public string GroundNode { get; set; } = "0";

        /// <summary>
        /// Adds an element to the netlist.
        /// </summary>
        public CircuitNetlist AddElement(NetlistElement element)
        {
            Elements.Add(element);
            return this;
        }

        /// <summary>
        /// Adds a probe to the netlist.
        /// </summary>
        public CircuitNetlist AddProbe(ProbeDefinition probe)
        {
            Probes.Add(probe);
            return this;
        }

        /// <summary>
        /// Gets all unique node names in the circuit.
        /// </summary>
        public HashSet<string> GetNodes()
        {
            var nodes = new HashSet<string>();
            foreach (var element in Elements)
            {
                foreach (var node in element.Nodes)
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }
    }
}
