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
        private readonly List<Net> _nets = new List<Net>();
        private int _nextComponentId = 1;
        private int _nextNetId = 1;

        /// <summary>Gets the board boundaries.</summary>
        public BoardBounds Bounds { get; }

        /// <summary>Gets the read-only list of placed components.</summary>
        public IReadOnlyList<PlacedComponent> Components => _components.AsReadOnly();

        /// <summary>Gets the read-only list of nets.</summary>
        public IReadOnlyList<Net> Nets => _nets.AsReadOnly();

        /// <summary>Event raised when a component is placed on the board.</summary>
        public event Action<PlacedComponent> OnComponentPlaced;

        /// <summary>Event raised when a component is removed from the board.</summary>
        public event Action<int> OnComponentRemoved;

        /// <summary>Event raised when a net is created.</summary>
        public event Action<Net> OnNetCreated;

        /// <summary>Event raised when two pins are connected via a net.</summary>
        public event Action<int, PinReference, PinReference> OnPinsConnected;

        /// <summary>
        /// Creates a new board state.
        /// </summary>
        /// <param name="width">Board width in grid cells.</param>
        /// <param name="height">Board height in grid cells.</param>
        public BoardState(int width, int height)
        {
            Bounds = new BoardBounds(width, height);
        }

        /// <summary>
        /// Places a component on the board.
        /// </summary>
        /// <param name="componentDefId">Component definition ID.</param>
        /// <param name="position">Position on the board.</param>
        /// <param name="rotation">Rotation (0, 90, 180, 270).</param>
        /// <param name="pins">Pin instances for this component.</param>
        /// <returns>The placed component.</returns>
        public PlacedComponent PlaceComponent(string componentDefId, GridPosition position, 
                                               int rotation, IEnumerable<PinInstance> pins)
        {
            if (!Bounds.Contains(position))
                throw new ArgumentException($"Position {position} is outside board bounds.");

            var instanceId = _nextComponentId++;
            var component = new PlacedComponent(instanceId, componentDefId, position, rotation, pins);
            _components.Add(component);

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

        _components.Remove(component);
        OnComponentRemoved?.Invoke(instanceId);
        return true;
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
            return _components.Any(c => c.Position.Equals(pos));
        }

        /// <summary>
        /// Returns a string representation of the board state.
        /// </summary>
        public override string ToString()
        {
            return $"Board[{Bounds}] {_components.Count} components, {_nets.Count} nets";
        }
    }
}
