using UnityEngine;

namespace CircuitCraft.Data
{
    /// <summary>
    /// ScriptableObject that consolidates grid configuration settings.
    /// Used by PlacementController, BoardView, GridCursor, and GridRenderer
    /// to maintain consistent grid parameters across the application.
    /// </summary>
    [CreateAssetMenu(fileName = "GridSettings", menuName = "CircuitCraft/Settings/Grid Settings")]
    public class GridSettings : ScriptableObject
    {
        [Header("Grid Cell Configuration")]
        [SerializeField]
        [Tooltip("Size of each grid cell in world units.")]
        private float _cellSize = 1.0f;
        
        [SerializeField]
        [Tooltip("World position of the grid's origin (0,0).")]
        private Vector3 _gridOrigin = Vector3.zero;
        
        [Header("Grid Dimensions")]
        [SerializeField]
        [Tooltip("Width of the grid (number of cells).")]
        private int _boardWidth = 20;
        
        [SerializeField]
        [Tooltip("Height of the grid (number of cells).")]
        private int _boardHeight = 20;
        
        /// <summary>
        /// Size of each grid cell in world units.
        /// </summary>
        public float CellSize => _cellSize;
        
        /// <summary>
        /// World position of the grid's origin (0,0).
        /// </summary>
        public Vector3 GridOrigin => _gridOrigin;
        
        /// <summary>
        /// Width of the grid (number of cells).
        /// </summary>
        public int BoardWidth => _boardWidth;
        
        /// <summary>
        /// Height of the grid (number of cells).
        /// </summary>
        public int BoardHeight => _boardHeight;
    }
}
