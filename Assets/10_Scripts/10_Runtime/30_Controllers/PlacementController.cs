using UnityEngine;
using CircuitCraft.Data;
using CircuitCraft.Core;
using CircuitCraft.Utils;
using CircuitCraft.Managers;
using CircuitCraft.Components;
using CircuitCraft.Commands;
using System.Collections.Generic;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Handles component placement on the grid with mouse input.
    /// Detects left-click, converts screen position to grid coordinates, validates placement,
    /// and instantiates ComponentView prefabs at snapped grid positions.
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
        [Tooltip("Prefab to instantiate when placing components.")]
        private GameObject _componentViewPrefab;
        
        [Header("Grid Settings")]
        [SerializeField]
        [Tooltip("Grid configuration settings (cell size, origin, dimensions).")]
        private GridSettings _gridSettings;

        [Header("Command History")]
        [SerializeField]
        [Tooltip("Tracks placement commands for undo and redo.")]
        private CommandHistory _commandHistory = new CommandHistory();
        
        // State
        private ComponentDefinition _selectedComponent;
        private GameObject _previewInstance;
        private int _currentRotation = 0;
        private float? _customValue;
        
        // Cached preview component references
        private ComponentView _cachedPreviewView;
        private SpriteRenderer _cachedPreviewSprite;
        
        private void Awake() => Init();
        
        private void Init()
        {
            InitializeCamera();
            ValidateDependencies();
        }
        
        private void InitializeCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogError("PlacementController: No camera assigned and Camera.main is null!");
                }
            }
        }
        
        private void ValidateDependencies()
        {
            if (_gameManager == null)
            {
                Debug.LogError("PlacementController: GameManager reference is missing!");
            }
            
            if (_componentViewPrefab == null)
            {
                Debug.LogWarning("PlacementController: ComponentView prefab is not assigned!");
            }
            
            if (_gridSettings == null)
            {
                Debug.LogError("PlacementController: GridSettings reference is missing!");
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup preview instance
            DestroyPreview();
        }
        
        private void Update()
        {
            HandleCancellation();
            HandleRotation();
            UpdatePreview();
            HandlePlacement();
        }
        
        /// <summary>
        /// Handles right-click input to cancel component placement.
        /// </summary>
        private void HandleCancellation()
        {
            // Right-click to cancel selection
            if (Input.GetMouseButtonDown(1))
            {
                if (_selectedComponent != null)
                {
                    SetSelectedComponent(null);
                }
            }
        }
        
        /// <summary>
        /// Handles R key input to rotate component preview by 90° clockwise.
        /// </summary>
        private void HandleRotation()
        {
            // R key to rotate component preview
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_selectedComponent != null)
                {
                    // Cycle rotation: 0 → 90 → 180 → 270 → 0
                    _currentRotation = (_currentRotation + 90) % 360;
                    
                    // Apply rotation to preview instance
                    if (_previewInstance != null)
                    {
                        // Negative because Unity Z-rotation is counter-clockwise, we want clockwise visual
                        _previewInstance.transform.rotation = Quaternion.Euler(0, 0, -_currentRotation);
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the preview instance position and visual state.
        /// Preview follows cursor and shows valid/invalid placement state.
        /// </summary>
        private void UpdatePreview()
        {
            if (_selectedComponent == null || _previewInstance == null || _gridSettings == null)
                return;
            
            // Get cursor grid position
            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            
            // Check if position is valid
            bool isValid = IsValidPlacement(gridPos);
            
            // Update preview world position
            Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _gridSettings.CellSize, _gridSettings.GridOrigin);
            _previewInstance.transform.position = worldPos;
            
            // Update preview color (use hover color for invalid state) - using cached reference
            if (_cachedPreviewView != null)
            {
                _cachedPreviewView.SetHovered(!isValid);
            }
        }
        
        /// <summary>
        /// Creates a preview instance of the selected component.
        /// Preview is semi-transparent and follows cursor.
        /// </summary>
        private void CreatePreview()
        {
            if (_componentViewPrefab == null || _selectedComponent == null)
                return;
            
            // Instantiate preview at origin (will be positioned in UpdatePreview)
            _previewInstance = Instantiate(_componentViewPrefab, Vector3.zero, Quaternion.identity);
            
            // Cache ComponentView reference
            _cachedPreviewView = _previewInstance.GetComponent<ComponentView>();
            if (_cachedPreviewView != null)
            {
                _cachedPreviewView.Initialize(_selectedComponent);
            }
            
            // Cache SpriteRenderer reference and make semi-transparent
            _cachedPreviewSprite = _previewInstance.GetComponent<SpriteRenderer>();
            if (_cachedPreviewSprite != null)
            {
                Color c = _cachedPreviewSprite.color;
                c.a = 0.5f; // Semi-transparent
                _cachedPreviewSprite.color = c;
            }
        }
        
        /// <summary>
        /// Destroys the preview instance if it exists.
        /// </summary>
        private void DestroyPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
                
                // Clear cached references
                _cachedPreviewView = null;
                _cachedPreviewSprite = null;
            }
        }
        
        /// <summary>
        /// Handles mouse input for component placement.
        /// Checks for left mouse button down, validates position, and places component.
        /// </summary>
        private void HandlePlacement()
        {
            // Only process input if we have a component selected
            if (_selectedComponent == null || _gridSettings == null)
                return;
            
            // Check for left mouse button down
            if (Input.GetMouseButtonDown(0))
            {
                // Convert mouse position to grid coordinates
                Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                    Input.mousePosition,
                    _camera,
                    _gridSettings.CellSize,
                    _gridSettings.GridOrigin
                );
                
                // Validate and place component
                if (IsValidPlacement(gridPos))
                {
                    PlaceComponent(gridPos);
                }
                else
                {
                    Debug.Log($"PlacementController: Invalid placement at {gridPos}");
                }
            }
        }
        
        /// <summary>
        /// Checks if a component can be placed at the given grid position.
        /// Validates position is not already occupied (grid is unbounded).
        /// </summary>
        /// <param name="gridPos">Grid position to validate.</param>
        /// <returns>True if placement is valid, false otherwise.</returns>
        private bool IsValidPlacement(Vector2Int gridPos)
        {
            // Check if position is already occupied
            if (_gameManager != null && _gameManager.BoardState != null)
            {
                GridPosition checkPos = new GridPosition(gridPos.x, gridPos.y);
                if (_gameManager.BoardState.IsPositionOccupied(checkPos))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Places a component at the specified grid position.
        /// Creates a PlacedComponent in BoardState and instantiates the visual ComponentView.
        /// </summary>
        /// <param name="gridPos">Grid position to place component at.</param>
        private void PlaceComponent(Vector2Int gridPos)
        {
            if (_gameManager == null || _gameManager.BoardState == null)
            {
                Debug.LogError("PlacementController: Cannot place component - GameManager or BoardState is null!");
                return;
            }
            
            // Create pin instances from component definition
            List<PinInstance> pinInstances = new List<PinInstance>();
            if (_selectedComponent.Pins != null)
            {
                for (int i = 0; i < _selectedComponent.Pins.Length; i++)
                {
                    var pinDef = _selectedComponent.Pins[i];
                    
                    // Convert PinDefinition local position to GridPosition
                    GridPosition pinLocalPos = new GridPosition(pinDef.LocalPosition.x, pinDef.LocalPosition.y);
                    
                    PinInstance pinInstance = new PinInstance(
                        pinIndex: i,
                        pinName: pinDef.PinName,
                        localPosition: pinLocalPos
                    );
                    
                    pinInstances.Add(pinInstance);
                }
            }
            
            // Place component in BoardState
            GridPosition position = new GridPosition(gridPos.x, gridPos.y);
            var placeCommand = new PlaceComponentCommand(
                _gameManager.BoardState,
                _selectedComponent.Id,
                position,
                _currentRotation,
                pinInstances,
                _customValue
            );
            _commandHistory.ExecuteCommand(placeCommand);
            
            Debug.Log($"PlacementController: Placed {_selectedComponent.DisplayName} at {position}");
            
            // Instantiate ComponentView prefab at world position
            if (_componentViewPrefab != null && _gridSettings != null)
            {
                Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _gridSettings.CellSize, _gridSettings.GridOrigin);
                GameObject viewObject = Instantiate(_componentViewPrefab, worldPos, Quaternion.Euler(0, 0, -_currentRotation));
                
                // Initialize ComponentView with definition
                ComponentView componentView = viewObject.GetComponent<ComponentView>();
                if (componentView != null)
                {
                    componentView.Initialize(_selectedComponent);
                    componentView.GridPosition = gridPos;
                }
                else
                {
                    Debug.LogWarning("PlacementController: ComponentView prefab does not have ComponentView component!");
                }
            }
        }
        
        /// <summary>
        /// Sets the currently selected component definition for placement.
        /// Creates/destroys preview instance when selection changes.
        /// </summary>
        /// <param name="definition">ComponentDefinition to place, or null to deselect.</param>
        public void SetSelectedComponent(ComponentDefinition definition)
        {
            _selectedComponent = definition;
            
            // Reset rotation when selecting a new component
            _currentRotation = 0;
            _customValue = null;
            
            // Destroy old preview
            DestroyPreview();
            
            if (_selectedComponent != null)
            {
                Debug.Log($"PlacementController: Selected component: {_selectedComponent.DisplayName}");
                
                // Create new preview
                CreatePreview();
            }
            else
            {
                Debug.Log("PlacementController: Deselected component");
            }
        }

        /// <summary>
        /// Undoes the last executed placement command.
        /// </summary>
        public void UndoLastAction()
        {
            _commandHistory.Undo();
        }

        /// <summary>
        /// Redoes the most recently undone placement command.
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
        /// Gets the currently selected component definition.
        /// </summary>
        /// <returns>Currently selected ComponentDefinition, or null if none selected.</returns>
        public ComponentDefinition GetSelectedComponent()
        {
            return _selectedComponent;
        }

        /// <summary>
        /// Sets the custom electrical value for the next placed component.
        /// Applies to resistors (Ω), capacitors (F), inductors (H), voltage sources (V), and current sources (A).
        /// Pass null to use the component definition's default value.
        /// </summary>
        /// <param name="value">Custom value, or null to use default.</param>
        public void SetCustomValue(float? value)
        {
            _customValue = value;
        }

        /// <summary>
        /// Gets the current custom electrical value that will be applied to the next placed component.
        /// </summary>
        /// <returns>Custom value, or null if using definition default.</returns>
        public float? GetCustomValue()
        {
            return _customValue;
        }
    }
}
