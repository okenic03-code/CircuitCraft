using System;
using System.Collections.Generic;
using System.Linq;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents the complete state of the circuit board.
    /// This is the central domain model containing all placed components and nets.
    /// </summary>
    public class BoardState
    {
        private readonly List<PlacedComponent> _components = new List<PlacedComponent>();
        private readonly Dictionary<GridPosition, PlacedComponent> _componentsByPosition = new Dictionary<GridPosition, PlacedComponent>();
        private readonly List<Net> _nets = new List<Net>();
        private readonly List<TraceSegment> _traces = new List<TraceSegment>();
        private readonly IReadOnlyList<PlacedComponent> _readOnlyComponents;
        private readonly IReadOnlyList<TraceSegment> _readOnlyTraces;
        private int _nextComponentId = 1;
        private int _nextNetId = 1;
        private int _nextTraceId = 1;

        /// <summary>Gets the suggested board area for initial framing and UX.</summary>
        public BoardBounds SuggestedBounds { get; }

        /// <summary>Gets the suggested board area for backward compatibility.</summary>
        public BoardBounds Bounds => SuggestedBounds;

        /// <summary>Gets the read-only list of placed components.</summary>
        public IReadOnlyList<PlacedComponent> Components => _readOnlyComponents;

        /// <summary>Gets the read-only list of nets.</summary>
        public IReadOnlyList<Net> Nets => _nets.AsReadOnly();

        /// <summary>Gets the read-only list of traces.</summary>
        public IReadOnlyList<TraceSegment> Traces => _readOnlyTraces;

        /// <summary>Event raised when a component is placed on the board.</summary>
        public event Action<PlacedComponent> OnComponentPlaced;

        /// <summary>Event raised when a component is removed from the board.</summary>
        public event Action<int> OnComponentRemoved;

        /// <summary>Event raised when a net is created.</summary>
        public event Action<Net> OnNetCreated;

        /// <summary>Event raised when two pins are connected via a net.</summary>
        public event Action<int, PinReference, PinReference> OnPinsConnected;

        /// <summary>Event raised when a trace is added to the board.</summary>
        public event Action<TraceSegment> OnTraceAdded;

        /// <summary>Event raised when a trace is removed from the board.</summary>
        public event Action<int> OnTraceRemoved;

        /// <summary>
        /// Creates a new board state.
        /// </summary>
        /// <param name="width">Board width in grid cells.</param>
        /// <param name="height">Board height in grid cells.</param>
        public BoardState(int width, int height)
        {
            SuggestedBounds = new BoardBounds(width, height);
            _readOnlyComponents = _components.AsReadOnly();
            _readOnlyTraces = _traces.AsReadOnly();
        }

        /// <summary>
        /// Places a component on the board.
        /// </summary>
        /// <param name="componentDefId">Component definition ID.</param>
        /// <param name="position">Position on the board.</param>
        /// <param name="rotation">Rotation (0, 90, 180, 270).</param>
        /// <param name="pins">Pin instances for this component.</param>
        /// <param name="customValue">User-specified custom electrical value (null to use definition default).</param>
        /// <returns>The placed component.</returns>
        public PlacedComponent PlaceComponent(string componentDefId, GridPosition position, 
                                               int rotation, IEnumerable<PinInstance> pins, float? customValue = null)
        {
            if (_componentsByPosition.ContainsKey(position))
                throw new InvalidOperationException($"Position {position} is already occupied.");

            var instanceId = _nextComponentId++;
            var component = new PlacedComponent(instanceId, componentDefId, position, rotation, pins, customValue);
            _components.Add(component);
            _componentsByPosition.Add(position, component);

            OnComponentPlaced?.Invoke(component);
            return component;
        }

        /// <summary>
        /// Removes a component from the board.
        /// </summary>
        /// <param name="instanceId">Component instance ID.</param>
        /// <returns>True if component was removed.</returns>
        public bool RemoveComponent(int instanceId)
        {
            var component = _components.FirstOrDefault(c => c.InstanceId == instanceId);
            if (component == null)
                return false;

            var removedPinPositions = new HashSet<GridPosition>();
            foreach (var pin in component.Pins)
            {
                removedPinPositions.Add(component.GetPinWorldPosition(pin.PinIndex));
            }

            // Remove component's pins from all nets
            var netsToCheck = new List<Net>();
            foreach (var net in _nets)
            {
                var pinsToRemove = net.ConnectedPins
                    .Where(p => p.ComponentInstanceId == instanceId)
                    .ToList();
                foreach (var pin in pinsToRemove)
                {
                    net.RemovePin(pin);
                }

                // Track nets that might now be empty
                if (pinsToRemove.Count > 0)
                {
                    netsToCheck.Add(net);
                }
            }

            // Remove empty nets
            foreach (var net in netsToCheck)
            {
                if (net.ConnectedPins.Count == 0)
                {
                    _nets.Remove(net);
                }
            }

            // Remove any traces that start or end on removed component pins.
            for (int i = _traces.Count - 1; i >= 0; i--)
            {
                var trace = _traces[i];
                if (removedPinPositions.Contains(trace.Start) || removedPinPositions.Contains(trace.End))
                {
                    RemoveTrace(trace.SegmentId);
                }
            }

            _components.Remove(component);
            _componentsByPosition.Remove(component.Position);
            OnComponentRemoved?.Invoke(instanceId);
            return true;
        }

        /// <summary>
        /// Adds a trace segment to an existing net.
        /// </summary>
        /// <param name="netId">Target net ID.</param>
        /// <param name="start">Trace start position.</param>
        /// <param name="end">Trace end position.</param>
        /// <returns>The created trace segment.</returns>
        public TraceSegment AddTrace(int netId, GridPosition start, GridPosition end)
        {
            var net = GetNet(netId);
            if (net == null)
                throw new ArgumentException($"Net {netId} not found.", nameof(netId));

            var trace = new TraceSegment(_nextTraceId++, netId, start, end);
            _traces.Add(trace);
            OnTraceAdded?.Invoke(trace);
            return trace;
        }

        /// <summary>
        /// Computes the axis-aligned bounding rectangle that contains all placed components and trace endpoints.
        /// Returns SuggestedBounds when the board has no content.
        /// </summary>
        /// <returns>Computed content bounds or SuggestedBounds when empty.</returns>
        public BoardBounds ComputeContentBounds()
        {
            var positions = new List<GridPosition>();

            foreach (var component in _components)
            {
                positions.Add(component.Position);
                foreach (var pin in component.Pins)
                {
                    positions.Add(component.GetPinWorldPosition(pin.PinIndex));
                }
            }

            foreach (var trace in _traces)
            {
                positions.Add(trace.Start);
                positions.Add(trace.End);
            }

            if (positions.Count == 0)
                return SuggestedBounds;

            return BoardBounds.FromContent(positions);
        }

        /// <summary>
        /// Removes a trace segment by ID.
        /// </summary>
        /// <param name="segmentId">Trace segment ID.</param>
        /// <returns>True when removed.</returns>
        public bool RemoveTrace(int segmentId)
        {
            var trace = _traces.FirstOrDefault(t => t.SegmentId == segmentId);
            if (trace == null)
                return false;

            _traces.Remove(trace);
            OnTraceRemoved?.Invoke(segmentId);

            // If the net has no remaining traces, clear pin links and remove it.
            if (!_traces.Any(t => t.NetId == trace.NetId))
            {
                var net = GetNet(trace.NetId);
                if (net != null)
                {
                    foreach (var pin in net.ConnectedPins.ToList())
                    {
                        var component = GetComponent(pin.ComponentInstanceId);
                        var pinInstance = component?.Pins.FirstOrDefault(p => p.PinIndex == pin.PinIndex);
                        if (pinInstance != null && pinInstance.ConnectedNetId == net.NetId)
                        {
                            pinInstance.ConnectedNetId = null;
                        }

                        net.RemovePin(pin);
                    }

                    _nets.Remove(net);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all trace segments that belong to the specified net.
        /// </summary>
        /// <param name="netId">Net ID.</param>
        /// <returns>Matching trace segments.</returns>
        public IReadOnlyList<TraceSegment> GetTraces(int netId)
        {
            return _traces.Where(t => t.NetId == netId).ToList();
        }

        /// <summary>
        /// Creates a new net.
        /// </summary>
        /// <param name="netName">Name for the net (e.g., "VIN", "GND").</param>
        /// <returns>The created net.</returns>
        public Net CreateNet(string netName)
        {
            var netId = _nextNetId++;
            var net = new Net(netId, netName);
            _nets.Add(net);

            OnNetCreated?.Invoke(net);
            return net;
        }

        /// <summary>
        /// Connects a pin to a net.
        /// </summary>
        /// <param name="netId">Net ID.</param>
        /// <param name="pin">Pin reference to connect.</param>
        public void ConnectPinToNet(int netId, PinReference pin)
        {
            var net = GetNet(netId);
            if (net == null)
                throw new ArgumentException($"Net {netId} not found.", nameof(netId));

            // Update component's pin instance
            var component = GetComponent(pin.ComponentInstanceId);
            if (component == null)
                throw new ArgumentException($"Component {pin.ComponentInstanceId} not found.");

            var pinInstance = component.Pins.FirstOrDefault(p => p.PinIndex == pin.PinIndex);
            if (pinInstance == null)
                throw new ArgumentException($"Pin {pin.PinIndex} not found on component {pin.ComponentInstanceId}.");

            if (pinInstance.ConnectedNetId.HasValue && pinInstance.ConnectedNetId.Value != netId)
            {
                var previousNet = GetNet(pinInstance.ConnectedNetId.Value);
                if (previousNet != null)
                {
                    previousNet.RemovePin(pin);
                    if (previousNet.ConnectedPins.Count == 0)
                    {
                        _nets.Remove(previousNet);
                    }
                }
            }

            pinInstance.ConnectedNetId = netId;
            net.AddPin(pin);

            // Check if this creates a connection between two pins
            var connectedPins = net.ConnectedPins;
            if (connectedPins.Count >= 2)
            {
                var otherPin = connectedPins[connectedPins.Count - 2];
                OnPinsConnected?.Invoke(netId, otherPin, pin);
            }
        }

        /// <summary>
        /// Gets a component by instance ID.
        /// </summary>
        /// <param name="instanceId">Component instance ID.</param>
        /// <returns>The component, or null if not found.</returns>
        public PlacedComponent GetComponent(int instanceId)
        {
            return _components.FirstOrDefault(c => c.InstanceId == instanceId);
        }

        /// <summary>
        /// Gets a net by ID.
        /// </summary>
        /// <param name="netId">Net ID.</param>
        /// <returns>The net, or null if not found.</returns>
        public Net GetNet(int netId)
        {
            return _nets.FirstOrDefault(n => n.NetId == netId);
        }

        /// <summary>
        /// Gets a net by name.
        /// </summary>
        /// <param name="netName">Net name (e.g., "VIN", "GND").</param>
        /// <returns>The net, or null if not found.</returns>
        public Net GetNetByName(string netName)
        {
            return _nets.FirstOrDefault(n => n.NetName == netName);
        }

        /// <summary>
        /// Checks if a position is occupied by a component.
        /// </summary>
        /// <param name="pos">Position to check.</param>
        /// <returns>True if position is occupied.</returns>
        public bool IsPositionOccupied(GridPosition pos)
        {
            return _componentsByPosition.ContainsKey(pos);
        }

        /// <summary>
        /// Gets a component at a specific board position.
        /// </summary>
        /// <param name="position">Position to query.</param>
        /// <returns>The component at the position, or null if unoccupied.</returns>
        public PlacedComponent GetComponentAt(GridPosition position)
        {
            _componentsByPosition.TryGetValue(position, out var component);
            return component;
        }

        /// <summary>
        /// Returns a string representation of the board state.
        /// </summary>
        public override string ToString()
        {
            return $"Board[{SuggestedBounds}] {_components.Count} components, {_nets.Count} nets";
        }
    }
}
