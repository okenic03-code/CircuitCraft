using UnityEngine;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Renders a visual grid for component placement using LineRenderer components.
    /// Creates a grid of lines at runtime to visualize the placement area.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int _gridWidth = 20;
        [SerializeField] private int _gridHeight = 20;
        [SerializeField] private float _cellSize = 1.0f;
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private Material _lineMaterial;

        private GameObject _gridContainer;

        private void Start()
        {
            GenerateGrid();
        }

        /// <summary>
        /// Generates the grid visualization using LineRenderer components.
        /// Creates horizontal and vertical lines as child GameObjects.
        /// </summary>
        private void GenerateGrid()
        {
            // Create parent container for all grid lines
            _gridContainer = new GameObject("Grid Lines");
            _gridContainer.transform.SetParent(transform);
            _gridContainer.transform.localPosition = Vector3.zero;

            // Generate horizontal lines (width + 1 lines)
            for (int y = 0; y <= _gridHeight; y++)
            {
                CreateHorizontalLine(y);
            }

            // Generate vertical lines (height + 1 lines)
            for (int x = 0; x <= _gridWidth; x++)
            {
                CreateVerticalLine(x);
            }

            Debug.Log($"GridRenderer: Generated {_gridWidth}x{_gridHeight} grid at {_gridOrigin}");
        }

        /// <summary>
        /// Creates a horizontal grid line at the specified Y coordinate.
        /// </summary>
        /// <param name="y">Grid Y coordinate (0 to _gridHeight)</param>
        private void CreateHorizontalLine(int y)
        {
            GameObject lineObj = new GameObject($"HLine_{y}");
            lineObj.transform.SetParent(_gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line);

            // Set line start and end positions
            Vector3 startPos = _gridOrigin + new Vector3(0, 0, y * _cellSize);
            Vector3 endPos = _gridOrigin + new Vector3(_gridWidth * _cellSize, 0, y * _cellSize);

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
            GameObject lineObj = new GameObject($"VLine_{x}");
            lineObj.transform.SetParent(_gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line);

            // Set line start and end positions
            Vector3 startPos = _gridOrigin + new Vector3(x * _cellSize, 0, 0);
            Vector3 endPos = _gridOrigin + new Vector3(x * _cellSize, 0, _gridHeight * _cellSize);

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
