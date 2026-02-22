using System;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Commands;
using CircuitCraft.Controllers;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Views;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the main HUD layout: Toolbar, GameView, and StatusBar.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField, Tooltip("UI document that provides the root visual tree for the HUD.")] private UIDocument _uiDocument;
        [SerializeField, Tooltip("Main gameplay camera used to compute zoom display.")] private Camera _mainCamera;
        [SerializeField, Tooltip("Grid cursor controller used to report live grid coordinates.")] private GridCursor _gridCursor;
        [SerializeField, Tooltip("Placement controller reference for HUD-driven placement actions.")] private PlacementController _placementController;
        [SerializeField, Tooltip("Grid settings asset used to display grid cell size.")] private GridSettings _gridSettings;
        [SerializeField, Tooltip("Game manager providing board state and command history access.")] private GameManager _gameManager;
        [SerializeField, Tooltip("Stage manager providing current stage and stage-loaded events.")] private StageManager _stageManager;
        private CommandHistory _commandHistory;
        private VisualElement _root;
        private VisualElement _toolbar;
        private VisualElement _gameView;
        private VisualElement _statusBar;
        private VisualElement _componentPalette;
        private VisualElement _paletteResizeHandle;

        private Label _statusText;
        private Label _statusCoords;
        private Label _statusGrid;
        private Label _statusZoom;
        private Label _statusBudget;
        private Label _stageTitle;
        private Label _stageTargets;
        private PaletteResizer _paletteResizer;
        private Button _clearBoardButton;
        private Button _undoButton;
        private Button _redoButton;
        private StatusBarUpdater _statusBarUpdater;
        private BudgetTracker _budgetTracker;

        private void OnEnable()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("UIController: No UIDocument component found.");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            if (_root is null)
            {
                Debug.LogError("UIController: UIDocument has no root visual element.");
                return;
            }
            QueryVisualElements();
            _paletteResizer = new PaletteResizer(_componentPalette, _paletteResizeHandle, _root);
            if (_gameManager != null)
                _commandHistory = _gameManager.CommandHistory;
            _statusBarUpdater = new StatusBarUpdater(
                _statusCoords,
                _statusGrid,
                _statusZoom,
                _statusText,
                _undoButton,
                _redoButton,
                _gridCursor,
                _gridSettings,
                _mainCamera,
                _commandHistory);
            _budgetTracker = new BudgetTracker(
                _statusBudget,
                _stageTitle,
                _stageTargets,
                _gameManager,
                _stageManager);
            RegisterCallbacks();
            RegisterStatusBarSubscriptions();
            _paletteResizer.RegisterCallbacks();
            RegisterDataSubscriptions();
            SetStatusText("Ready");
            HandleStageLoaded();
            _statusBarUpdater.UpdateAll();
        }
        private void OnDisable()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked -= OnClearBoard;
            if (_undoButton != null)
                _undoButton.clicked -= OnUndo;
            if (_redoButton != null)
                _redoButton.clicked -= OnRedo;
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleStageLoaded;
            UnregisterStatusBarSubscriptions();
            _paletteResizer?.UnregisterCallbacks();
            _budgetTracker?.Dispose();
        }
        private void Update() => _statusBarUpdater?.UpdateZoom();

        private void QueryVisualElements()
        {
            _toolbar = _root.Q<VisualElement>("Toolbar");
            _gameView = _root.Q<VisualElement>("GameView");
            _statusBar = _root.Q<VisualElement>("StatusBar");
            _componentPalette = _root.Q<VisualElement>("ComponentPalette");
            _paletteResizeHandle = _root.Q<VisualElement>("PaletteResizeHandle");

            _statusText = _root.Q<Label>("StatusText");
            _statusBudget = _root.Q<Label>("StatusBudget");
            _statusCoords = _root.Q<Label>("StatusCoords");
            _statusGrid = _root.Q<Label>("StatusGrid");
            _statusZoom = _root.Q<Label>("StatusZoom");
            _stageTitle = _root.Q<Label>("stage-title");
            _stageTargets = _root.Q<Label>("stage-targets");

            _clearBoardButton = _root.Q<Button>("ClearBoardButton");
            _undoButton = _root.Q<Button>("UndoButton");
            _redoButton = _root.Q<Button>("RedoButton");
            if (_statusText == null) Debug.LogWarning("UIController: StatusText not found.");
            if (_toolbar == null) Debug.LogWarning("UIController: Toolbar not found.");
            if (_gameView == null) Debug.LogWarning("UIController: GameView not found.");
            if (_statusBar == null) Debug.LogWarning("UIController: StatusBar not found.");
            if (_undoButton == null) Debug.LogWarning("UIController: UndoButton not found.");
            if (_redoButton == null) Debug.LogWarning("UIController: RedoButton not found.");
            if (_componentPalette == null) Debug.LogWarning("UIController: ComponentPalette not found.");
            if (_paletteResizeHandle == null) Debug.LogWarning("UIController: PaletteResizeHandle not found.");
        }
        private void RegisterDataSubscriptions()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleStageLoaded;
            _budgetTracker?.RebindBoardState();
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
        private void RegisterStatusBarSubscriptions()
        {
            if (_gridCursor != null)
                _gridCursor.OnPositionChanged += OnGridCursorPositionChanged;
            if (_commandHistory != null)
                _commandHistory.OnHistoryChanged += OnHistoryChanged;
        }
        private void UnregisterStatusBarSubscriptions()
        {
            if (_gridCursor != null)
                _gridCursor.OnPositionChanged -= OnGridCursorPositionChanged;
            if (_commandHistory != null)
                _commandHistory.OnHistoryChanged -= OnHistoryChanged;
        }
        private void OnGridCursorPositionChanged() => _statusBarUpdater?.UpdateGridCoordinates();
        private void OnHistoryChanged() => _statusBarUpdater?.UpdateUndoRedo();
        /// <summary>
        /// Sets the status bar message text.
        /// </summary>
        public void SetStatusText(string text) => _statusBarUpdater?.SetStatusText(text);

        /// <summary>
        /// Updates the budget display with current cost and stage budget limit.
        /// </summary>
        public void UpdateBudget(float currentCost, float budgetLimit) => _budgetTracker?.UpdateBudget(currentCost, budgetLimit);

        private void HandleStageLoaded()
        {
            if (_gameManager != null)
                _commandHistory = _gameManager.CommandHistory;
            _statusBarUpdater?.SetCommandHistory(_commandHistory);
            _budgetTracker?.HandleStageLoaded();
            _statusBarUpdater?.UpdateUndoRedo();
        }
        private void OnClearBoard()
        {
            if (_gameManager == null) return;
            int width = 20, height = 15;
            if (_stageManager != null && _stageManager.CurrentStage != null)
            {
                int side = (int)Math.Ceiling(Math.Sqrt(_stageManager.CurrentStage.TargetArea));
                width = side;
                height = side;
            }
            _gameManager.ResetBoard(width, height);
            _budgetTracker?.HandleStageLoaded();
            if (_commandHistory != null)
                _commandHistory.Clear();
            Debug.Log("UIController: Board cleared.");
            SetStatusText("Board cleared.");
        }
        private void OnUndo()
        {
            if (_commandHistory != null && _commandHistory.CanUndo)
            { _commandHistory.Undo(); SetStatusText("Undo"); }
        }
        private void OnRedo()
        {
            if (_commandHistory != null && _commandHistory.CanRedo)
            { _commandHistory.Redo(); SetStatusText("Redo"); }
        }
    }
}
