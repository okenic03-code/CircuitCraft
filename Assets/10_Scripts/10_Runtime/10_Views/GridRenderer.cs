using UnityEngine;
using CircuitCraft.Data;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Renders a visual grid for component placement using LineRenderer components.
    /// Creates a grid of lines at runtime to visualize the placement area.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridSettings _gridSettings;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _gridColor = new Color(0.12f, 0.12f, 0.22f, 0.7f);
        [SerializeField] private float _lineWidth = 0.02f;
        [SerializeField] private Material _lineMaterial;

        private GameObject _gridContainer;

        private void Start()
        {
            if (_gridSettings == null)
            {
                Debug.LogError("GridRenderer: GridSettings reference is missing!");
                return;
            }
            
            GenerateGrid();
        }

        /// <summary>
        /// Generates the grid visualization using LineRenderer components.
        /// Creates horizontal and vertical lines as child GameObjects.
        /// </summary>
        private void GenerateGrid()
        {
            if (_gridSettings == null)
                return;
            
            // Create parent container for all grid lines
            _gridContainer = new GameObject("Grid Lines");
            _gridContainer.transform.SetParent(transform);
            _gridContainer.transform.localPosition = Vector3.zero;

            // Generate horizontal lines (width + 1 lines)
            for (int y = 0; y <= _gridSettings.BoardHeight; y++)
            {
                CreateHorizontalLine(y);
            }

            // Generate vertical lines (height + 1 lines)
            for (int x = 0; x <= _gridSettings.BoardWidth; x++)
            {
                CreateVerticalLine(x);
            }

            Debug.Log($"GridRenderer: Generated {_gridSettings.BoardWidth}x{_gridSettings.BoardHeight} grid at {_gridSettings.GridOrigin}");
        }

        /// <summary>
        /// Creates a horizontal grid line at the specified Y coordinate.
        /// </summary>
        /// <param name="y">Grid Y coordinate (0 to _gridHeight)</param>
        private void CreateHorizontalLine(int y)
        {
            if (_gridSettings == null)
                return;
            
            GameObject lineObj = new GameObject($"HLine_{y}");
            lineObj.transform.SetParent(_gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line);

            // Set line start and end positions
            Vector3 startPos = _gridSettings.GridOrigin + new Vector3(0, 0, y * _gridSettings.CellSize);
            Vector3 endPos = _gridSettings.GridOrigin + new Vector3(_gridSettings.BoardWidth * _gridSettings.CellSize, 0, y * _gridSettings.CellSize);

            line.positionCount = 2;
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
        }

        /// <summary>
        /// Creates a vertical grid line at the specified X coordinate.
        /// </summary>
        /// <param name="x">Grid X coordinate (0 to _gridWidth)</param>
        private void CreateVerticalLine(int x)
        {
            if (_gridSettings == null)
                return;
            
            GameObject lineObj = new GameObject($"VLine_{x}");
            lineObj.transform.SetParent(_gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line);

            // Set line start and end positions
            Vector3 startPos = _gridSettings.GridOrigin + new Vector3(x * _gridSettings.CellSize, 0, 0);
            Vector3 endPos = _gridSettings.GridOrigin + new Vector3(x * _gridSettings.CellSize, 0, _gridSettings.BoardHeight * _gridSettings.CellSize);

            line.positionCount = 2;
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
        }

        /// <summary>
        /// Configures a LineRenderer with the grid's visual settings.
        /// </summary>
        /// <param name="line">LineRenderer component to configure</param>
        private void ConfigureLineRenderer(LineRenderer line)
        {
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.startColor = _gridColor;
            line.endColor = _gridColor;
            line.material = _lineMaterial != null ? _lineMaterial : CreateDefaultMaterial();
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
        }

        /// <summary>
        /// Creates a default unlit material if no material is assigned.
        /// </summary>
        /// <returns>New material instance with grid color</returns>
        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = _gridColor;
            return mat;
        }

        /// <summary>
        /// Regenerates the grid when settings change in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            // Regenerate grid in play mode when values change
            if (Application.isPlaying && _gridContainer != null)
            {
                Destroy(_gridContainer);
                GenerateGrid();
            }
        }

        private void OnDestroy()
        {
            // Clean up grid container when destroyed
            if (_gridContainer != null)
            {
                Destroy(_gridContainer);
            }
        }
    }
}
