using UnityEngine;
using CircuitCraft.Core;
using CircuitCraft.Components;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Commands;
using CircuitCraft.Utils;
using System.Collections.Generic;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Handles component selection and deletion via mouse and keyboard input.
    /// - Click a component to select it (displays selection highlight)
    /// - Delete key removes the selected component from BoardState and destroys its GameObject
    /// </summary>
    public class ComponentInteraction : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Reference to the GameManager for accessing BoardState.")]
        private GameManager _gameManager;

        [SerializeField]
        [Tooltip("Stage manager used to refresh cached board references after stage loads.")]
        private StageManager _stageManager;
        
        [SerializeField]
        [Tooltip("Camera used for raycasting (defaults to Camera.main if not set).")]
        private Camera _camera;

        [SerializeField]
        [Tooltip("Grid settings for position snapping during drag.")]
        private GridSettings _gridSettings;

        [SerializeField]
        [Tooltip("Wire routing controller used to block component interaction while wiring.")]
        private WireRoutingController _wireRoutingController;
        
        [Header("Raycast Settings")]
        [SerializeField]
        [Tooltip("Maximum raycast distance for component detection.")]
        private float _raycastDistance = 100f;
        
        // State
        private ComponentView _selectedComponent;
        private BoardState _boardState;
        private CommandHistory _commandHistory;
        private System.Action<string> _onBoardLoadedHandler;
        private ComponentView _pressedComponent;
        private bool _isDragging;
        private Vector2Int _dragStartGridPos;
        private Vector3 _mouseDownPos;
        private const float DragThreshold = 5f;
        private float _rotateClickArmUntil = float.NegativeInfinity;
        private const float RotateClickArmDuration = 0.8f;
        private int _escapeConsumedFrame = -1;
        
        private void Awake() => Init();
        
        private void Init()
        {
            InitializeCamera();
        }
        
        private void InitializeCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning("ComponentInteraction: No Camera assigned and Camera.main is null.", this);
                }
            }
        }
        
        private void Start()
        {
            // Get BoardState from GameManager
            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                _commandHistory = _gameManager.CommandHistory;
            }
            else
            {
                Debug.LogError("ComponentInteraction: GameManager reference is missing.", this);
            }

            if (_wireRoutingController == null)
            {
                _wireRoutingController = FindObjectOfType<WireRoutingController>();
            }

            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleBoardReset;

            if (_gameManager != null)
            {
                _onBoardLoadedHandler = _ => HandleBoardReset();
                _gameManager.OnBoardLoaded += _onBoardLoadedHandler;
            }
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;

            if (_gameManager != null)
                _gameManager.OnBoardLoaded -= _onBoardLoadedHandler;
        }
        
        private void Update()
        {
            if (IsWireRoutingBlockingInteraction())
            {
                ResetPointerInteractionState();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && TryConsumeEscape())
                return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                _rotateClickArmUntil = Time.unscaledTime + RotateClickArmDuration;
            }

            HandleSelection();
            HandleDeletion();
        }
        
        /// <summary>
        /// Handles mouse click input for component selection.
        /// Left-click on a component to select it, or click empty space to deselect.
        /// </summary>
        private void HandleSelection()
        {
            if (_camera == null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _mouseDownPos = Input.mousePosition;
                _isDragging = false;

                if (TryRaycastComponent(out ComponentView view, out bool hitPinDot))
                {
                    if (hitPinDot)
                    {
                        // Pin clicks are reserved for wiring interactions.
                        _pressedComponent = null;
                        return;
                    }

                    bool rotateArmed = Input.GetKey(KeyCode.R) || Time.unscaledTime <= _rotateClickArmUntil;
                    if (rotateArmed)
                    {
#if UNITY_EDITOR
                        Debug.Log($"ComponentInteraction: Rotate click on '{view.name}' (keyHeld={Input.GetKey(KeyCode.R)}, armedUntil={_rotateClickArmUntil:F2}, now={Time.unscaledTime:F2})", this);
#endif
                        RotateComponent(view);
                        _rotateClickArmUntil = float.NegativeInfinity;
                        _pressedComponent = null;
                        return;
                    }

                    _pressedComponent = view;
                    _dragStartGridPos = view.GridPosition;
                    SelectComponent(view);
                    return;
                }

                _pressedComponent = null;

                // Clicked empty space - deselect
                DeselectAll();
            }

            if (Input.GetMouseButton(0) && _pressedComponent != null)
            {
                float dragDistance = Vector2.Distance(Input.mousePosition, _mouseDownPos);
                if (!_isDragging && dragDistance >= DragThreshold)
                {
                    _isDragging = true;
                    SelectComponent(_pressedComponent);
                }

                if (_isDragging)
                    UpdateDragPosition();
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_pressedComponent == null)
                    return;

                if (_isDragging)
                    FinalizeDragMove();
                else
                    SelectComponent(_pressedComponent);

                _pressedComponent = null;
                _isDragging = false;
            }
        }

        private bool TryRaycastComponent(out ComponentView view, out bool hitPinDot)
        {
            view = null;
            hitPinDot = false;

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _raycastDistance))
                return false;

            hitPinDot = hit.collider is SphereCollider
                        && hit.collider.gameObject.name.StartsWith("PinDot_");

            view = hit.collider.GetComponent<ComponentView>();
            if (view == null)
                view = hit.collider.GetComponentInParent<ComponentView>();

            return view != null;
        }

        private void UpdateDragPosition()
        {
            if (_selectedComponent == null || _gridSettings == null)
                return;

            Vector2Int dragGridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            Vector3 dragWorldPos = GridUtility.GridToWorldPosition(
                dragGridPos,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            _selectedComponent.transform.position = dragWorldPos;
        }

        private void FinalizeDragMove()
        {
            if (_selectedComponent == null || _gridSettings == null || _boardState is null || _commandHistory == null)
                return;

            Vector2Int targetGridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            GridPosition startPosition = new(_dragStartGridPos.x, _dragStartGridPos.y);
            PlacedComponent placedComponent = _boardState.GetComponentAt(startPosition);
            if (placedComponent is null)
                return;

            bool isSameCell = targetGridPos == _dragStartGridPos;
            if (isSameCell)
            {
                SnapSelectedToGrid(_dragStartGridPos);
                return;
            }

            GridPosition targetPosition = new(targetGridPos.x, targetGridPos.y);
            if (!PlacementValidator.IsValidPlacement(_boardState, targetPosition))
            {
                SnapSelectedToGrid(_dragStartGridPos);
                return;
            }

            DeselectAll();

            _commandHistory.ExecuteCommand(new RemoveComponentCommand(_boardState, placedComponent.InstanceId, allowFixed: true));
            _commandHistory.ExecuteCommand(new PlaceComponentCommand(
                _boardState,
                placedComponent.ComponentDefinitionId,
                targetPosition,
                placedComponent.Rotation,
                ClonePinsWithoutNet(placedComponent.Pins),
                placedComponent.CustomValue,
                placedComponent.IsFixed));
        }

        private void RotateComponent(ComponentView view)
        {
            if (view == null || _boardState is null || _commandHistory == null)
                return;

            GridPosition boardPosition = new(view.GridPosition.x, view.GridPosition.y);
            PlacedComponent placedComponent = _boardState.GetComponentAt(boardPosition);
            if (placedComponent is null)
                return;

            int rotated = (placedComponent.Rotation + RotationConstants.Quarter) % RotationConstants.Full;

            DeselectAll();

            _commandHistory.ExecuteCommand(new RemoveComponentCommand(_boardState, placedComponent.InstanceId, allowFixed: true));
            _commandHistory.ExecuteCommand(new PlaceComponentCommand(
                _boardState,
                placedComponent.ComponentDefinitionId,
                placedComponent.Position,
                rotated,
                ClonePinsWithoutNet(placedComponent.Pins),
                placedComponent.CustomValue,
                placedComponent.IsFixed));

#if UNITY_EDITOR
            Debug.Log($"ComponentInteraction: Rotated '{placedComponent.ComponentDefinitionId}' at {placedComponent.Position} from {placedComponent.Rotation} to {rotated} (fixed={placedComponent.IsFixed})", this);
#endif
        }

        private static List<PinInstance> ClonePinsWithoutNet(IReadOnlyList<PinInstance> pins)
        {
            var clonedPins = new List<PinInstance>(pins?.Count ?? 0);
            if (pins is null)
            {
                return clonedPins;
            }

            for (int i = 0; i < pins.Count; i++)
            {
                PinInstance source = pins[i];
                if (source is null)
                {
                    continue;
                }

                clonedPins.Add(new PinInstance(source.PinIndex, source.PinName, source.LocalPosition));
            }

            return clonedPins;
        }

        private void SnapSelectedToGrid(Vector2Int gridPos)
        {
            if (_selectedComponent == null || _gridSettings == null)
                return;

            Vector3 snappedPosition = GridUtility.GridToWorldPosition(
                gridPos,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            _selectedComponent.transform.position = snappedPosition;
        }
        
        /// <summary>
        /// Handles Delete key input for removing the selected component.
        /// Calls BoardState.RemoveComponent() and destroys the ComponentView GameObject.
        /// </summary>
        private void HandleDeletion()
        {
            if (IsWireRoutingBlockingInteraction())
                return;

            if (Input.GetKeyDown(KeyCode.Delete) && _selectedComponent != null)
            {
                DeleteSelectedComponent();
            }
        }
        
        /// <summary>
        /// Selects a component and applies visual feedback.
        /// Only one component can be selected at a time.
        /// </summary>
        /// <param name="view">ComponentView to select.</param>
        private void SelectComponent(ComponentView view)
        {
            if (view == null) return;
            
            // Deselect previous component
            if (_selectedComponent != null && _selectedComponent != view)
            {
                _selectedComponent.SetSelected(false);
            }
            
            // Select new component
            _selectedComponent = view;
            _selectedComponent.SetSelected(true);
        }
        
        /// <summary>
        /// Deselects the currently selected component.
        /// </summary>
        public void DeselectAll()
        {
            if (_selectedComponent != null)
            {
                _selectedComponent.SetSelected(false);
                _selectedComponent = null;
            }
        }

        /// <summary>
        /// Gets whether this controller consumed ESC on the current frame.
        /// </summary>
        public bool ConsumedEscapeThisFrame => _escapeConsumedFrame == Time.frameCount;

        /// <summary>
        /// Tries to consume ESC by canceling current component interaction.
        /// Returns true when ESC was consumed.
        /// </summary>
        public bool TryConsumeEscape()
        {
            bool hadPointerInteraction = _pressedComponent != null || _isDragging;
            bool hadSelection = _selectedComponent != null;
            if (!hadPointerInteraction && !hadSelection)
                return false;

            if (_isDragging)
                SnapSelectedToGrid(_dragStartGridPos);

            ResetPointerInteractionState();
            DeselectAll();
            _rotateClickArmUntil = float.NegativeInfinity;
            _escapeConsumedFrame = Time.frameCount;
            return true;
        }

        private void HandleBoardReset()
        {
            ResetPointerInteractionState();
            DeselectAll();

            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                _commandHistory = _gameManager.CommandHistory;
            }
        }
        
        /// <summary>
        /// Deletes the currently selected component.
        /// Removes it from BoardState and destroys the GameObject.
        /// </summary>
        private void DeleteSelectedComponent()
        {
            if (_selectedComponent == null || _boardState is null) return;
            
            // Find the PlacedComponent in BoardState by matching GridPosition
            // (ComponentView stores GridPosition, PlacedComponent has InstanceId)
            Vector2Int gridPos = _selectedComponent.GridPosition;
            
            // Search for component at this grid position
            GridPosition boardPosition = new(gridPos.x, gridPos.y);
            PlacedComponent placedComponent = _boardState.GetComponentAt(boardPosition);
            
            if (placedComponent is not null)
            {
                int instanceId = placedComponent.InstanceId;
                
                // Remove from BoardState via command history (enables undo)
                _commandHistory.ExecuteCommand(new RemoveComponentCommand(_boardState, instanceId));
                
                // BoardView handles GameObject destruction via OnComponentRemoved event
                _selectedComponent = null;
                
#if UNITY_EDITOR
                Debug.Log($"ComponentInteraction: Deleted component {instanceId} at position {gridPos}");
#endif
            }
            else
            {
                Debug.LogWarning($"ComponentInteraction: Could not find PlacedComponent at position {gridPos}.", this);
                
                // Clean up orphaned ComponentView
                GameObject componentObject = _selectedComponent.gameObject;
                _selectedComponent = null;
                Destroy(componentObject);
            }
        }
        
        /// <summary>
        /// Gets the currently selected component.
        /// </summary>
        /// <returns>Selected ComponentView, or null if none selected.</returns>
        public ComponentView GetSelectedComponent()
            => _selectedComponent;

        private bool IsWireRoutingBlockingInteraction()
        {
            return _wireRoutingController != null
                && (_wireRoutingController.IsWiringModeActive || _wireRoutingController.IsRoutingInProgress);
        }

        private void ResetPointerInteractionState()
        {
            _pressedComponent = null;
            _isDragging = false;
        }
    }
}
