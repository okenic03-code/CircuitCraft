using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CircuitCraft.Commands")]

namespace CircuitCraft.Core
{
    /// <summary>
    /// Represents an electrical net connecting multiple component pins.
    /// A net is a set of electrically connected points in the circuit.
    /// </summary>
    public class Net
    {
        private readonly List<PinReference> _connectedPins = new();

        /// <summary>Gets the unique identifier for this net.</summary>
        public int NetId { get; }

        /// <summary>Gets the name of this net (e.g., "VIN", "GND", "VOUT").</summary>
        public string NetName { get; internal set; }

        /// <summary>Gets the read-only list of pins connected to this net.</summary>
        public IReadOnlyList<PinReference> ConnectedPins => _connectedPins.AsReadOnly();

        /// <summary>Gets whether this is the ground net.</summary>
        public bool IsGround => NetName == "GND" || NetName == "0";

        /// <summary>Gets whether this is a power net (starts with "V").</summary>
        public bool IsPower => NetName.StartsWith("V");

        /// <summary>
        /// Creates a new net.
        /// </summary>
        /// <param name="netId">Unique net identifier.</param>
        /// <param name="netName">Net name (e.g., "VIN", "GND").</param>
        public Net(int netId, string netName)
        {
            if (string.IsNullOrWhiteSpace(netName))
                throw new ArgumentException("Net name cannot be null or empty.", nameof(netName));

            NetId = netId;
            NetName = netName;
        }

        /// <summary>
        /// Adds a pin to this net.
        /// </summary>
        /// <param name="pin">The pin reference to add.</param>
        public void AddPin(PinReference pin)
        {
            if (!_connectedPins.Contains(pin))
            {
                _connectedPins.Add(pin);
            }
        }

        /// <summary>
        /// Removes a pin from this net.
        /// </summary>
        /// <param name="pin">The pin reference to remove.</param>
        /// <returns>True if the pin was removed, false if it wasn't in the net.</returns>
        public bool RemovePin(PinReference pin)
        {
            return _connectedPins.Remove(pin);
        }

        /// <summary>
        /// Checks if a specific pin is connected to this net.
        /// </summary>
        /// <param name="componentInstanceId">Component instance ID.</param>
        /// <param name="pinIndex">Pin index.</param>
        /// <returns>True if the pin is in this net.</returns>
        public bool ContainsPin(int componentInstanceId, int pinIndex)
        {
            foreach (var pin in _connectedPins)
            {
                if (pin.ComponentInstanceId == componentInstanceId && pin.PinIndex == pinIndex)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a string representation of this net.
        /// </summary>
        public override string ToString()
        {
            return $"Net[{NetId}:{NetName}] ({_connectedPins.Count} pins)";
        }
    }
}
