using System.Collections.Generic;
using UnityEngine;
using CircuitCraft.Data;

namespace CircuitCraft.Views
{
    public class GridRenderer : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridSettings _gridSettings;
        [SerializeField] private Camera _camera;

        [Header("Visual Settings")]
        [SerializeField] private Color _gridColor = new Color(0.15f, 0.25f, 0.4f, 0.8f);
        [SerializeField] private float _lineWidth = 0.03f;
        [SerializeField] private Material _lineMaterial;

        [Header("Suggested Area")]
        [SerializeField] private Color _suggestedAreaBorderColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        [SerializeField] private float _suggestedAreaBorderWidth = 0.06f;

        private GameObject _gridContainer;
        private readonly List<LineRenderer> _horizontalLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _verticalLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _suggestedAreaLines = new List<LineRenderer>(4);

        private Vector3 _cachedCamPos;
        private float _cachedOrthoSize;
        private float _cachedAspect;
        private bool _hasCachedCameraState;
        private bool _forceRefresh = true;
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

            if (!_forceRefresh && !HasCameraChanged())
            {
                return;
            }

            EnsureGridContainer();
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
            UpdateSuggestedAreaBorder(origin, cellSize);
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
            EnsureLinePoolSize(_horizontalLines, neededCount, "HLine");

            float startX = origin.x + (gridMinX * cellSize);
            float endX = origin.x + (gridMaxX * cellSize);
            float y = origin.y;

            int lineIndex = 0;
            for (int gridY = gridMinY; gridY <= gridMaxY; gridY++)
            {
                float z = origin.z + (gridY * cellSize);
                LineRenderer line = _horizontalLines[lineIndex++];
                line.enabled = true;
                SetLineVisual(line, _gridColor, _lineWidth);

                line.positionCount = 2;
                line.SetPosition(0, new Vector3(startX, y, z));
                line.SetPosition(1, new Vector3(endX, y, z));
            }

            DisableUnusedLines(_horizontalLines, neededCount);
        }

        private void UpdateVerticalLines(int gridMinX, int gridMaxX, int gridMinY, int gridMaxY, Vector3 origin, float cellSize)
        {
            int neededCount = Mathf.Max(0, gridMaxX - gridMinX + 1);
            EnsureLinePoolSize(_verticalLines, neededCount, "VLine");

            float startZ = origin.z + (gridMinY * cellSize);
            float endZ = origin.z + (gridMaxY * cellSize);
            float y = origin.y;

            int lineIndex = 0;
            for (int gridX = gridMinX; gridX <= gridMaxX; gridX++)
            {
                float x = origin.x + (gridX * cellSize);
                LineRenderer line = _verticalLines[lineIndex++];
                line.enabled = true;
                SetLineVisual(line, _gridColor, _lineWidth);

                line.positionCount = 2;
                line.SetPosition(0, new Vector3(x, y, startZ));
                line.SetPosition(1, new Vector3(x, y, endZ));
            }

            DisableUnusedLines(_verticalLines, neededCount);
        }

        private void UpdateSuggestedAreaBorder(Vector3 origin, float cellSize)
        {
            EnsureLinePoolSize(_suggestedAreaLines, 4, "SuggestedBorder");

            float x0 = origin.x;
            float z0 = origin.z;
            float x1 = origin.x + (_gridSettings.SuggestedWidth * cellSize);
            float z1 = origin.z + (_gridSettings.SuggestedHeight * cellSize);
            float y = origin.y;

            SetBorderLine(_suggestedAreaLines[0], new Vector3(x0, y, z0), new Vector3(x1, y, z0));
            SetBorderLine(_suggestedAreaLines[1], new Vector3(x1, y, z0), new Vector3(x1, y, z1));
            SetBorderLine(_suggestedAreaLines[2], new Vector3(x1, y, z1), new Vector3(x0, y, z1));
            SetBorderLine(_suggestedAreaLines[3], new Vector3(x0, y, z1), new Vector3(x0, y, z0));
        }

        private void SetBorderLine(LineRenderer line, Vector3 start, Vector3 end)
        {
            line.enabled = true;
            SetLineVisual(line, _suggestedAreaBorderColor, _suggestedAreaBorderWidth);
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private void EnsureLinePoolSize(List<LineRenderer> pool, int requiredCount, string linePrefix)
        {
            while (pool.Count < requiredCount)
            {
                int nextIndex = pool.Count;
                GameObject lineObject = new GameObject($"{linePrefix}_{nextIndex}");
                lineObject.transform.SetParent(_gridContainer.transform, false);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(line);
                line.enabled = false;
                line.positionCount = 0;
                pool.Add(line);
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

        private void ConfigureLineRenderer(LineRenderer line)
        {
            SetLineVisual(line, _gridColor, _lineWidth);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
        }

        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Unlit/Color"));
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

            if (_runtimeLineMaterial != null)
            {
                _runtimeLineMaterial.color = _gridColor;
            }

            for (int i = 0; i < _horizontalLines.Count; i++)
            {
                SetLineVisual(_horizontalLines[i], _gridColor, _lineWidth);
            }

            for (int i = 0; i < _verticalLines.Count; i++)
            {
                SetLineVisual(_verticalLines[i], _gridColor, _lineWidth);
            }

            for (int i = 0; i < _suggestedAreaLines.Count; i++)
            {
                SetLineVisual(_suggestedAreaLines[i], _suggestedAreaBorderColor, _suggestedAreaBorderWidth);
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
