using CircuitCraft.Commands;
using CircuitCraft.Components;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Utils;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Pin Selection")]
        [SerializeField]
        [Tooltip("Maximum Manhattan distance used when resolving start pins from grid clicks. 0 means exact pin cell only.")]
        [Min(0)]
        private int _startPinSnapDistance = 0;

        [SerializeField]
        [Tooltip("Maximum Manhattan distance used when resolving end pins during commit. 0 means exact pin cell only.")]
        [Min(0)]
        private int _commitPinSnapDistance = 0;

        [Header("Debug")]
        [SerializeField] private bool _debugLogs = true;

        private enum RoutingState
        {
            Idle,
            PinSelected,
            Drawing
        }

        private RoutingState _state = RoutingState.Idle;
        private BoardState _boardState;
        private System.Action<string> _onBoardLoadedHandler;
        private PinReference _startPin;
        private int _selectedTraceSegmentId = -1;
        private readonly List<(GridPosition start, GridPosition end)> _pendingRouteSegments = new();
        private GridPosition _currentRouteAnchor;
        private int _escapeConsumedFrame = -1;
        private Coroutine _statusRestoreCoroutine;

        private const string StatusWiring = "배선 중... (ESC: 취소)";
        private const string StatusWiringMode = "배선 모드 (Ctrl+W: 해제)";
        private const string StatusWireCompleted = "배선 완료";
        private const string StatusReady = "Ready";
        private const float CommitStatusDisplaySeconds = 1.2f;

        private void LogDebug(string message)
        {
            if (!_debugLogs)
            {
                return;
            }

            Debug.Log($"[WireRoutingController] {message}", this);
        }

        private static string FormatPin(PinReference pin)
        {
            return $"C{pin.ComponentInstanceId}:P{pin.PinIndex}@{pin.Position}";
        }

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

            if (_gameManager != null)
            {
                _onBoardLoadedHandler = _ => HandleBoardReset();
                _gameManager.OnBoardLoaded += _onBoardLoadedHandler;
            }

            SyncBoardReferences();
        }

        private void Update()
        {
            SyncBoardReferences();

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
                UpdateRoutingPreview(currentPos);
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

            Vector2Int clickGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            LogDebug($"LeftClick state={_state}, wiringMode={_wiringModeActive}, screen={Input.mousePosition}, grid=({clickGrid.x},{clickGrid.y})");

            if (_wiringModeActive)
            {
                bool pointerOverUI = UIInputHelper.IsPointerOverRealUI(_uiDocuments);

                if (_state == RoutingState.Idle)
                {
                    bool hasStartPin = TryGetClickedPinByGrid(out var clickedStartPin, maxDistance: _startPinSnapDistance)
                                       || TryGetClickedPin(out clickedStartPin);
                    LogDebug($"WiringMode idle click: pointerOverUI={pointerOverUI}, hasStartPin={hasStartPin}" + (hasStartPin ? $", pin={FormatPin(clickedStartPin)}" : ""));

                    if (hasStartPin)
                    {
                        StartRouting(clickedStartPin);
                        return;
                    }

                    if (pointerOverUI)
                    {
                        LogDebug("WiringMode idle click blocked by UI (no start pin).");
                        return;
                    }

                    LogDebug("WiringMode idle click had no resolvable start pin.");
                    return;
                }

                bool hasEndPin = TryGetRoutingCommitPin(out var clickedEndPin);
                LogDebug($"WiringMode routing click: pointerOverUI={pointerOverUI}, hasEndPin={hasEndPin}" + (hasEndPin ? $", pin={FormatPin(clickedEndPin)}" : ""));
                if (hasEndPin)
                {
                    CommitRouting(clickedEndPin);
                    return;
                }

                if (pointerOverUI)
                {
                    LogDebug("WiringMode routing click blocked by UI (no exact end pin).");
                    return;
                }

                GridPosition waypointPosition = new(clickGrid.x, clickGrid.y);
                if (TryGetTraceAtPosition(waypointPosition, out var clickedTrace))
                {
                    LogDebug($"WiringMode routing click hit trace segment {clickedTrace.SegmentId}; committing joint at {waypointPosition}.");
                    CommitRoutingToTrace(clickedTrace.NetId, waypointPosition);
                    return;
                }

                AddRoutingWaypoint(waypointPosition);
                return;
            }

            // Skip if pointer is over UI
            if (UIInputHelper.IsPointerOverUI(_uiDocuments))
            {
                LogDebug("LeftClick ignored because pointer is over UI.");
                return;
            }

            // If routing is already active, handle commit or waypoint
            if (_state == RoutingState.Drawing || _state == RoutingState.PinSelected)
            {
                if (TryGetRoutingCommitPin(out var commitPin))
                {
                    LogDebug($"Commit via exact pin: {FormatPin(commitPin)}");
                    CommitRouting(commitPin);
                }
                else
                {
                    GridPosition waypointPosition = new(clickGrid.x, clickGrid.y);
                    if (TryGetTraceAtPosition(waypointPosition, out var clickedTrace))
                    {
                        LogDebug($"Routing active click hit trace segment {clickedTrace.SegmentId}; committing joint at {waypointPosition}.");
                        CommitRoutingToTrace(clickedTrace.NetId, waypointPosition);
                    }
                    else
                    {
                        LogDebug("Routing active click had no exact end pin/trace; adding waypoint.");
                        AddRoutingWaypoint(waypointPosition);
                    }
                }
                return;
            }

            // IDLE state: start routing only when clicking a pin dot.
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, _raycastDistance))
            {
                bool isPinDot = hit.collider is SphereCollider
                                && hit.collider.gameObject.name.StartsWith("PinDot_");
                bool hitComponent = hit.collider.GetComponentInParent<ComponentView>() != null;
                LogDebug($"Raycast hit '{hit.collider.gameObject.name}' ({hit.collider.GetType().Name}), point={hit.point}, isPinDot={isPinDot}, hitComponent={hitComponent}");
                if (isPinDot)
                {
                    if (TryGetClickedPinByGrid(out var startPin, maxDistance: _startPinSnapDistance))
                    {
                        LogDebug($"Start routing via grid pin: {FormatPin(startPin)}");
                        StartRouting(startPin);
                        return;
                    }
                    if (TryGetClickedPin(out var startPinFallback))
                    {
                        LogDebug($"Start routing via raycast pin fallback: {FormatPin(startPinFallback)}");
                        StartRouting(startPinFallback);
                        return;
                    }

                    LogDebug("Hit component/pin-dot but failed to resolve pin.");
                }

                if (hitComponent)
                {
                    // Let component interaction own body-click behaviors (select/move/rotate).
                    LogDebug("Component body click ignored for routing start.");
                    return;
                }
            }
            else
            {
                LogDebug("Raycast missed all colliders.");
            }

            if (_state == RoutingState.Idle)
            {
                LogDebug("No pin hit; trying trace selection.");
                TrySelectTraceAtMouse();
            }
        }

        private void HandleCancelInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (_state != RoutingState.Idle)
                    CancelRouting();
                else if (_wiringModeActive)
                    DeactivateWiringMode();

                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                TryConsumeEscape();
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
            StopPendingStatusRestore();
            _startPin = startPin;
            _state = RoutingState.PinSelected;
            _selectedTraceSegmentId = -1;
            _pendingRouteSegments.Clear();
            _currentRouteAnchor = startPin.Position;
            _state = RoutingState.Drawing;
            LogDebug($"StartRouting startPin={FormatPin(startPin)}");

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            var currentPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            _wirePreviewManager?.Show();
            UpdateRoutingPreview(currentPos);

            if (_statusLabel is not null)
                _statusLabel.text = StatusWiring;
        }

        private void CommitRouting(PinReference endPin)
        {
            LogDebug($"CommitRouting startPin={FormatPin(_startPin)}, endPin={FormatPin(endPin)}");
            if (_startPin.ComponentInstanceId == endPin.ComponentInstanceId && _startPin.PinIndex == endPin.PinIndex)
            {
                LogDebug("CommitRouting canceled: same pin selected.");
                CancelRouting();
                return;
            }

            PinReference committedStartPin = _startPin;
            var segments = BuildSegmentsTo(endPin.Position);
            if (segments.Count == 0)
            {
                LogDebug("CommitRouting canceled: no valid segments to commit.");
                CancelRouting();
                return;
            }

            var command = new RouteTraceCommand(_boardState, _startPin, endPin, segments);
            _commandHistory.ExecuteCommand(command);
            LogDebug($"CommitRouting executed with {segments.Count} segment(s).");

            CancelRouting();
            ShowRoutingCommitStatus(committedStartPin, endPin);
        }

        private void CommitRoutingToTrace(int targetNetId, GridPosition targetPoint)
        {
            LogDebug($"CommitRoutingToTrace startPin={FormatPin(_startPin)}, targetNet={targetNetId}, targetPoint={targetPoint}");

            var segments = BuildSegmentsTo(targetPoint);
            if (segments.Count == 0)
            {
                LogDebug("CommitRoutingToTrace canceled: no valid segments to commit.");
                CancelRouting();
                return;
            }

            PinReference committedStartPin = _startPin;
            var command = new RouteTraceToNetPointCommand(_boardState, _startPin, targetNetId, targetPoint, segments);
            _commandHistory.ExecuteCommand(command);
            LogDebug($"CommitRoutingToTrace executed with {segments.Count} segment(s).");

            CancelRouting();
            ShowRoutingCommitStatus(committedStartPin, new PinReference(-1, -1, targetPoint));
        }

        private bool TryGetClickedPin(out PinReference pinRef)
        {
            pinRef = default;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, _raycastDistance))
            {
                LogDebug("TryGetClickedPin: raycast miss.");
                return false;
            }

            ComponentView componentView = hit.collider.GetComponent<ComponentView>();
            if (componentView == null)
            {
                componentView = hit.collider.GetComponentInParent<ComponentView>();
            }

            if (componentView == null)
            {
                LogDebug($"TryGetClickedPin: hit '{hit.collider.gameObject.name}' but no ComponentView found.");
                return false;
            }

            var boardPos = new GridPosition(componentView.GridPosition.x, componentView.GridPosition.y);
            var primaryComponent = _boardState.GetComponentAt(boardPos);
            LogDebug($"TryGetClickedPin: componentView='{componentView.name}', boardPos={boardPos}, hasPrimaryComponent={primaryComponent != null}");

            // Prefer exact pin resolution when raycast directly hit a pin dot collider.
            if (primaryComponent is not null
                && hit.collider is SphereCollider
                && hit.collider.gameObject.name.StartsWith("PinDot_"))
            {
                string pinName = hit.collider.gameObject.name.Substring("PinDot_".Length);
                foreach (var pin in primaryComponent.Pins)
                {
                    if (pin is null)
                        continue;

                    if (!string.Equals(pin.PinName, pinName))
                        continue;

                    pinRef = new PinReference(
                        primaryComponent.InstanceId,
                        pin.PinIndex,
                        primaryComponent.GetPinWorldPosition(pin.PinIndex)
                    );
                    LogDebug($"TryGetClickedPin: exact PinDot hit resolved to {FormatPin(pinRef)}.");
                    return true;
                }

                LogDebug($"TryGetClickedPin: PinDot '{pinName}' did not map to primary component pins.");
            }

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            var mouseGridPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            if (PinDetector.TryGetNearestPin(primaryComponent, mouseGridPos, out pinRef, maxDistance: 0))
            {
                LogDebug($"TryGetClickedPin: nearest pin on primary component resolved to {FormatPin(pinRef)}.");
                return true;
            }

            var fallbackComponent = _boardState.GetComponentAt(mouseGridPos);
            if (primaryComponent is null || fallbackComponent is null || primaryComponent.InstanceId != fallbackComponent.InstanceId)
            {
                if (PinDetector.TryGetNearestPin(fallbackComponent, mouseGridPos, out pinRef, maxDistance: 0))
                {
                    LogDebug($"TryGetClickedPin: fallback component resolved to {FormatPin(pinRef)}.");
                    return true;
                }
            }

            LogDebug($"TryGetClickedPin: failed to resolve pin at mouseGrid={mouseGridPos}.");
            return false;
        }

        private bool TryGetClickedPinByGrid(out PinReference pinRef, int maxDistance = 1)
        {
            pinRef = default;

            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );

            var mouseGridPos = new GridPosition(mouseGrid.x, mouseGrid.y);

            bool found = PinDetector.TryGetNearestPinFromAll(_boardState.Components, mouseGridPos, out pinRef, maxDistance);
            if (found)
            {
                LogDebug($"TryGetClickedPinByGrid: resolved {FormatPin(pinRef)} from mouseGrid={mouseGridPos}, maxDistance={maxDistance}.");
            }
            else
            {
                LogDebug($"TryGetClickedPinByGrid: no pin near mouseGrid={mouseGridPos}, maxDistance={maxDistance}.");
            }

            return found;
        }

private bool TryGetRoutingCommitPin(out PinReference pinRef)
        {
            // During active routing, only commit when click resolves to an exact pin.
            // This prevents intermediate grid clicks from snapping to nearby pins.
            if (TryGetClickedPinByGrid(out pinRef, maxDistance: _commitPinSnapDistance))
            {
                LogDebug($"TryGetRoutingCommitPin: exact grid match {FormatPin(pinRef)}.");
                return true;
            }

            if (TryGetPinByDirectPinDotHit(out pinRef))
            {
                LogDebug($"TryGetRoutingCommitPin: direct pin-dot hit {FormatPin(pinRef)}.");
                return true;
            }

            LogDebug("TryGetRoutingCommitPin: no exact pin resolved.");
            return false;
        }

private bool TryGetPinByDirectPinDotHit(out PinReference pinRef)
        {
            pinRef = default;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, _raycastDistance))
            {
                return false;
            }

            if (hit.collider is not SphereCollider || !hit.collider.gameObject.name.StartsWith("PinDot_"))
            {
                return false;
            }

            ComponentView componentView = hit.collider.GetComponentInParent<ComponentView>();
            if (componentView == null)
            {
                return false;
            }

            var boardPos = new GridPosition(componentView.GridPosition.x, componentView.GridPosition.y);
            var component = _boardState.GetComponentAt(boardPos);
            if (component is null)
            {
                return false;
            }

            string pinName = hit.collider.gameObject.name.Substring("PinDot_".Length);
            foreach (var pin in component.Pins)
            {
                if (pin is null || !string.Equals(pin.PinName, pinName))
                {
                    continue;
                }

                pinRef = new PinReference(
                    component.InstanceId,
                    pin.PinIndex,
                    component.GetPinWorldPosition(pin.PinIndex)
                );
                return true;
            }

            return false;
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

        private bool TryGetTraceAtPosition(GridPosition position, out TraceSegment traceSegment)
        {
            traceSegment = null;
            foreach (var trace in _boardState.Traces)
            {
                if (!WirePathCalculator.IsPointOnTrace(trace, position))
                {
                    continue;
                }

                traceSegment = trace;
                return true;
            }

            return false;
        }

        private void CancelRouting()
        {
            LogDebug("CancelRouting.");
            StopPendingStatusRestore();
            _state = RoutingState.Idle;
            _startPin = default;
            _currentRouteAnchor = default;
            _pendingRouteSegments.Clear();
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
            LogDebug("ActivateWiringMode.");
            CancelRouting();

            if (_placementController != null)
                _placementController.SetSelectedComponent(null);

            UpdateStatusForWiringMode();
        }

        private void DeactivateWiringMode()
        {
            _wiringModeActive = false;
            LogDebug("DeactivateWiringMode.");
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
            LogDebug("HandleBoardReset.");
            CancelRouting();
            _selectedTraceSegmentId = -1;
            SyncBoardReferences();
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;

            if (_gameManager != null)
                _gameManager.OnBoardLoaded -= _onBoardLoadedHandler;

            StopPendingStatusRestore();
        }

        /// <summary>
        /// Gets whether wiring mode is currently active.
        /// </summary>
        public bool IsWiringModeActive => _wiringModeActive;

        /// <summary>
        /// Gets whether a routing gesture is currently active.
        /// </summary>
        public bool IsRoutingInProgress => _state != RoutingState.Idle;

        /// <summary>
        /// Whether this controller will consume the next ESC key press.
        /// True when wiring mode is active or routing is in progress.
        /// </summary>
        public bool ShouldConsumeEscape => _state != RoutingState.Idle || _wiringModeActive;

        /// <summary>
        /// Gets whether this controller consumed ESC on the current frame.
        /// </summary>
        public bool ConsumedEscapeThisFrame => _escapeConsumedFrame == Time.frameCount;

        /// <summary>
        /// Tries to consume ESC by canceling active routing or wiring mode.
        /// Returns true when ESC was consumed.
        /// </summary>
        public bool TryConsumeEscape()
        {
            if (_state != RoutingState.Idle)
            {
                CancelRouting();
                _escapeConsumedFrame = Time.frameCount;
                return true;
            }

            if (_wiringModeActive)
            {
                DeactivateWiringMode();
                _escapeConsumedFrame = Time.frameCount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Toggles wiring mode on/off. Called by UI button.
        /// </summary>
        public void ToggleWiringModePublic()
        {
            ToggleWiringMode();
        }

        private void SyncBoardReferences()
        {
            if (_gameManager == null)
            {
                return;
            }

            BoardState latestBoard = _gameManager.BoardState;
            CommandHistory latestHistory = _gameManager.CommandHistory;
            bool boardChanged = !ReferenceEquals(_boardState, latestBoard);
            bool historyChanged = !ReferenceEquals(_commandHistory, latestHistory);

            if (!boardChanged && !historyChanged)
            {
                return;
            }

            _boardState = latestBoard;
            _commandHistory = latestHistory;

            int componentCount = _boardState != null ? _boardState.Components.Count : -1;
            LogDebug($"SyncBoardReferences boardChanged={boardChanged}, historyChanged={historyChanged}, componentCount={componentCount}");
        }

        private void AddRoutingWaypoint(GridPosition waypointPosition)
        {
            if (_state != RoutingState.Drawing && _state != RoutingState.PinSelected)
            {
                return;
            }

            if (waypointPosition == _currentRouteAnchor)
            {
                LogDebug($"AddRoutingWaypoint ignored: waypoint {waypointPosition} is same as current anchor.");
                return;
            }

            int addedSegments = AppendNonDegenerateSegments(
                _pendingRouteSegments,
                WirePathCalculator.BuildManhattanSegments(_currentRouteAnchor, waypointPosition));
            _currentRouteAnchor = waypointPosition;

            LogDebug($"AddRoutingWaypoint waypoint={waypointPosition}, addedSegments={addedSegments}, totalLockedSegments={_pendingRouteSegments.Count}");
            UpdateRoutingPreview(waypointPosition);

            if (_statusLabel is not null)
            {
                _statusLabel.text = $"{StatusWiring} | 경유점: ({waypointPosition.X},{waypointPosition.Y})";
            }
        }

        private List<(GridPosition start, GridPosition end)> BuildSegmentsTo(GridPosition targetPosition)
        {
            var combinedSegments = new List<(GridPosition start, GridPosition end)>(_pendingRouteSegments.Count + 2);
            combinedSegments.AddRange(_pendingRouteSegments);
            AppendNonDegenerateSegments(
                combinedSegments,
                WirePathCalculator.BuildManhattanSegments(_currentRouteAnchor, targetPosition));
            return combinedSegments;
        }

        private static int AppendNonDegenerateSegments(
            List<(GridPosition start, GridPosition end)> destination,
            IReadOnlyList<(GridPosition start, GridPosition end)> source)
        {
            int added = 0;
            for (int i = 0; i < source.Count; i++)
            {
                var segment = source[i];
                if (segment.start == segment.end)
                {
                    continue;
                }

                destination.Add(segment);
                added++;
            }

            return added;
        }

        private void UpdateRoutingPreview(GridPosition currentMouseGridPos)
        {
            _wirePreviewManager?.UpdatePath(
                _pendingRouteSegments,
                _currentRouteAnchor,
                currentMouseGridPos,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);
        }

        private void ShowRoutingCommitStatus(PinReference startPin, PinReference endPin)
        {
            if (_statusLabel is null)
            {
                return;
            }

            _statusLabel.text = $"{StatusWireCompleted}: ({startPin.Position.X},{startPin.Position.Y}) -> ({endPin.Position.X},{endPin.Position.Y})";
            string fallbackStatus = _wiringModeActive ? StatusWiringMode : StatusReady;
            _statusRestoreCoroutine = StartCoroutine(RestoreStatusAfterDelay(CommitStatusDisplaySeconds, fallbackStatus));
        }

        private IEnumerator RestoreStatusAfterDelay(float seconds, string fallbackStatus)
        {
            yield return new WaitForSeconds(seconds);

            if (_statusLabel is not null)
            {
                _statusLabel.text = fallbackStatus;
            }

            _statusRestoreCoroutine = null;
        }

        private void StopPendingStatusRestore()
        {
            if (_statusRestoreCoroutine is null)
            {
                return;
            }

            StopCoroutine(_statusRestoreCoroutine);
            _statusRestoreCoroutine = null;
        }

    }
}
