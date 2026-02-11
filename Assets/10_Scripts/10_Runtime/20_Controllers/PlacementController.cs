using UnityEngine;
using CircuitCraft.Data;
using CircuitCraft.Core;
using CircuitCraft.Utils;
using CircuitCraft.Managers;
using CircuitCraft.Components;
using System.Collections.Generic;
using System.Linq;

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
        [Tooltip("Size of each grid cell in world units.")]
        private float _cellSize = 1.0f;
        
        [SerializeField]
        [Tooltip("World position of the grid's origin (0,0).")]
        private Vector3 _gridOrigin = Vector3.zero;
        
        [SerializeField]
        [Tooltip("Width of the grid (number of cells).")]
        private int _gridWidth = 20;
        
        [SerializeField]
        [Tooltip("Height of the grid (number of cells).")]
        private int _gridHeight = 20;
        
        // State
        private ComponentDefinition _selectedComponent;
        private GameObject _previewInstance;
        
        private void Awake()
        {
            // Auto-assign camera if not set
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogError("PlacementController: No camera assigned and Camera.main is null!");
                }
            }
            
            // Validate dependencies
            if (_gameManager == null)
            {
                Debug.LogError("PlacementController: GameManager reference is missing!");
            }
            
            if (_componentViewPrefab == null)
            {
                Debug.LogWarning("PlacementController: ComponentView prefab is not assigned!");
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
        /// Updates the preview instance position and visual state.
        /// Preview follows cursor and shows valid/invalid placement state.
        /// </summary>
        private void UpdatePreview()
        {
            if (_selectedComponent == null || _previewInstance == null)
                return;
            
            // Get cursor grid position
            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition,
                _camera,
                _cellSize,
                _gridOrigin
            );
            
            // Check if position is valid
            bool isValid = IsValidPlacement(gridPos);
            
            // Update preview world position
            Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _cellSize, _gridOrigin);
            _previewInstance.transform.position = worldPos;
            
            // Update preview color (use hover color for invalid state)
            ComponentView previewView = _previewInstance.GetComponent<ComponentView>();
            if (previewView != null)
            {
                previewView.SetHovered(!isValid);
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
            
            // Initialize ComponentView with definition
            ComponentView view = _previewInstance.GetComponent<ComponentView>();
            if (view != null)
            {
                view.Initialize(_selectedComponent);
            }
            
            // Make semi-transparent
            SpriteRenderer sr = _previewInstance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.5f; // Semi-transparent
                sr.color = c;
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
            }
        }
        
        /// <summary>
        /// Handles mouse input for component placement.
        /// Checks for left mouse button down, validates position, and places component.
        /// </summary>
        private void HandlePlacement()
        {
            // Only process input if we have a component selected
            if (_selectedComponent == null)
                return;
            
            // Check for left mouse button down
            if (Input.GetMouseButtonDown(0))
            {
                // Convert mouse position to grid coordinates
                Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                    Input.mousePosition,
                    _camera,
                    _cellSize,
                    _gridOrigin
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
        /// Validates position is within grid bounds and not already occupied.
        /// </summary>
        /// <param name="gridPos">Grid position to validate.</param>
        /// <returns>True if placement is valid, false otherwise.</returns>
        private bool IsValidPlacement(Vector2Int gridPos)
        {
            // Check grid bounds
            if (!GridUtility.IsValidGridPosition(gridPos, _gridWidth, _gridHeight))
            {
                return false;
            }
            
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
                    // Assuming PinDefinition has a LocalPosition field - if not, use default (0,0)
                    GridPosition pinLocalPos = new GridPosition(0, 0); // TODO: Get from pinDef.LocalPosition when available
                    
                    PinInstance pinInstance = new PinInstance(
                        pinIndex: i,
                        pinName: pinDef.ToString(), // TODO: Get actual pin name from PinDefinition
                        localPosition: pinLocalPos
                    );
                    
                    pinInstances.Add(pinInstance);
                }
            }
            
            // Place component in BoardState
            GridPosition position = new GridPosition(gridPos.x, gridPos.y);
            PlacedComponent placedComponent = _gameManager.BoardState.PlaceComponent(
                componentDefId: _selectedComponent.Id,
                position: position,
                rotation: 0, // Default rotation
                pins: pinInstances
            );
            
            Debug.Log($"PlacementController: Placed {_selectedComponent.DisplayName} at {position}");
            
            // Instantiate ComponentView prefab at world position
            if (_componentViewPrefab != null)
            {
                Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _cellSize, _gridOrigin);
                GameObject viewObject = Instantiate(_componentViewPrefab, worldPos, Quaternion.identity);
                
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
        /// Gets the currently selected component definition.
        /// </summary>
        /// <returns>Currently selected ComponentDefinition, or null if none selected.</returns>
        public ComponentDefinition GetSelectedComponent()
        {
            return _selectedComponent;
        }
    }
}
