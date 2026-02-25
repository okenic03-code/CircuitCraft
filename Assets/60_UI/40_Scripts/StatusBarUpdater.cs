using CircuitCraft.Commands;
using CircuitCraft.Controllers;
using CircuitCraft.Data;
using CircuitCraft.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Maintains status bar labels for coordinates, grid size, zoom level, and undo/redo state.
    /// </summary>
    public class StatusBarUpdater
    {
        private readonly Label _statusCoords;
        private readonly Label _statusGrid;
        private readonly Label _statusZoom;
        private readonly Label _statusText;
        private readonly Button _undoButton;
        private readonly Button _redoButton;
        private readonly GridCursor _gridCursor;
        private readonly GridSettings _gridSettings;
        private readonly Camera _mainCamera;
        private CommandHistory _commandHistory;
        private Vector2Int _lastGridPos = new Vector2Int(int.MinValue, int.MinValue);
        private int _lastGridX = int.MinValue;
        private int _lastGridY = int.MinValue;
        private bool _lastGridValid;
        private float _lastCellSize = float.MinValue;
        private int _lastZoomPercent = int.MinValue;
        private bool _lastCanUndo;
        private bool _lastCanRedo;

        /// <summary>
        /// Creates a status bar updater with references to status labels, buttons, and data sources.
        /// </summary>
        public StatusBarUpdater(
            Label statusCoords,
            Label statusGrid,
            Label statusZoom,
            Label statusText,
            Button undoButton,
            Button redoButton,
            GridCursor gridCursor,
            GridSettings gridSettings,
            Camera mainCamera,
            CommandHistory commandHistory)
        {
            _statusCoords = statusCoords;
            _statusGrid = statusGrid;
            _statusZoom = statusZoom;
            _statusText = statusText;
            _undoButton = undoButton;
            _redoButton = redoButton;
            _gridCursor = gridCursor;
            _gridSettings = gridSettings;
            _mainCamera = mainCamera;
            _commandHistory = commandHistory;
        }
        /// <summary>
        /// Updates the command history source used by undo/redo state display.
        /// </summary>
        public void SetCommandHistory(CommandHistory history) => _commandHistory = history;

        /// <summary>
        /// Refreshes all status bar sections in one pass.
        /// </summary>
        public void UpdateAll()
        {
            UpdateGridCoordinates();
            UpdateGridCellSizeStatus();
            UpdateZoom();
            UpdateUndoRedo();
        }

        /// <summary>
        /// Refreshes the current grid coordinates label from the grid cursor state.
        /// </summary>
        public void UpdateGridCoordinates()
        {
            if (_statusCoords is null || _gridCursor == null)
                return;
            Vector2Int gridPos = _gridCursor.GetCurrentGridPosition();
            bool overGrid = _gridCursor.IsOverValidGrid();
            if (overGrid)
            {
                if (!_lastGridValid || gridPos.x != _lastGridX || gridPos.y != _lastGridY)
                {
                    _statusCoords.text = $"Pos: ({gridPos.x}, {gridPos.y})";
                    _lastGridPos = gridPos;
                    _lastGridX = gridPos.x;
                    _lastGridY = gridPos.y;
                    _lastGridValid = true;
                }
            }
            else if (_lastGridValid)
            {
                _statusCoords.text = "Pos: --";
                _lastGridValid = false;
                _lastGridX = int.MinValue;
                _lastGridY = int.MinValue;
                _lastGridPos = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        /// <summary>
        /// Refreshes the zoom label using the orthographic camera size.
        /// </summary>
        public void UpdateZoom()
        {
            if (_statusZoom is null || _mainCamera == null || !_mainCamera.orthographic)
                return;
            float zoomPercent = (12f / _mainCamera.orthographicSize) * 100f;
            int roundedZoomPercent = Mathf.RoundToInt(zoomPercent);
            if (_lastZoomPercent != roundedZoomPercent)
            {
                _statusZoom.text = roundedZoomPercent + "%";
                _lastZoomPercent = roundedZoomPercent;
            }
        }
        /// <summary>
        /// Updates undo and redo button enabled states from command history.
        /// </summary>
        public void UpdateUndoRedo()
        {
            if (_commandHistory is null)
                return;
            bool canUndo = _commandHistory.CanUndo;
            bool canRedo = _commandHistory.CanRedo;
            if (_undoButton is null && _redoButton is null)
                return;
            if (_undoButton is not null && _lastCanUndo != canUndo)
            {
                _undoButton.SetEnabled(canUndo);
                _lastCanUndo = canUndo;
            }

            if (_redoButton is not null && _lastCanRedo != canRedo)
            {
                _redoButton.SetEnabled(canRedo);
                _lastCanRedo = canRedo;
            }
        }

        /// <summary>
        /// Sets the general status text label.
        /// </summary>
        public void SetStatusText(string text)
        {
            if (_statusText is not null)
                _statusText.text = text;
        }
        private void UpdateGridCellSizeStatus()
        {
            if (_statusGrid is null || _gridSettings == null)
                return;
            if (!Mathf.Approximately(_lastCellSize, _gridSettings.CellSize))
            {
                _statusGrid.text = $"Grid: {_gridSettings.CellSize:F1} | \u221e";
                _lastCellSize = _gridSettings.CellSize;
            }
        }
    }
}
