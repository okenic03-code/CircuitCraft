using System;
using System.Collections.Generic;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace CircuitCraft.Simulation.SpiceSharp
{
    /// <summary>
    /// Converts domain CircuitNetlist to SpiceSharp Circuit objects.
    /// Infrastructure adapter - bridges domain DTOs to SpiceSharp types.
    /// </summary>
    public class NetlistBuilder
    {
        /// <summary>
        /// Builds a SpiceSharp Circuit from a domain CircuitNetlist.
        /// </summary>
        /// <param name="netlist">The domain netlist description.</param>
        /// <returns>A SpiceSharp Circuit ready for simulation.</returns>
        /// <exception cref="ArgumentNullException">If netlist is null.</exception>
        /// <exception cref="InvalidOperationException">If netlist contains invalid elements.</exception>
        public Circuit Build(CircuitNetlist netlist)
        {
            if (netlist == null)
                throw new ArgumentNullException(nameof(netlist));

            var circuit = new Circuit();

            foreach (var element in netlist.Elements)
            {
                var component = CreateComponent(element);
                if (component != null)
                {
                    circuit.Add(component);
                }
            }

            return circuit;
        }

        /// <summary>
        /// Creates a SpiceSharp component from a domain element definition.
        /// </summary>
        private IEntity CreateComponent(NetlistElement element)
        {
            if (element == null || string.IsNullOrEmpty(element.Id))
                return null;

            // Validate we have enough nodes
            if (element.Nodes == null || element.Nodes.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Element '{element.Id}' must have at least 2 nodes");
            }

            var nodeA = element.Nodes[0];
            var nodeB = element.Nodes[1];

            switch (element.Type)
            {
                case ElementType.Resistor:
                    return CreateResistor(element.Id, nodeA, nodeB, element.Value);

                case ElementType.Capacitor:
                    return CreateCapacitor(element.Id, nodeA, nodeB, element.Value);

                case ElementType.Inductor:
                    return CreateInductor(element.Id, nodeA, nodeB, element.Value);

                case ElementType.VoltageSource:
                    return CreateVoltageSource(element.Id, nodeA, nodeB, element.Value);

                case ElementType.CurrentSource:
                    return CreateCurrentSource(element.Id, nodeA, nodeB, element.Value);

                case ElementType.Diode:
                    return CreateDiode(element.Id, nodeA, nodeB, element.ModelName);

                case ElementType.BJT:
                    if (element.Nodes.Count < 3)
                        throw new InvalidOperationException($"BJT '{element.Id}' requires 3 nodes (C, B, E)");
                    return CreateBJT(element.Id, element.Nodes[0], element.Nodes[1], element.Nodes[2], element.ModelName);

                case ElementType.MOSFET:
                    if (element.Nodes.Count < 4)
                        throw new InvalidOperationException($"MOSFET '{element.Id}' requires 4 nodes (D, G, S, B)");
                    return CreateMOSFET(element.Id, element.Nodes[0], element.Nodes[1], 
                        element.Nodes[2], element.Nodes[3], element.ModelName);

                default:
                    throw new NotSupportedException(
                        $"Element type '{element.Type}' is not supported");
            }
        }

        /// <summary>Creates a resistor component.</summary>
        private Resistor CreateResistor(string id, string nodeA, string nodeB, double ohms)
        {
            if (ohms <= 0)
                throw new ArgumentException($"Resistor '{id}' value must be positive, got {ohms}");
            
            return new Resistor(id, nodeA, nodeB, ohms);
        }

        /// <summary>Creates a capacitor component.</summary>
        private Capacitor CreateCapacitor(string id, string nodeA, string nodeB, double farads)
        {
            if (farads <= 0)
                throw new ArgumentException($"Capacitor '{id}' value must be positive, got {farads}");
            
            return new Capacitor(id, nodeA, nodeB, farads);
        }

        /// <summary>Creates an inductor component.</summary>
        private Inductor CreateInductor(string id, string nodeA, string nodeB, double henries)
        {
            if (henries <= 0)
                throw new ArgumentException($"Inductor '{id}' value must be positive, got {henries}");
            
            return new Inductor(id, nodeA, nodeB, henries);
        }

        /// <summary>Creates a DC voltage source component.</summary>
        private VoltageSource CreateVoltageSource(string id, string nodePositive, string nodeNegative, double volts)
        {
            return new VoltageSource(id, nodePositive, nodeNegative, volts);
        }

        /// <summary>Creates a DC current source component.</summary>
        private CurrentSource CreateCurrentSource(string id, string nodePositive, string nodeNegative, double amps)
        {
            return new CurrentSource(id, nodePositive, nodeNegative, amps);
        }

        /// <summary>Creates a diode component with a default model.</summary>
        private Diode CreateDiode(string id, string anode, string cathode, string modelName)
        {
            var diode = new Diode(id, anode, cathode, modelName ?? "D1N4148");
            return diode;
        }

        /// <summary>Creates a BJT transistor.</summary>
        private BipolarJunctionTransistor CreateBJT(string id, string collector, string basee, string emitter, string modelName)
        {
            // SpiceSharp BJT requires a substrate node parameter
            return new BipolarJunctionTransistor(id, collector, basee, emitter, "0", modelName ?? "2N2222");
        }

        /// <summary>Creates a MOSFET transistor.</summary>
        private Mosfet1 CreateMOSFET(string id, string drain, string gate, string source, string bulk, string modelName)
        {
            // SpiceSharp uses Mosfet1, Mosfet2, or Mosfet3 for different model levels
            // Default to Mosfet1 (Level 1 model)
            var mosfet = new Mosfet1(id, drain, gate, source, bulk, modelName ?? "NMOS");
            return mosfet;
        }

        /// <summary>
        /// Validates a netlist for basic correctness before building.
        /// </summary>
        /// <param name="netlist">The netlist to validate.</param>
        /// <returns>List of validation issues found.</returns>
        public List<SimulationIssue> Validate(CircuitNetlist netlist)
        {
            var issues = new List<SimulationIssue>();

            if (netlist == null)
            {
                issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.General, "Netlist is null"));
                return issues;
            }

            if (netlist.Elements == null || netlist.Elements.Count == 0)
            {
                issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.Topology, "Netlist has no elements"));
                return issues;
            }

            // Check for ground node
            var nodes = netlist.GetNodes();
            if (!nodes.Contains(netlist.GroundNode ?? "0"))
            {
                issues.Add(new SimulationIssue(IssueSeverity.Warning, IssueCategory.Topology, 
                    $"Ground node '{netlist.GroundNode ?? "0"}' not found in circuit"));
            }

            // Check for voltage source (required for DC analysis)
            bool hasVoltageSource = false;
            foreach (var element in netlist.Elements)
            {
                if (element.Type == ElementType.VoltageSource)
                {
                    hasVoltageSource = true;
                    break;
                }
            }
            if (!hasVoltageSource)
            {
                issues.Add(new SimulationIssue(IssueSeverity.Warning, IssueCategory.Topology, 
                    "No voltage source in circuit - DC analysis may fail"));
            }

            // Check each element
            foreach (var element in netlist.Elements)
            {
                if (string.IsNullOrEmpty(element.Id))
                {
                    issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.Parameter, 
                        "Element has no ID"));
                    continue;
                }

                if (element.Nodes == null || element.Nodes.Count < 2)
                {
                    issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.Topology, 
                        $"Element '{element.Id}' has fewer than 2 nodes", element.Id));
                    continue;
                }

                // Check for zero/negative values where positive is required
                if ((element.Type == ElementType.Resistor || 
                     element.Type == ElementType.Capacitor || 
                     element.Type == ElementType.Inductor) && element.Value <= 0)
                {
                    issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.Parameter, 
                        $"Element '{element.Id}' has non-positive value: {element.Value}", element.Id));
                }
            }

            return issues;
        }
    }
}
