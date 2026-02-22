using CircuitCraft.Commands;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Handles mouse-driven wire routing between component pins.
    /// </summary>
    public class WireRoutingController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Game manager that provides board state and command history.")]
        [SerializeField] private GameManager _gameManager;

        [Tooltip("Stage manager used to refresh references when stages load.")]
        [SerializeField] private StageManager _stageManager;

        [Tooltip("Grid settings used for screen-to-grid conversion.")]
        [SerializeField] private GridSettings _gridSettings;

        [Tooltip("Camera used for routing raycasts and cursor grid conversion.")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("Preview manager used to draw temporary routing paths.")]
        [SerializeField] private WirePreviewManager _wirePreviewManager;

        private CommandHistory _commandHistory;
        [Tooltip("UI documents used to suppress board input when hovering UI.")]
        [SerializeField] private UIDocument[] _uiDocuments;
        private bool _wiringModeActive;

        [Tooltip("Placement controller used to coordinate mode switching.")]
        [SerializeField] private PlacementController _placementController;
        private Label _statusLabel;

        [Header("Raycast Settings")]
        [SerializeField] private float _raycastDistance = 100f;

        private enum RoutingState
        {
            Idle,
            PinSelected,
            Drawing
        }

        private RoutingState _state = RoutingState.Idle;
        private BoardState _boardState;
        private PinReference _startPin;
        private int _selectedTraceSegmentId = -1;

        private const string StatusWiring = "배선 중... (ESC: 취소)";
        private const string StatusWiringMode = "배선 모드 (Ctrl+W: 해제)";
        private const string StatusReady = "Ready";

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                _commandHistory = _gameManager.CommandHistory;
            }

            if (_wirePreviewManager == null)
                _wirePreviewManager = GetComponent<WirePreviewManager>();

            if (_uiDocuments is not null)
            {
                foreach (var doc in _uiDocuments)
                {
                    if (doc == null || doc.rootVisualElement is null)
                        continue;

                    _statusLabel = doc.rootVisualElement.Q<Label>("StatusText");
                    if (_statusLabel is not null)
                        break;
                }
            }

            if (_wirePreviewManager != null)
            {
                _wirePreviewManager.Initialize();
                _wirePreviewManager.Hide();
            }
        }

        private void Start()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleBoardReset;
        }

        private void Update()
        {
            if (_boardState is null || _gridSettings == null || _mainCamera == null)
                return;

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.W))
            {
                ToggleWiringMode();
                return;
            }

            if (_wiringModeActive && _placementController != null && _placementController.GetSelectedComponent() != null)
            {
                DeactivateWiringMode();
            }

            HandleUndoRedoInput();
            HandleCancelInput();
            HandleDeleteSelectedTrace();
            HandleLeftClick();

            if (_state == RoutingState.Drawing)
            {
                Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                    Input.mousePosition,
                    _mainCamera,
                    _gridSettings.CellSize,
                    _gridSettings.GridOrigin
                );
                var currentPos = new GridPosition(mouseGrid.x, mouseGrid.y);
                _wirePreviewManager?.UpdatePath(_startPin.Position, currentPos, _gridSettings.CellSize, _gridSettings.GridOrigin);
            }
        }

        private void HandleUndoRedoInput()
        {
            // Only handle undo/redo when not actively routing
            if (_state != RoutingState.Idle)
                return;

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrl)
                return;

            if (Input.GetKeyDown(KeyCode.Z))
            {
                _commandHistory.Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                _commandHistory.Redo();
            }
        }

        private void HandleLeftClick()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (_wiringModeActive)
            {
                if (UIInputHelper.IsPointerOverRealUI(_uiDocuments))
                    return;

                if (TryGetClickedPinByGrid(out var clickedPinByGrid))
                {
                    if (_state == RoutingState.Idle)
                    {
                        StartRouting(clickedPinByGrid);
                    }
                    else if (_state == RoutingState.Drawing || _state == RoutingState.PinSelected)
                    {
                        CommitRouting(clickedPinByGrid);
                    }
                }

                return;
            }

            // Skip if pointer is over UI
            if (UIInputHelper.IsPointerOverUI(_uiDocuments))
                return;

            if (TryGetClickedPin(out var clickedPin))
            {
                if (_state == RoutingState.Idle)
                {
                    StartRouting(clickedPin);
                }
                else if (_state == RoutingState.Drawing || _state == RoutingState.PinSelected)
                {
                    CommitRouting(clickedPin);
                }

                return;
            }

            if (_state == RoutingState.Idle)
            {
                TrySelectTraceAtMouse();
            }
        }

        private void HandleCancelInput()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (_state != RoutingState.Idle)
                    CancelRouting();
                else if (_wiringModeActive)
                    DeactivateWiringMode();
            }
        }

        private void HandleDeleteSelectedTrace()
        {
            if (!Input.GetKeyDown(KeyCode.Delete) || _selectedTraceSegmentId < 0)
                return;

            TraceSegment selectedTrace = null;
            foreach (var trace in _boardState.Traces)
            {
                if (trace.SegmentId == _selectedTraceSegmentId)
                {
                    selectedTrace = trace;
                    break;
                }
            }

            if (selectedTrace is null)
            {
                _selectedTraceSegmentId = -1;
                return;
            }

            _commandHistory.ExecuteCommand(new DeleteTraceNetCommand(_boardState, selectedTrace.NetId));

            _selectedTraceSegmentId = -1;
        }

        private void StartRouting(PinReference startPin)
        {
            _startPin = startPin;
            _state = RoutingState.PinSelected;
            _selectedTraceSegmentId = -1;
            _state = RoutingState.Drawing;

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            var currentPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            _wirePreviewManager?.Show();
            _wirePreviewManager?.UpdatePath(_startPin.Position, currentPos, _gridSettings.CellSize, _gridSettings.GridOrigin);

            if (_statusLabel is not null)
                _statusLabel.text = StatusWiring;
        }

        private void CommitRouting(PinReference endPin)
        {
            if (_startPin.ComponentInstanceId == endPin.ComponentInstanceId && _startPin.PinIndex == endPin.PinIndex)
            {
                CancelRouting();
                return;
            }

            var segments = WirePathCalculator.BuildManhattanSegments(_startPin.Position, endPin.Position);
            var command = new RouteTraceCommand(_boardState, _startPin, endPin, segments);
            _commandHistory.ExecuteCommand(command);

            CancelRouting();
        }

        private bool TryGetClickedPin(out PinReference pinRef)
        {
            pinRef = default;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, _raycastDistance))
                return false;

            ComponentView componentView = hit.collider.GetComponent<ComponentView>();
            if (componentView == null)
            {
                componentView = hit.collider.GetComponentInParent<ComponentView>();
            }

            if (componentView == null)
                return false;

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            var mouseGridPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            var boardPos = new GridPosition(componentView.GridPosition.x, componentView.GridPosition.y);
            var primaryComponent = _boardState.GetComponentAt(boardPos);
            if (PinDetector.TryGetNearestPin(primaryComponent, mouseGridPos, out pinRef))
                return true;

            var fallbackComponent = _boardState.GetComponentAt(mouseGridPos);
            if (primaryComponent is null || fallbackComponent is null || primaryComponent.InstanceId != fallbackComponent.InstanceId)
            {
                if (PinDetector.TryGetNearestPin(fallbackComponent, mouseGridPos, out pinRef))
                    return true;
            }

            return false;
        }

        private bool TryGetClickedPinByGrid(out PinReference pinRef)
        {
            pinRef = default;

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );

            var mouseGridPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            return PinDetector.TryGetNearestPinFromAll(_boardState.Components, mouseGridPos, out pinRef);
        }

        private void TrySelectTraceAtMouse()
        {
            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            var pos = new GridPosition(mouseGrid.x, mouseGrid.y);

            _selectedTraceSegmentId = -1;
            foreach (var trace in _boardState.Traces)
            {
                if (WirePathCalculator.IsPointOnTrace(trace, pos))
                {
                    _selectedTraceSegmentId = trace.SegmentId;
                    break;
                }
            }
        }

        private void CancelRouting()
        {
            _state = RoutingState.Idle;
            _startPin = default;
            _wirePreviewManager?.Hide();

            if (_wiringModeActive)
            {
                UpdateStatusForWiringMode();
            }
            else if (_statusLabel is not null)
            {
                _statusLabel.text = StatusReady;
            }
        }

        private void ToggleWiringMode()
        {
            if (_wiringModeActive)
                DeactivateWiringMode();
            else
                ActivateWiringMode();
        }

        private void ActivateWiringMode()
        {
            _wiringModeActive = true;
            CancelRouting();

            if (_placementController != null)
                _placementController.SetSelectedComponent(null);

            UpdateStatusForWiringMode();
        }

        private void DeactivateWiringMode()
        {
            _wiringModeActive = false;
            CancelRouting();

            if (_statusLabel is not null)
                _statusLabel.text = StatusReady;
        }

        private void UpdateStatusForWiringMode()
        {
            if (_statusLabel is not null)
                _statusLabel.text = StatusWiringMode;
        }

        private void HandleBoardReset()
        {
            CancelRouting();
            _selectedTraceSegmentId = -1;

            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                _commandHistory = _gameManager.CommandHistory;
            }
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;
        }

    }
}
