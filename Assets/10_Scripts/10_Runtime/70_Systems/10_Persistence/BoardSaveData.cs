using System;
using System.Collections.Generic;

namespace CircuitCraft.Systems
{
    /// <summary>
    /// Top-level DTO for serializing a player's board solution.
    /// All inner types use public fields for JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public class BoardSaveData
    {
        /// <summary>Which stage this solution is for.</summary>
        public string stageId;

        /// <summary>Suggested board width in grid cells (not a hard limit).</summary>
        public int boardWidth;

        /// <summary>Suggested board height in grid cells (not a hard limit).</summary>
        public int boardHeight;

        /// <summary>All placed components.</summary>
        public List<ComponentSaveData> components = new List<ComponentSaveData>();

        /// <summary>All trace segments.</summary>
        public List<TraceSaveData> traces = new List<TraceSaveData>();

        /// <summary>All nets.</summary>
        public List<NetSaveData> nets = new List<NetSaveData>();

        /// <summary>All pin-to-net connections.</summary>
        public List<PinConnectionSaveData> pinConnections = new List<PinConnectionSaveData>();
    }

    /// <summary>
    /// Serializable data for a placed component.
    /// </summary>
    [Serializable]
    public class ComponentSaveData
    {
        /// <summary>Runtime instance ID (used for cross-referencing pin connections).</summary>
        public int instanceId;

        /// <summary>Component definition ID (e.g., "resistor_1k").</summary>
        public string componentDefId;

        /// <summary>Grid X position.</summary>
        public int positionX;

        /// <summary>Grid Y position.</summary>
        public int positionY;

        /// <summary>Rotation in degrees (0, 90, 180, 270).</summary>
        public int rotation;

        /// <summary>Pin instances belonging to this component.</summary>
        public List<PinInstanceSaveData> pins = new List<PinInstanceSaveData>();
    }

    /// <summary>
    /// Serializable data for a pin instance on a component.
    /// </summary>
    [Serializable]
    public class PinInstanceSaveData
    {
        /// <summary>Pin index (0-based).</summary>
        public int pinIndex;

        /// <summary>Pin name (e.g., "anode", "cathode").</summary>
        public string pinName;

        /// <summary>Local X position relative to component origin.</summary>
        public int localPositionX;

        /// <summary>Local Y position relative to component origin.</summary>
        public int localPositionY;
    }

    /// <summary>
    /// Serializable data for a trace segment.
    /// </summary>
    [Serializable]
    public class TraceSaveData
    {
        /// <summary>Net this trace belongs to.</summary>
        public int netId;

        /// <summary>Start X position.</summary>
        public int startX;

        /// <summary>Start Y position.</summary>
        public int startY;

        /// <summary>End X position.</summary>
        public int endX;

        /// <summary>End Y position.</summary>
        public int endY;
    }

    /// <summary>
    /// Serializable data for a net.
    /// </summary>
    [Serializable]
    public class NetSaveData
    {
        /// <summary>Unique net identifier.</summary>
        public int netId;

        /// <summary>Net name (e.g., "VIN", "GND").</summary>
        public string netName;
    }

    /// <summary>
    /// Serializable data for a pin-to-net connection.
    /// </summary>
    [Serializable]
    public class PinConnectionSaveData
    {
        /// <summary>Component instance this pin belongs to.</summary>
        public int componentInstanceId;

        /// <summary>Pin index on the component.</summary>
        public int pinIndex;

        /// <summary>Net this pin is connected to.</summary>
        public int netId;

        /// <summary>World X position of the pin (needed for PinReference reconstruction).</summary>
        public int pinWorldX;

        /// <summary>World Y position of the pin (needed for PinReference reconstruction).</summary>
        public int pinWorldY;
    }
}
