using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CircuitCraft.Core;
using UnityEngine;

namespace CircuitCraft.Systems
{
    /// <summary>
    /// Pure C# service for serializing and deserializing player board solutions.
    /// Converts between <see cref="BoardState"/> and JSON via <see cref="BoardSaveData"/> DTOs.
    /// </summary>
    public class SaveLoadService
    {
        /// <summary>
        /// Serializes the current board state to a JSON string.
        /// </summary>
        /// <param name="boardState">The board state to serialize.</param>
        /// <param name="stageId">The stage this solution belongs to.</param>
        /// <returns>JSON string representation of the board solution.</returns>
        public string Serialize(BoardState boardState, string stageId)
        {
            if (boardState == null)
                throw new ArgumentNullException(nameof(boardState));
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("Stage ID cannot be null or empty.", nameof(stageId));

            var data = ConvertToSaveData(boardState, stageId);
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Deserializes a JSON string back to a <see cref="BoardSaveData"/> DTO.
        /// </summary>
        /// <param name="json">JSON string to parse.</param>
        /// <returns>Deserialized board save data.</returns>
        public BoardSaveData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            return JsonUtility.FromJson<BoardSaveData>(json);
        }

        /// <summary>
        /// Restores a <see cref="BoardSaveData"/> DTO into a <see cref="BoardState"/>.
        /// The provided board state should be empty (freshly constructed).
        /// Restoration order: components -> nets -> traces -> pin connections.
        /// </summary>
        /// <param name="boardState">Target board state (should be empty).</param>
        /// <param name="data">Save data to restore from.</param>
        public void RestoreToBoard(BoardState boardState, BoardSaveData data)
        {
            if (boardState == null)
                throw new ArgumentNullException(nameof(boardState));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Maps from saved IDs to newly assigned IDs (BoardState auto-assigns IDs).
            var componentIdMap = new Dictionary<int, int>();
            var netIdMap = new Dictionary<int, int>();

            // 1. Place components
            if (data.components != null)
            {
                foreach (var compData in data.components)
                {
                    var pins = new List<PinInstance>();
                    if (compData.pins != null)
                    {
                        foreach (var pinData in compData.pins)
                        {
                            pins.Add(new PinInstance(
                                pinData.pinIndex,
                                pinData.pinName,
                                new GridPosition(pinData.localPositionX, pinData.localPositionY)
                            ));
                        }
                    }

                    var customValue = compData.hasCustomValue ? (float?)compData.customValue : null;
                    var placed = boardState.PlaceComponent(
                        compData.componentDefId,
                        new GridPosition(compData.positionX, compData.positionY),
                        compData.rotation,
                        pins,
                        customValue
                    );

                    componentIdMap[compData.instanceId] = placed.InstanceId;
                }
            }

            // 2. Create nets
            if (data.nets != null)
            {
                foreach (var netData in data.nets)
                {
                    var net = boardState.CreateNet(netData.netName);
                    netIdMap[netData.netId] = net.NetId;
                }
            }

            // 3. Add traces (using mapped net IDs)
            if (data.traces != null)
            {
                foreach (var traceData in data.traces)
                {
                    if (!netIdMap.TryGetValue(traceData.netId, out var mappedNetId))
                        continue; // Skip traces for nets that weren't restored

                    boardState.AddTrace(
                        mappedNetId,
                        new GridPosition(traceData.startX, traceData.startY),
                        new GridPosition(traceData.endX, traceData.endY)
                    );
                }
            }

            // 4. Connect pins to nets (using mapped IDs)
            if (data.pinConnections != null)
            {
                foreach (var conn in data.pinConnections)
                {
                    if (!componentIdMap.TryGetValue(conn.componentInstanceId, out var mappedCompId))
                        continue;
                    if (!netIdMap.TryGetValue(conn.netId, out var mappedNetId))
                        continue;

                    // Recalculate world position from the placed component
                    var component = boardState.GetComponent(mappedCompId);
                    if (component == null)
                        continue;

                    var worldPos = component.GetPinWorldPosition(conn.pinIndex);
                    var pinRef = new PinReference(mappedCompId, conn.pinIndex, worldPos);
                    boardState.ConnectPinToNet(mappedNetId, pinRef);
                }
            }
        }

        /// <summary>
        /// Saves the board state to a file as JSON.
        /// Creates the directory if it does not exist.
        /// </summary>
        /// <param name="filePath">Full file path to write to.</param>
        /// <param name="boardState">The board state to save.</param>
        /// <param name="stageId">The stage this solution belongs to.</param>
        public void SaveToFile(string filePath, BoardState boardState, string stageId)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = Serialize(boardState, stageId);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads board save data from a JSON file.
        /// </summary>
        /// <param name="filePath">Full file path to read from.</param>
        /// <returns>Deserialized board save data.</returns>
        public BoardSaveData LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Save file not found: {filePath}", filePath);

            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }

        /// <summary>
        /// Converts a <see cref="BoardState"/> to a <see cref="BoardSaveData"/> DTO.
        /// </summary>
        private BoardSaveData ConvertToSaveData(BoardState boardState, string stageId)
        {
            var data = new BoardSaveData
            {
                stageId = stageId,
                boardWidth = boardState.Bounds.Width,
                boardHeight = boardState.Bounds.Height
            };

            // Serialize components and their pins
            foreach (var comp in boardState.Components)
            {
                var compData = new ComponentSaveData
                {
                    instanceId = comp.InstanceId,
                    componentDefId = comp.ComponentDefinitionId,
                    positionX = comp.Position.X,
                    positionY = comp.Position.Y,
                    rotation = comp.Rotation
                };
                compData.hasCustomValue = comp.CustomValue.HasValue;
                compData.customValue = comp.CustomValue ?? 0f;

                foreach (var pin in comp.Pins)
                {
                    compData.pins.Add(new PinInstanceSaveData
                    {
                        pinIndex = pin.PinIndex,
                        pinName = pin.PinName,
                        localPositionX = pin.LocalPosition.X,
                        localPositionY = pin.LocalPosition.Y
                    });
                }

                data.components.Add(compData);
            }

            // Serialize nets and pin connections
            foreach (var net in boardState.Nets)
            {
                data.nets.Add(new NetSaveData
                {
                    netId = net.NetId,
                    netName = net.NetName
                });

                foreach (var pinRef in net.ConnectedPins)
                {
                    data.pinConnections.Add(new PinConnectionSaveData
                    {
                        componentInstanceId = pinRef.ComponentInstanceId,
                        pinIndex = pinRef.PinIndex,
                        netId = net.NetId,
                        pinWorldX = pinRef.Position.X,
                        pinWorldY = pinRef.Position.Y
                    });
                }
            }

            // Serialize traces
            foreach (var trace in boardState.Traces)
            {
                data.traces.Add(new TraceSaveData
                {
                    netId = trace.NetId,
                    startX = trace.Start.X,
                    startY = trace.Start.Y,
                    endX = trace.End.X,
                    endY = trace.End.Y
                });
            }

            return data;
        }
    }
}
