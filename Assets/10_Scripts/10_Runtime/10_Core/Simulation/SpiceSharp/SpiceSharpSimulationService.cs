using System;
using System.Threading;
using System.Threading.Tasks;
using SpiceSharp;

namespace CircuitCraft.Simulation.SpiceSharp
{
    /// <summary>
    /// Implementation of ISimulationService using SpiceSharp.
    /// Infrastructure adapter - primary entry point for circuit simulation.
    /// </summary>
    public class SpiceSharpSimulationService : ISimulationService
    {
        private readonly NetlistBuilder _netlistBuilder;
        private readonly SimulationRunner _simulationRunner;

        /// <summary>
        /// Creates a new SpiceSharp simulation service.
        /// </summary>
        public SpiceSharpSimulationService()
        {
            _netlistBuilder = new NetlistBuilder();
            _simulationRunner = new SimulationRunner(_netlistBuilder);
        }

        /// <summary>
        /// Creates a new SpiceSharp simulation service with custom components.
        /// </summary>
        /// <param name="netlistBuilder">Custom netlist builder.</param>
        /// <param name="simulationRunner">Custom simulation runner.</param>
        public SpiceSharpSimulationService(NetlistBuilder netlistBuilder, SimulationRunner simulationRunner)
        {
            _netlistBuilder = netlistBuilder ?? new NetlistBuilder();
            _simulationRunner = simulationRunner ?? new SimulationRunner(_netlistBuilder);
        }

        /// <inheritdoc/>
        public SimulationResult Run(SimulationRequest request)
        {
            if (request == null)
            {
                return SimulationResult.Failure(SimulationType.DCOperatingPoint, 
                    SimulationStatus.InvalidCircuit, "Request is null");
            }

            if (request.Netlist == null)
            {
                return SimulationResult.Failure(request.SimulationType, 
                    SimulationStatus.InvalidCircuit, "Netlist is null");
            }

            // Validate netlist first
            var validationResult = Validate(request.Netlist);
            if (validationResult.HasErrors)
            {
                validationResult.SimulationType = request.SimulationType;
                validationResult.Tag = request.Tag;
                return validationResult;
            }

            try
            {
                // Build SpiceSharp circuit
                var circuit = _netlistBuilder.Build(request.Netlist);

                // Run appropriate simulation type
                SimulationResult result;
                switch (request.SimulationType)
                {
                    case SimulationType.DCOperatingPoint:
                        result = _simulationRunner.RunDCOperatingPoint(circuit, request.Netlist);
                        break;

                    case SimulationType.Transient:
                        if (request.TransientConfig == null)
                        {
                            return SimulationResult.Failure(SimulationType.Transient,
                                SimulationStatus.InvalidCircuit, "Transient config is required for transient analysis");
                        }
                        result = _simulationRunner.RunTransient(circuit, request.Netlist, request.TransientConfig);
                        break;

                    case SimulationType.DCSweep:
                        if (request.DCSweepConfig == null)
                        {
                            return SimulationResult.Failure(SimulationType.DCSweep,
                                SimulationStatus.InvalidCircuit, "DC Sweep config is required");
                        }
                        result = _simulationRunner.RunDCSweep(circuit, request.Netlist, request.DCSweepConfig);
                        break;

                    default:
                        return SimulationResult.Failure(request.SimulationType,
                            SimulationStatus.InvalidCircuit, $"Simulation type '{request.SimulationType}' not yet supported");
                }

                // Check safety limits if enabled
                if (request.IsSafetyChecksEnabled)
                {
                    _simulationRunner.CheckSafetyLimits(request.Netlist, result);
                }

                result.Tag = request.Tag;
                return result;
            }
            catch (Exception ex)
            {
                var result = SimulationResult.Failure(request.SimulationType,
                    SimulationStatus.Error, $"Simulation failed: {ex.Message}");
                result.Tag = request.Tag;
                result.Issues.Add(new SimulationIssue(IssueSeverity.Error, IssueCategory.General, ex.Message));
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<SimulationResult> RunAsync(SimulationRequest request, CancellationToken cancellationToken = default)
        {
            // Run simulation on thread pool to avoid blocking Unity main thread
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Run(request);
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public SimulationResult Validate(CircuitNetlist netlist)
        {
            var issues = _netlistBuilder.Validate(netlist);
            
            var result = new SimulationResult
            {
                HasRun = false,
                SimulationType = SimulationType.DCOperatingPoint
            };

            if (issues.Count == 0)
            {
                result.Status = SimulationStatus.Success;
                result.StatusMessage = "Validation passed";
            }
            else
            {
                result.Issues.AddRange(issues);
                
                // Check if any are errors
                bool hasErrors = false;
                foreach (var issue in issues)
                {
                    if (issue.Severity == IssueSeverity.Error)
                    {
                        hasErrors = true;
                        break;
                    }
                }

                if (hasErrors)
                {
                    result.Status = SimulationStatus.InvalidCircuit;
                    result.StatusMessage = $"Validation failed with {issues.Count} issue(s)";
                }
                else
                {
                    result.Status = SimulationStatus.CompletedWithWarnings;
                    result.StatusMessage = $"Validation passed with {issues.Count} warning(s)";
                }
            }

            return result;
        }
    }
}
