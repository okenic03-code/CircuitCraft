using UnityEngine;

namespace CircuitCraft.Utils
{
    /// <summary>
    /// Static utility for grid coordinate conversions and validation.
    /// Provides methods to convert between screen space, world space, and grid coordinates.
    /// </summary>
    public static class GridUtility
    {
        /// <summary>
        /// Converts a screen position to a grid position using raycasting.
        /// </summary>
        /// <param name="screenPosition">Screen position from Input.mousePosition</param>
        /// <param name="camera">Camera to raycast from</param>
        /// <param name="cellSize">Size of each grid cell in world units</param>
        /// <param name="gridOrigin">World position of the grid's origin (0,0)</param>
        /// <returns>Grid coordinates (x, z) or Vector2Int.zero if raycast fails</returns>
        public static Vector2Int ScreenToGridPosition(Vector3 screenPosition, Camera camera, float cellSize, Vector3 gridOrigin)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane gridPlane = new Plane(Vector3.up, gridOrigin);
            
            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                
                // Convert world position to grid coordinates
                int x = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / cellSize);
                int z = Mathf.RoundToInt((worldPos.z - gridOrigin.z) / cellSize);
                
                return new Vector2Int(x, z);
            }
            
            return Vector2Int.zero;
        }
        
        /// <summary>
        /// Converts a grid position to world space position.
        /// </summary>
        /// <param name="gridPosition">Grid coordinates (x, z)</param>
        /// <param name="cellSize">Size of each grid cell in world units</param>
        /// <param name="gridOrigin">World position of the grid's origin (0,0)</param>
        /// <returns>World position at the center of the grid cell</returns>
        public static Vector3 GridToWorldPosition(Vector2Int gridPosition, float cellSize, Vector3 gridOrigin)
        {
            float worldX = gridOrigin.x + (gridPosition.x * cellSize);
            float worldZ = gridOrigin.z + (gridPosition.y * cellSize);
            
            return new Vector3(worldX, gridOrigin.y, worldZ);
        }
        
        /// <summary>
        /// Checks if a grid position is within valid bounds.
        /// </summary>
        /// <param name="gridPosition">Grid coordinates to validate</param>
        /// <param name="gridWidth">Width of the grid (number of cells)</param>
        /// <param name="gridHeight">Height of the grid (number of cells)</param>
        /// <returns>True if position is within grid bounds</returns>
        public static bool IsValidGridPosition(Vector2Int gridPosition, int gridWidth, int gridHeight)
        {
            return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
                   gridPosition.y >= 0 && gridPosition.y < gridHeight;
        }
    }
}
