using CircuitCraft.Commands;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Handles component selection, preview, rotation, and placement on the board grid.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Reference to GameManager for accessing BoardState.")]
        private GameManager _gameManager;

        [SerializeField]
        [Tooltip("Camera used for raycasting (defaults to Camera.main).")]
        private Camera _camera;

        [SerializeField]
        [Tooltip("Manager responsible for placement preview visuals.")]
        private ComponentPreviewManager _componentPreviewManager;

        [Header("Grid Settings")]
        [SerializeField]
        [Tooltip("Grid configuration settings (cell size, origin, dimensions).")]
        private GridSettings _gridSettings;

        private CommandHistory _commandHistory;

        [Tooltip("UI documents used to block placement input when pointer is over UI.")]
        [SerializeField] private UIDocument[] _uiDocuments;
        private ComponentDefinition _selectedComponent;
        private int _currentRotation;
        private Vector3 _lastMousePosition = Vector3.negativeInfinity;
        private float? _customValue;

        private void Awake() => Init();

        private void Init()
        {
            InitializeCamera();

            if (_componentPreviewManager == null)
                _componentPreviewManager = GetComponent<ComponentPreviewManager>();

            ValidateDependencies();

            if (_gameManager != null)
                _commandHistory = _gameManager.CommandHistory;

        }

        private void InitializeCamera()
        {
            if (_camera != null)
                return;

            _camera = Camera.main;
            if (_camera == null)
                Debug.LogError("PlacementController: No camera assigned and Camera.main is null!");
        }

        private void ValidateDependencies()
        {
            if (_gameManager == null)
                Debug.LogError("PlacementController: GameManager reference is missing!");

            if (_gridSettings == null)
                Debug.LogError("PlacementController: GridSettings reference is missing!");
        }

        private void Update()
        {
            HandleCancellation();
            HandleRotation();

            if (Input.mousePosition != _lastMousePosition)
                UpdatePreview();

            HandlePlacement();
        }

        private void HandleCancellation()
        {
            if (Input.GetMouseButtonDown(1) && _selectedComponent != null)
                SetSelectedComponent(null);
        }

        private void HandleRotation()
        {
            if (!Input.GetKeyDown(KeyCode.R) || _selectedComponent == null)
                return;

            _currentRotation = (_currentRotation + RotationConstants.Quarter) % RotationConstants.Full;
            _lastMousePosition = Vector3.negativeInfinity;
            _componentPreviewManager?.ApplyRotation(_currentRotation);
        }

        private void UpdatePreview()
        {
            if (Input.mousePosition == _lastMousePosition)
                return;

            if (_selectedComponent == null || _gridSettings == null)
                return;

            if (_componentPreviewManager == null || !_componentPreviewManager.HasPreview)
                return;

            _lastMousePosition = Input.mousePosition;

            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            GridPosition checkPos = new(gridPos.x, gridPos.y);
            bool isValid = PlacementValidator.IsValidPlacement(
                _gameManager != null ? _gameManager.BoardState : null,
                checkPos);

            _componentPreviewManager.UpdatePosition(
                gridPos,
                isValid,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);
        }

        private void HandlePlacement()
        {
            if (UIInputHelper.IsPointerOverUI(_uiDocuments))
                return;

            if (_selectedComponent == null || _gridSettings == null)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin);

            GridPosition checkPos = new(gridPos.x, gridPos.y);
            if (PlacementValidator.IsValidPlacement(_gameManager?.BoardState, checkPos))
            {
                PlaceComponent(gridPos);
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"PlacementController: Invalid placement at {gridPos}");
#endif
        }

        private void PlaceComponent(Vector2Int gridPos)
        {
            if (_gameManager == null || _gameManager.BoardState is null)
            {
                Debug.LogError("PlacementController: Cannot place component - GameManager or BoardState is null!");
                return;
            }

            var pinInstances = PinInstanceFactory.CreatePinInstances(_selectedComponent);
            GridPosition position = new(gridPos.x, gridPos.y);
            var placeCommand = new PlaceComponentCommand(
                _gameManager.BoardState,
                _selectedComponent.Id,
                position,
                _currentRotation,
                pinInstances,
                _customValue);

            _commandHistory.ExecuteCommand(placeCommand);

#if UNITY_EDITOR
            Debug.Log($"PlacementController: Placed {_selectedComponent.DisplayName} at {position}");
#endif
        }

        /// <summary>
        /// Sets the currently selected component definition for placement.
        /// </summary>
        /// <param name="definition">Component definition to place, or null to clear selection.</param>
        public void SetSelectedComponent(ComponentDefinition definition)
        {
            _selectedComponent = definition;
            _currentRotation = 0;
            _customValue = null;
            _lastMousePosition = Vector3.negativeInfinity;

            _componentPreviewManager?.DestroyPreview();

            if (_selectedComponent == null)
            {
#if UNITY_EDITOR
                Debug.Log("PlacementController: Deselected component");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"PlacementController: Selected component: {_selectedComponent.DisplayName}");
#endif
            _componentPreviewManager?.CreatePreview(_selectedComponent);
        }

        /// <summary>
        /// Undoes the last placement-related command.
        /// </summary>
        public void UndoLastAction()
        {
            _commandHistory.Undo();
        }

        /// <summary>
        /// Redoes the most recently undone placement-related command.
        /// </summary>
        public void RedoLastAction()
        {
            _commandHistory.Redo();
        }

        /// <summary>
        /// Gets whether there is at least one command available to undo.
        /// </summary>
        /// <returns>True if an undo operation is available; otherwise false.</returns>
        public bool CanUndo => _commandHistory.CanUndo;

        /// <summary>
        /// Gets whether there is at least one command available to redo.
        /// </summary>
        /// <returns>True if a redo operation is available; otherwise false.</returns>
        public bool CanRedo => _commandHistory.CanRedo;

        /// <summary>
        /// Gets the currently selected component definition.
        /// </summary>
        /// <returns>Selected component definition, or null when no component is selected.</returns>
        public ComponentDefinition GetSelectedComponent()
            => _selectedComponent;

        /// <summary>
        /// Sets an optional custom value used when placing the selected component.
        /// </summary>
        /// <param name="value">Custom numeric value to apply, or null to use the default definition value.</param>
        public void SetCustomValue(float? value)
        {
            _customValue = value;
        }

        /// <summary>
        /// Gets the custom placement value currently configured for new placements.
        /// </summary>
        /// <returns>Configured custom value, or null when not set.</returns>
        public float? GetCustomValue()
            => _customValue;
    }
}
