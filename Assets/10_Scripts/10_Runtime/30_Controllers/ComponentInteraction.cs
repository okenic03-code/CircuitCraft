using UnityEngine;
using CircuitCraft.Core;
using CircuitCraft.Components;
using CircuitCraft.Managers;

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
        [Tooltip("Camera used for raycasting (defaults to Camera.main if not set).")]
        private Camera _camera;
        
        [Header("Raycast Settings")]
        [SerializeField]
        [Tooltip("Maximum raycast distance for component detection.")]
        private float _raycastDistance = 100f;
        
        // State
        private ComponentView _selectedComponent;
        private BoardState _boardState;
        
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
            }
            else
            {
                Debug.LogError("ComponentInteraction: GameManager reference is missing.", this);
            }
        }
        
        private void Update()
        {
            HandleSelection();
            HandleDeletion();
        }
        
        /// <summary>
        /// Handles mouse click input for component selection.
        /// Left-click on a component to select it, or click empty space to deselect.
        /// </summary>
        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_camera == null) return;
                
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, _raycastDistance))
                {
                    // Try to get ComponentView from hit collider or parent
                    ComponentView view = hit.collider.GetComponent<ComponentView>();
                    if (view == null)
                    {
                        view = hit.collider.GetComponentInParent<ComponentView>();
                    }
                    
                    if (view != null)
                    {
                        SelectComponent(view);
                        return;
                    }
                }
                
                // Clicked empty space - deselect
                DeselectAll();
            }
        }
        
        /// <summary>
        /// Handles Delete key input for removing the selected component.
        /// Calls BoardState.RemoveComponent() and destroys the ComponentView GameObject.
        /// </summary>
        private void HandleDeletion()
        {
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
        /// Deletes the currently selected component.
        /// Removes it from BoardState and destroys the GameObject.
        /// </summary>
        private void DeleteSelectedComponent()
        {
            if (_selectedComponent == null || _boardState == null) return;
            
            // Find the PlacedComponent in BoardState by matching GridPosition
            // (ComponentView stores GridPosition, PlacedComponent has InstanceId)
            Vector2Int gridPos = _selectedComponent.GridPosition;
            
            // Search for component at this grid position
            var boardPosition = new GridPosition(gridPos.x, gridPos.y);
            PlacedComponent placedComponent = _boardState.GetComponentAt(boardPosition);
            
            if (placedComponent != null)
            {
                int instanceId = placedComponent.InstanceId;
                
                // Remove from BoardState
                bool isRemoved = _boardState.RemoveComponent(instanceId);
                
                if (isRemoved)
                {
                    // Destroy the GameObject
                    GameObject componentObject = _selectedComponent.gameObject;
                    _selectedComponent = null; // Clear reference before destroying
                    Destroy(componentObject);
                    
#if UNITY_EDITOR
                    Debug.Log($"ComponentInteraction: Deleted component {instanceId} at position {gridPos}");
#endif
                }
                else
                {
                    Debug.LogWarning($"ComponentInteraction: Failed to remove component {instanceId} from BoardState.", this);
                }
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
        {
            return _selectedComponent;
        }
    }
}
