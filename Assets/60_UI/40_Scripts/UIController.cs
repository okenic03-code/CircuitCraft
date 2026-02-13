using System;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Controllers;
using CircuitCraft.Data;
using CircuitCraft.Views;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the main HUD layout: Toolbar, GameView, StatusBar.
    /// Component palette, simulation button, and results panel are handled
    /// by their own dedicated controllers.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GridCursor _gridCursor;
        [SerializeField] private PlacementController _placementController;
        [SerializeField] private GridSettings _gridSettings;

        private VisualElement _root;
        private VisualElement _toolbar;
        private VisualElement _gameView;
        private VisualElement _statusBar;

        private Label _statusText;
        private Label _statusCoords;
        private Label _statusGrid;
        private Label _statusZoom;

        private Button _clearBoardButton;
        private Button _undoButton;
        private Button _redoButton;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_gridCursor == null)
                _gridCursor = FindFirstObjectByType<GridCursor>();

            if (_placementController == null)
                _placementController = FindFirstObjectByType<PlacementController>();
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UIController: No UIDocument component found.");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("UIController: UIDocument has no root visual element.");
                return;
            }

            QueryVisualElements();
            RegisterCallbacks();
            SetStatusText("Ready");
        }

        private void OnDisable()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked -= OnClearBoard;

            if (_undoButton != null)
                _undoButton.clicked -= OnUndo;

            if (_redoButton != null)
                _redoButton.clicked -= OnRedo;
        }

        private void Update()
        {
            UpdateStatusBar();
        }

        private void QueryVisualElements()
        {
            _toolbar = _root.Q<VisualElement>("Toolbar");
            _gameView = _root.Q<VisualElement>("GameView");
            _statusBar = _root.Q<VisualElement>("StatusBar");

            _statusText = _root.Q<Label>("StatusText");
            _statusCoords = _root.Q<Label>("StatusCoords");
            _statusGrid = _root.Q<Label>("StatusGrid");
            _statusZoom = _root.Q<Label>("StatusZoom");

            _clearBoardButton = _root.Q<Button>("ClearBoardButton");
            _undoButton = _root.Q<Button>("UndoButton");
            _redoButton = _root.Q<Button>("RedoButton");

            if (_statusText == null) Debug.LogWarning("UIController: StatusText not found.");
            if (_toolbar == null) Debug.LogWarning("UIController: Toolbar not found.");
            if (_gameView == null) Debug.LogWarning("UIController: GameView not found.");
            if (_statusBar == null) Debug.LogWarning("UIController: StatusBar not found.");
            if (_undoButton == null) Debug.LogWarning("UIController: UndoButton not found.");
            if (_redoButton == null) Debug.LogWarning("UIController: RedoButton not found.");
        }

        private void RegisterCallbacks()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked += OnClearBoard;

            if (_undoButton != null)
                _undoButton.clicked += OnUndo;

            if (_redoButton != null)
                _redoButton.clicked += OnRedo;
        }

        private void UpdateStatusBar()
        {
            // Grid coordinates from GridCursor
            if (_statusCoords != null && _gridCursor != null)
            {
                Vector2Int gridPos = _gridCursor.GetCurrentGridPosition();
                _statusCoords.text = _gridCursor.IsOverValidGrid()
                    ? $"Pos: ({gridPos.x}, {gridPos.y})"
                    : "Pos: --";
            }

            // Grid cell size from GridSettings
            if (_statusGrid != null && _gridSettings != null)
            {
                _statusGrid.text = $"Grid: {_gridSettings.CellSize:F1} | \u221e";
            }

            // Zoom percentage from camera orthographic size
            if (_statusZoom != null && _mainCamera != null && _mainCamera.orthographic)
            {
                // Default ortho size = 12, treat as 100%
                float zoomPercent = (12f / _mainCamera.orthographicSize) * 100f;
                _statusZoom.text = $"{Mathf.RoundToInt(zoomPercent)}%";
            }

            // Undo/Redo button enabled state
            UpdateUndoRedoState();
        }

        /// <summary>
        /// Updates the status text in the bottom-left of the status bar.
        /// </summary>
        public void SetStatusText(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        private void OnClearBoard()
        {
            Debug.Log("UIController: Clear Board requested.");
            SetStatusText("Board cleared.");
        }

        private void OnUndo()
        {
            if (_placementController != null && _placementController.CanUndo)
            {
                _placementController.UndoLastAction();
                SetStatusText("Undo");
            }
        }

        private void OnRedo()
        {
            if (_placementController != null && _placementController.CanRedo)
            {
                _placementController.RedoLastAction();
                SetStatusText("Redo");
            }
        }

        private void UpdateUndoRedoState()
        {
            if (_placementController == null)
                return;

            if (_undoButton != null)
                _undoButton.SetEnabled(_placementController.CanUndo);

            if (_redoButton != null)
                _redoButton.SetEnabled(_placementController.CanRedo);
        }
    }
}
