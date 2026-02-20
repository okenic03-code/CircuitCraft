using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private GridSettings _gridSettings;
        [SerializeField] private Camera _mainCamera;

        private CommandHistory _commandHistory;
        private UIDocument[] _uiDocuments;
        private bool _wiringModeActive;
        private PlacementController _placementController;
        private Label _statusLabel;

        [Header("Raycast Settings")]
        [SerializeField] private float _raycastDistance = 100f;

        [Header("Preview")]
        [SerializeField] private Color _previewColor = Color.yellow;
        [SerializeField] private float _previewWidth = 0.08f;
        [SerializeField] private float _previewY = 0.06f;
        [SerializeField] private Shader _previewShader;

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
        private LineRenderer _previewLine;

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

            // Cache UIDocuments for UI click-through detection
            _uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            _placementController = FindFirstObjectByType<PlacementController>();

            if (_uiDocuments != null)
            {
                foreach (var doc in _uiDocuments)
                {
                    if (doc == null || doc.rootVisualElement == null)
                        continue;

                    _statusLabel = doc.rootVisualElement.Q<Label>("StatusText");
                    if (_statusLabel != null)
                        break;
                }
            }

            EnsurePreviewLine();
            HidePreview();
        }

        private void Start()
        {
            if (_stageManager == null)
                _stageManager = FindFirstObjectByType<StageManager>();
            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleBoardReset;
        }

        private void Update()
        {
            if (_boardState == null || _gridSettings == null || _mainCamera == null)
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
                UpdatePreviewPath();
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
                if (IsPointerOverRealUI())
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
            if (IsPointerOverUI())
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

            var selectedTrace = _boardState.Traces.FirstOrDefault(t => t.SegmentId == _selectedTraceSegmentId);
            if (selectedTrace == null)
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
            ShowPreview();
            UpdatePreviewPath();

            if (_statusLabel != null)
                _statusLabel.text = "배선 중... (ESC: 취소)";
        }

        private void CommitRouting(PinReference endPin)
        {
            if (_startPin.ComponentInstanceId == endPin.ComponentInstanceId && _startPin.PinIndex == endPin.PinIndex)
            {
                CancelRouting();
                return;
            }

            var segments = BuildManhattanSegments(_startPin.Position, endPin.Position);
            var command = new RouteTraceCommand(_boardState, _startPin, endPin, segments);
            _commandHistory.ExecuteCommand(command);

            CancelRouting();
        }

        private List<(GridPosition start, GridPosition end)> BuildManhattanSegments(GridPosition start, GridPosition end)
        {
            var segments = new List<(GridPosition start, GridPosition end)>();

            if (start.X == end.X || start.Y == end.Y)
            {
                segments.Add((start, end));
                return segments;
            }

            var corner = new GridPosition(end.X, start.Y);
            segments.Add((start, corner));
            segments.Add((corner, end));

            return segments;
        }

        private void UpdatePreviewPath()
        {
            Vector2Int mouseGrid = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _mainCamera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );

            var current = new GridPosition(mouseGrid.x, mouseGrid.y);

            if (_startPin.Position.X == current.X || _startPin.Position.Y == current.Y)
            {
                _previewLine.positionCount = 2;
                SetPreviewPosition(0, _startPin.Position);
                SetPreviewPosition(1, current);
            }
            else
            {
                var corner = new GridPosition(current.X, _startPin.Position.Y);
                _previewLine.positionCount = 3;
                SetPreviewPosition(0, _startPin.Position);
                SetPreviewPosition(1, corner);
                SetPreviewPosition(2, current);
            }
        }

        private void SetPreviewPosition(int index, GridPosition gridPos)
        {
            Vector3 worldPos = GridUtility.GridToWorldPosition(
                new Vector2Int(gridPos.X, gridPos.Y),
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            worldPos.y += _previewY;
            _previewLine.SetPosition(index, worldPos);
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
            if (TryGetNearestPin(primaryComponent, mouseGridPos, out pinRef))
                return true;

            var fallbackComponent = _boardState.GetComponentAt(mouseGridPos);
            if (primaryComponent == null || fallbackComponent == null || primaryComponent.InstanceId != fallbackComponent.InstanceId)
            {
                if (TryGetNearestPin(fallbackComponent, mouseGridPos, out pinRef))
                    return true;
            }

            return false;
        }

        private static bool TryGetNearestPin(PlacedComponent component, GridPosition mouseGridPos, out PinReference pinRef)
        {
            pinRef = default;
            if (component == null)
                return false;

            PinReference? bestPin = null;
            int bestDistance = int.MaxValue;

            foreach (var pin in component.Pins)
            {
                GridPosition pinWorld = component.GetPinWorldPosition(pin.PinIndex);
                int distance = pinWorld.ManhattanDistance(mouseGridPos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPin = new PinReference(component.InstanceId, pin.PinIndex, pinWorld);
                }
            }

            if (!bestPin.HasValue)
                return false;

            if (bestDistance > 1)
                return false;

            pinRef = bestPin.Value;
            return true;
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

            PinReference? bestPin = null;
            int bestDistance = int.MaxValue;

            foreach (var component in _boardState.Components)
            {
                foreach (var pin in component.Pins)
                {
                    GridPosition pinWorld = component.GetPinWorldPosition(pin.PinIndex);
                    int distance = pinWorld.ManhattanDistance(mouseGridPos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPin = new PinReference(component.InstanceId, pin.PinIndex, pinWorld);
                    }
                }
            }

            if (bestPin.HasValue && bestDistance <= 1)
            {
                pinRef = bestPin.Value;
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
                if (IsPointOnTrace(trace, pos))
                {
                    _selectedTraceSegmentId = trace.SegmentId;
                    break;
                }
            }
        }

        private static bool IsPointOnTrace(TraceSegment trace, GridPosition point)
        {
            if (trace.Start.X == trace.End.X)
            {
                if (point.X != trace.Start.X)
                    return false;

                int minY = Mathf.Min(trace.Start.Y, trace.End.Y);
                int maxY = Mathf.Max(trace.Start.Y, trace.End.Y);
                return point.Y >= minY && point.Y <= maxY;
            }

            if (point.Y != trace.Start.Y)
                return false;

            int minX = Mathf.Min(trace.Start.X, trace.End.X);
            int maxX = Mathf.Max(trace.Start.X, trace.End.X);
            return point.X >= minX && point.X <= maxX;
        }

        private void CancelRouting()
        {
            _state = RoutingState.Idle;
            _startPin = default;
            HidePreview();

            if (_wiringModeActive)
            {
                UpdateStatusForWiringMode();
            }
            else if (_statusLabel != null)
            {
                _statusLabel.text = "Ready";
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

            if (_statusLabel != null)
                _statusLabel.text = "Ready";
        }

        private void UpdateStatusForWiringMode()
        {
            if (_statusLabel != null)
                _statusLabel.text = "배선 모드 (Ctrl+W: 해제)";
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

        private void EnsurePreviewLine()
        {
            var previewObject = new GameObject("WirePreview");
            previewObject.transform.SetParent(transform, false);

            _previewLine = previewObject.AddComponent<LineRenderer>();
            _previewLine.useWorldSpace = true;
            _previewLine.positionCount = 0;
            _previewLine.startWidth = _previewWidth;
            _previewLine.endWidth = _previewWidth;
            _previewLine.startColor = _previewColor;
            _previewLine.endColor = _previewColor;
            _previewLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _previewLine.receiveShadows = false;
            var shader = _previewShader != null ? _previewShader : Shader.Find("Sprites/Default");
            _previewLine.material = new Material(shader);
        }

        private void ShowPreview()
        {
            if (_previewLine != null)
            {
                _previewLine.enabled = true;
            }
        }

        private void HidePreview()
        {
            if (_previewLine != null)
            {
                _previewLine.positionCount = 0;
                _previewLine.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;

            if (_previewLine != null && _previewLine.material != null)
            {
                Destroy(_previewLine.material);
            }
        }

        /// <summary>
        /// Undoes the last executed routing command.
        /// </summary>
        public void UndoLastAction()
        {
            _commandHistory.Undo();
        }

        /// <summary>
        /// Redoes the most recently undone routing command.
        /// </summary>
        public void RedoLastAction()
        {
            _commandHistory.Redo();
        }

        /// <summary>
        /// Gets whether there is at least one command available to undo.
        /// </summary>
        public bool CanUndo => _commandHistory.CanUndo;

        /// <summary>
        /// Gets whether there is at least one command available to redo.
        /// </summary>
        public bool CanRedo => _commandHistory.CanRedo;
        
        /// <summary>
        /// Checks if the mouse pointer is currently over any UI Toolkit panel.
        /// </summary>
        /// <returns>True if pointer is over UI, false otherwise.</returns>
        private bool IsPointerOverUI()
        {
            if (_uiDocuments == null) return false;
            
            foreach (var doc in _uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null) continue;
                var panel = doc.rootVisualElement.panel;
                if (panel == null) continue;
                
                Vector2 screenPos = Input.mousePosition;
                // Convert screen position to panel position (screen Y is inverted for UI Toolkit)
                Vector2 panelPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);
                
                var picked = panel.Pick(panelPos);
                // Skip null, root, and TemplateContainer (Unity's implicit UXML wrapper)
                if (picked != null 
                    && picked != doc.rootVisualElement
                    && !(picked is TemplateContainer))
                    return true;
            }
            return false;
        }

        private bool IsPointerOverRealUI()
        {
            if (_uiDocuments == null)
                return false;

            foreach (var doc in _uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null)
                    continue;

                var panel = doc.rootVisualElement.panel;
                if (panel == null)
                    continue;

                Vector2 screenPos = Input.mousePosition;
                Vector2 panelPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);

                var picked = panel.Pick(panelPos);
                if (picked == null || picked == doc.rootVisualElement || picked is TemplateContainer)
                    continue;

                var gameView = doc.rootVisualElement.Q<VisualElement>("GameView");
                if (gameView != null && IsChildOf(picked, gameView))
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsChildOf(VisualElement element, VisualElement parent)
        {
            var current = element;
            while (current != null)
            {
                if (current == parent)
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
