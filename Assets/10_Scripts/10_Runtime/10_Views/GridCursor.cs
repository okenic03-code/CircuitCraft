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
        [SerializeField] private Camera _camera;
        
        [Header("Grid Settings")]
        [SerializeField] private GridSettings _gridSettings;
        
        [Header("Visual Settings")]
        [SerializeField] private SpriteRenderer _cursorSprite;
        [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);
        
        private Vector2Int _currentGridPosition;
        private bool _isOverGrid;
        
        private void Awake()
        {
            // Auto-assign camera if not set
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            // Auto-assign sprite renderer if not set
            if (_cursorSprite == null)
            {
                _cursorSprite = GetComponentInChildren<SpriteRenderer>();
            }
            
            // Validate GridSettings
            if (_gridSettings == null)
            {
                Debug.LogError("GridCursor: GridSettings reference is missing!");
            }
            
            // Hide cursor initially
            if (_cursorSprite != null)
            {
                _cursorSprite.color = _validColor;
            }
        }
        
        private void Update()
        {
            UpdateCursorPosition();
        }
        
        /// <summary>
        /// Updates cursor position to follow mouse and snap to grid.
        /// </summary>
        private void UpdateCursorPosition()
        {
            if (_camera == null || _gridSettings == null)
            {
                SetCursorVisible(false);
                return;
            }
            
            // Convert mouse position to grid coordinates
            Vector2Int gridPos = GridUtility.ScreenToGridPosition(
                Input.mousePosition, 
                _camera, 
                _gridSettings.CellSize, 
                _gridSettings.GridOrigin
            );
            
            // Check if position is valid
            bool isValid = GridUtility.IsValidGridPosition(gridPos, _gridSettings.BoardWidth, _gridSettings.BoardHeight);
            
            // Update current position
            _currentGridPosition = gridPos;
            _isOverGrid = isValid;
            
            // Update visual position
            if (isValid)
            {
                Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _gridSettings.CellSize, _gridSettings.GridOrigin);
                transform.position = worldPos;
                SetCursorVisible(true);
                SetCursorColor(_validColor);
            }
            else
            {
                SetCursorVisible(false);
            }
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
        public Vector2Int GetCurrentGridPosition()
        {
            return _currentGridPosition;
        }
        
        /// <summary>
        /// Checks if the cursor is currently over a valid grid position.
        /// </summary>
        public bool IsOverValidGrid()
        {
            return _isOverGrid;
        }
        
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
