using System;
using UnityEngine;
using CircuitCraft.Data;
using CircuitCraft.Utils;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Visual cursor showing current grid cell under mouse.
    /// Provides real-time visual feedback for component placement.
    /// </summary>
    public class GridCursor : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("Camera used to project mouse position onto the board plane.")]
        [SerializeField] private Camera _camera;
        
        [Header("Grid Settings")]
        [Tooltip("Grid settings asset used for screen-to-grid and grid-to-world conversion.")]
        [SerializeField] private GridSettings _gridSettings;
        
        [Header("Visual Settings")]
        [Tooltip("Sprite renderer for the cursor visual.")]
        [SerializeField] private SpriteRenderer _cursorSprite;
        [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);

        private Vector2Int _currentGridPosition;
        private Vector3 _lastMousePosition;
        private bool _isOverGrid;

        /// <summary>
        /// Raised when the snapped grid cursor position changes.
        /// </summary>
        public event Action OnPositionChanged;
        
        private void Awake() => Init();

        private void Init()
        {
            InitializeCamera();
            InitializeCursorSprite();
            ValidateGridSettings();
            SetupInitialCursorState();
        }

        private void InitializeCamera()
        {
            // Auto-assign camera if not set
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void InitializeCursorSprite()
        {
            // Auto-assign sprite renderer if not set
            if (_cursorSprite == null)
            {
                _cursorSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void ValidateGridSettings()
        {
            // Validate GridSettings reference
            if (_gridSettings == null)
            {
                Debug.LogError("GridCursor: GridSettings reference is missing!");
            }
        }

        private void SetupInitialCursorState()
        {
            // Lay cursor sprite flat on XZ plane for top-down camera
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Initialize cursor with valid color
            if (_cursorSprite != null)
            {
                _cursorSprite.color = _validColor;
            }
        }
        
        private void Update()
        {
            if (Input.mousePosition == _lastMousePosition)
            {
                return;
            }

            UpdateCursorPosition();
        }
        
        /// <summary>
        /// Updates cursor position to follow mouse and snap to grid.
        /// </summary>
        private void UpdateCursorPosition()
        {
            _lastMousePosition = Input.mousePosition;

            if (_camera == null || _gridSettings == null)
            {
                bool wasOverGrid = _isOverGrid;
                _isOverGrid = false;
                SetCursorVisible(false);
                if (wasOverGrid)
                    OnPositionChanged?.Invoke();
                return;
            }
            
            // Convert mouse position to grid coordinates
            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition, 
                _camera, 
                _gridSettings.CellSize, 
                _gridSettings.GridOrigin
            );
            
            // Update current position - grid is unbounded, always over grid
            bool positionChanged = !_isOverGrid || _currentGridPosition != gridPos;
            _currentGridPosition = gridPos;
            _isOverGrid = true;
            
            // Update world position - always show cursor at any coordinate
            Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _gridSettings.CellSize, _gridSettings.GridOrigin);
            transform.position = worldPos;
            SetCursorVisible(true);
            
            // Color hint: green inside suggested area, red outside
            bool insideSuggested = GridUtility.IsInsideSuggestedArea(gridPos, _gridSettings.SuggestedWidth, _gridSettings.SuggestedHeight);
            SetCursorColor(insideSuggested ? _validColor : _invalidColor);

            if (positionChanged)
                OnPositionChanged?.Invoke();
        }
        
        /// <summary>
        /// Shows or hides the cursor sprite.
        /// </summary>
        private void SetCursorVisible(bool isVisible)
        {
            if (_cursorSprite != null)
            {
                _cursorSprite.enabled = isVisible;
            }
        }
        
        /// <summary>
        /// Sets the cursor color (for valid/invalid feedback).
        /// </summary>
        private void SetCursorColor(Color color)
        {
            if (_cursorSprite != null)
            {
                _cursorSprite.color = color;
            }
        }
        
        /// <summary>
        /// Gets the current grid position under the cursor.
        /// </summary>
        public Vector2Int GetCurrentGridPosition() => _currentGridPosition;
        
        /// <summary>
        /// Checks if the cursor is currently over a valid grid position.
        /// </summary>
        public bool IsOverValidGrid() => _isOverGrid;
        
        /// <summary>
        /// Sets cursor to invalid state (for preview of blocked placement).
        /// </summary>
        public void SetInvalid(bool isInvalid)
        {
            if (_cursorSprite != null)
            {
                _cursorSprite.color = isInvalid ? _invalidColor : _validColor;
            }
        }
    }
}
