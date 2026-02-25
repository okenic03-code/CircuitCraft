using System.Collections.Generic;
using UnityEngine;
using CircuitCraft.Data;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Renders an infinite scrolling grid aligned to the configured board origin and cell size.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [Tooltip("Grid settings asset that defines origin and cell size.")]
        [SerializeField] private GridSettings _gridSettings;
        [Tooltip("Orthographic camera used to determine visible grid extents.")]
        [SerializeField] private Camera _camera;

        [Header("Visual Settings")]
        [SerializeField] private Color _gridColor = new Color(0.15f, 0.25f, 0.4f, 0.8f);
        [SerializeField] private float _lineWidth = 0.03f;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Shader _defaultShader;

        private GameObject _gridContainer;
        private readonly List<LineRenderer> _horizontalLines = new();
        private readonly List<LineRenderer> _verticalLines = new();

        private Vector3 _cachedCamPos;
        private float _cachedOrthoSize;
        private float _cachedAspect;
        private bool _hasCachedCameraState;
        private bool _forceRefresh = true;
        private bool _visualsDirty;
        private Material _runtimeLineMaterial;

        private void Awake()
        {
            EnsureCameraReference();
            EnsureGridContainer();

            if (_gridSettings == null)
            {
                Debug.LogError("GridRenderer: GridSettings reference is missing!");
            }
        }

        private void LateUpdate()
        {
            if (_gridSettings == null)
            {
                return;
            }

            EnsureCameraReference();
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            bool cameraChanged = HasCameraChanged();
            if (!_forceRefresh && !_visualsDirty && !cameraChanged)
            {
                return;
            }

            EnsureGridContainer();

            if (_forceRefresh || cameraChanged)
            {
                UpdateCachedCameraState();
                _forceRefresh = false;

                float cellSize = Mathf.Max(0.0001f, _gridSettings.CellSize);
                Vector3 origin = _gridSettings.GridOrigin;
                Vector3 camPos = _camera.transform.position;

                float halfHeight = _camera.orthographicSize;
                float halfWidth = halfHeight * _camera.aspect;

                float worldMinX = camPos.x - halfWidth;
                float worldMaxX = camPos.x + halfWidth;
                float worldMinZ = camPos.z - halfHeight;
                float worldMaxZ = camPos.z + halfHeight;

                int gridMinX = Mathf.FloorToInt((worldMinX - origin.x) / cellSize) - 1;
                int gridMaxX = Mathf.CeilToInt((worldMaxX - origin.x) / cellSize) + 1;
                int gridMinY = Mathf.FloorToInt((worldMinZ - origin.z) / cellSize) - 1;
                int gridMaxY = Mathf.CeilToInt((worldMaxZ - origin.z) / cellSize) + 1;

                UpdateHorizontalLines(gridMinY, gridMaxY, gridMinX, gridMaxX, origin, cellSize);
                UpdateVerticalLines(gridMinX, gridMaxX, gridMinY, gridMaxY, origin, cellSize);
            }

            if (_visualsDirty)
            {
                UpdateLineVisuals();
                _visualsDirty = false;
            }
        }

        private bool HasCameraChanged()
        {
            if (!_hasCachedCameraState)
            {
                return true;
            }

            Vector3 currentCamPos = _camera.transform.position;
            return currentCamPos != _cachedCamPos
                || !Mathf.Approximately(_camera.orthographicSize, _cachedOrthoSize)
                || !Mathf.Approximately(_camera.aspect, _cachedAspect);
        }

        private void UpdateCachedCameraState()
        {
            _cachedCamPos = _camera.transform.position;
            _cachedOrthoSize = _camera.orthographicSize;
            _cachedAspect = _camera.aspect;
            _hasCachedCameraState = true;
        }

        private void EnsureCameraReference()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void EnsureGridContainer()
        {
            if (_gridContainer != null)
            {
                return;
            }

            _gridContainer = new GameObject("Grid Lines");
            _gridContainer.transform.SetParent(transform, false);
        }

        private void UpdateHorizontalLines(int gridMinY, int gridMaxY, int gridMinX, int gridMaxX, Vector3 origin, float cellSize)
        {
            int neededCount = Mathf.Max(0, gridMaxY - gridMinY + 1);
            EnsureLinePoolSize(_horizontalLines, neededCount, "HLine", _gridColor, _lineWidth);

            float startX = origin.x + (gridMinX * cellSize);
            float endX = origin.x + (gridMaxX * cellSize);
            float y = origin.y;

            int lineIndex = 0;
            for (int gridY = gridMinY; gridY <= gridMaxY; gridY++)
            {
                float z = origin.z + (gridY * cellSize);
                LineRenderer line = _horizontalLines[lineIndex++];
                line.enabled = true;

                line.positionCount = 2;
                line.SetPosition(0, new Vector3(startX, y, z));
                line.SetPosition(1, new Vector3(endX, y, z));
            }

            DisableUnusedLines(_horizontalLines, neededCount);
        }

        private void UpdateVerticalLines(int gridMinX, int gridMaxX, int gridMinY, int gridMaxY, Vector3 origin, float cellSize)
        {
            int neededCount = Mathf.Max(0, gridMaxX - gridMinX + 1);
            EnsureLinePoolSize(_verticalLines, neededCount, "VLine", _gridColor, _lineWidth);

            float startZ = origin.z + (gridMinY * cellSize);
            float endZ = origin.z + (gridMaxY * cellSize);
            float y = origin.y;

            int lineIndex = 0;
            for (int gridX = gridMinX; gridX <= gridMaxX; gridX++)
            {
                float x = origin.x + (gridX * cellSize);
                LineRenderer line = _verticalLines[lineIndex++];
                line.enabled = true;

                line.positionCount = 2;
                line.SetPosition(0, new Vector3(x, y, startZ));
                line.SetPosition(1, new Vector3(x, y, endZ));
            }

            DisableUnusedLines(_verticalLines, neededCount);
        }

        private void EnsureLinePoolSize(List<LineRenderer> pool, int requiredCount, string linePrefix, Color lineColor, float lineWidth)
        {
            while (pool.Count < requiredCount)
            {
                int nextIndex = pool.Count;
                GameObject lineObject = new GameObject($"{linePrefix}_{nextIndex}");
                lineObject.transform.SetParent(_gridContainer.transform, false);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(line, lineColor, lineWidth);
                line.enabled = false;
                line.positionCount = 0;
                pool.Add(line);
            }
        }

        private void UpdateLineVisuals()
        {
            for (int i = 0; i < _horizontalLines.Count; i++)
            {
                SetLineVisual(_horizontalLines[i], _gridColor, _lineWidth);
            }

            for (int i = 0; i < _verticalLines.Count; i++)
            {
                SetLineVisual(_verticalLines[i], _gridColor, _lineWidth);
            }

        }

        private void DisableUnusedLines(List<LineRenderer> pool, int usedCount)
        {
            for (int i = usedCount; i < pool.Count; i++)
            {
                pool[i].positionCount = 0;
                pool[i].enabled = false;
            }
        }

        private void SetLineVisual(LineRenderer line, Color color, float width)
        {
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;

            Material targetMaterial = _lineMaterial != null ? _lineMaterial : GetOrCreateRuntimeMaterial();
            if (line.sharedMaterial != targetMaterial)
            {
                line.sharedMaterial = targetMaterial;
            }
        }

        private Material GetOrCreateRuntimeMaterial()
        {
            if (_runtimeLineMaterial != null)
            {
                return _runtimeLineMaterial;
            }

            _runtimeLineMaterial = CreateDefaultMaterial();
            return _runtimeLineMaterial;
        }

        private void ConfigureLineRenderer(LineRenderer line) => ConfigureLineRenderer(line, _gridColor, _lineWidth);

        private void ConfigureLineRenderer(LineRenderer line, Color lineColor, float width)
        {
            SetLineVisual(line, lineColor, width);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
        }

        private Material CreateDefaultMaterial()
        {
            var shader = _defaultShader != null ? _defaultShader : (Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            Material mat = new Material(shader);
            mat.color = _gridColor;
            return mat;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _forceRefresh = true;
            _visualsDirty = true;

            if (_runtimeLineMaterial != null)
            {
                _runtimeLineMaterial.color = _gridColor;
            }
        }

        private void OnDestroy()
        {
            if (_gridContainer != null)
            {
                Destroy(_gridContainer);
            }

            if (_runtimeLineMaterial != null)
            {
                Destroy(_runtimeLineMaterial);
            }
        }
    }
}
