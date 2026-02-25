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
            if (netlist is null)
                throw new ArgumentNullException(nameof(netlist));

            var circuit = new Circuit();
            var addedModelNames = new HashSet<string>();

            foreach (var element in netlist.Elements)
            {
                var entities = CreateEntities(element);
                foreach (var entity in entities)
                {
                    if (entity is not null)
                    {
                        if (IsSemiconductorModelEntity(entity) && !addedModelNames.Add(entity.Name))
                            continue;

                        circuit.Add(entity);
                    }
                }
            }

            return circuit;
        }

        private static bool IsSemiconductorModelEntity(IEntity entity)
        {
            return entity is DiodeModel
                or BipolarJunctionTransistorModel
                or Mosfet1Model;
        }

        /// <summary>
        /// Creates SpiceSharp entities from a domain element definition.
        /// For semiconductors, may return both component and model.
        /// </summary>
        private IEnumerable<IEntity> CreateEntities(NetlistElement element)
        {
            if (element is null || string.IsNullOrEmpty(element.Id))
                yield break;

            // Validate we have enough nodes
            if (element.Nodes is null || element.Nodes.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Element '{element.Id}' must have at least 2 nodes");
            }

            var nodeA = element.Nodes[0];
            var nodeB = element.Nodes[1];

            switch (element.Type)
            {
                case ElementType.Resistor:
                    yield return CreateResistor(element.Id, nodeA, nodeB, element.Value);
                    break;

                case ElementType.Capacitor:
                    yield return CreateCapacitor(element.Id, nodeA, nodeB, element.Value);
                    break;

                case ElementType.Inductor:
                    yield return CreateInductor(element.Id, nodeA, nodeB, element.Value);
                    break;

                case ElementType.VoltageSource:
                    yield return CreateVoltageSource(element.Id, nodeA, nodeB, element.Value);
                    break;

                case ElementType.CurrentSource:
                    yield return CreateCurrentSource(element.Id, nodeA, nodeB, element.Value);
                    break;

                case ElementType.Diode:
                    foreach (var entity in CreateDiode(element))
                        yield return entity;
                    break;

                case ElementType.BJT:
                    if (element.Nodes.Count < 3)
                        throw new InvalidOperationException($"BJT '{element.Id}' requires 3 nodes (C, B, E)");
                    foreach (var entity in CreateBJT(element))
                        yield return entity;
                    break;

                case ElementType.MOSFET:
                    if (element.Nodes.Count < 4)
                        throw new InvalidOperationException($"MOSFET '{element.Id}' requires 4 nodes (D, G, S, B)");
                    foreach (var entity in CreateMOSFET(element))
                        yield return entity;
                    break;

                default:
                    throw new NotSupportedException(
                        $"Element type '{element.Type}' is not supported");
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        private IEntity CreateComponent(NetlistElement element)
        {
            List<IEntity> entities = new(CreateEntities(element));
            return entities.Count > 0 ? entities[0] : null;
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

        /// <summary>Creates a diode component with optional model parameters.</summary>
        private IEnumerable<IEntity> CreateDiode(NetlistElement element)
        {
            var modelName = !string.IsNullOrEmpty(element.ModelName) ? element.ModelName : "D1N4148";
            var diode = new Diode(element.Id, element.Nodes[0], element.Nodes[1], modelName);
            yield return diode;

            // Create model with parameters if any are specified
            if (element.Parameters.Count > 0)
            {
                var model = new DiodeModel(modelName);
                
                if (element.Parameters.TryGetValue("Is", out var satCurrent))
                    model.Parameters.SaturationCurrent = satCurrent;
                if (element.Parameters.TryGetValue("N", out var emissionCoeff))
                    model.Parameters.EmissionCoefficient = emissionCoeff;
                if (element.Parameters.TryGetValue("BV", out var breakdownVoltage))
                    model.Parameters.BreakdownVoltage = breakdownVoltage;
                if (element.Parameters.TryGetValue("IBV", out var breakdownCurrent))
                    model.Parameters.BreakdownCurrent = breakdownCurrent;
                
                yield return model;
            }
        }

        /// <summary>Creates a BJT transistor with optional model parameters.</summary>
        private IEnumerable<IEntity> CreateBJT(NetlistElement element)
        {
            var modelName = !string.IsNullOrEmpty(element.ModelName) ? element.ModelName : "2N2222";
            // SpiceSharp BJT requires a substrate node parameter
            var bjt = new BipolarJunctionTransistor(element.Id, 
                element.Nodes[0], element.Nodes[1], element.Nodes[2], "0", modelName);
            yield return bjt;

            // Create model with parameters if any are specified
            if (element.Parameters.Count > 0)
            {
                // Determine NPN vs PNP from Value (1 = NPN, -1 = PNP)
                bool isNPN = element.Value >= 0;
                var model = new BipolarJunctionTransistorModel(modelName);
                if (isNPN)
                    model.Parameters.SetNpn(true);
                else
                    model.Parameters.SetPnp(true);
                
                if (element.Parameters.TryGetValue("Bf", out var beta))
                    model.Parameters.BetaF = beta;
                if (element.Parameters.TryGetValue("Vaf", out var earlyVoltage))
                    model.Parameters.EarlyVoltageForward = earlyVoltage;
                
                yield return model;
            }
        }

        /// <summary>Creates a MOSFET transistor with optional model parameters.</summary>
        private IEnumerable<IEntity> CreateMOSFET(NetlistElement element)
        {
            var modelName = !string.IsNullOrEmpty(element.ModelName) ? element.ModelName : "NMOS";
            // SpiceSharp uses Mosfet1, Mosfet2, or Mosfet3 for different model levels
            // Default to Mosfet1 (Level 1 model)
            var mosfet = new Mosfet1(element.Id, 
                element.Nodes[0], element.Nodes[1], element.Nodes[2], element.Nodes[3], modelName);
            yield return mosfet;

            // Create model with parameters if any are specified
            if (element.Parameters.Count > 0)
            {
                // Determine NMOS vs PMOS from Value (1 = NMOS, -1 = PMOS)
                bool isNChannel = element.Value >= 0;
                var model = new Mosfet1Model(modelName);
                if (isNChannel)
                    model.Parameters.SetNmos(true);
                else
                    model.Parameters.SetPmos(true);
                
                if (element.Parameters.TryGetValue("Vto", out var thresholdVoltage))
                    model.Parameters.Vt0 = thresholdVoltage;
                if (element.Parameters.TryGetValue("Kp", out var transconductance))
                    model.Parameters.Transconductance = transconductance;
                
                yield return model;
            }
        }

        /// <summary>
        /// Validates a netlist for basic correctness before building.
        /// </summary>
        /// <param name="netlist">The netlist to validate.</param>
        /// <returns>List of validation issues found.</returns>
        public List<SimulationIssue> Validate(CircuitNetlist netlist)
        {
            List<SimulationIssue> issues = new();

            if (netlist is null)
            {
                issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.General, "Netlist is null"));
                return issues;
            }

            if (netlist.Elements is null || netlist.Elements.Count == 0)
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

                if (element.Nodes is null || element.Nodes.Count < 2)
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
