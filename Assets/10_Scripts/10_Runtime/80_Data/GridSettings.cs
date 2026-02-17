using UnityEngine;
using UnityEngine.Serialization;

namespace CircuitCraft.Data
{
    /// <summary>
    /// ScriptableObject that consolidates grid configuration settings.
    /// Used by PlacementController, BoardView, GridCursor, and GridRenderer
    /// to maintain consistent grid parameters across the application.
    /// The grid is now unbounded - suggested dimensions are UI hints only.
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
        
        [Header("Suggested Area")]
        [SerializeField]
        [FormerlySerializedAs("_boardWidth")]
        [Tooltip("Suggested width of the playable area (not a hard limit).")]
        private int _suggestedWidth = 20;
        
        [SerializeField]
        [FormerlySerializedAs("_boardHeight")]
        [Tooltip("Suggested height of the playable area (not a hard limit).")]
        private int _suggestedHeight = 20;
        
        /// <summary>
        /// Size of each grid cell in world units.
        /// </summary>
        public float CellSize => _cellSize;
        
        /// <summary>
        /// World position of the grid's origin (0,0).
        /// </summary>
        public Vector3 GridOrigin => _gridOrigin;
        
        /// <summary>
        /// Suggested width of the playable area (number of cells).
        /// This is a UI hint only - placement is not restricted to this area.
        /// </summary>
        public int SuggestedWidth => _suggestedWidth;
        
        /// <summary>
        /// Suggested height of the playable area (number of cells).
        /// This is a UI hint only - placement is not restricted to this area.
        /// </summary>
        public int SuggestedHeight => _suggestedHeight;
        
        /// <summary>
        /// Width of the grid (number of cells).
        /// </summary>
        [System.Obsolete("Use SuggestedWidth instead")]
        public int BoardWidth => _suggestedWidth;
        
        /// <summary>
        /// Height of the grid (number of cells).
        /// </summary>
        [System.Obsolete("Use SuggestedHeight instead")]
        public int BoardHeight => _suggestedHeight;
    }
}
