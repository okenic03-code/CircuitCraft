using UnityEngine;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Controls camera pan and zoom for grid navigation.
    /// Supports keyboard (arrow keys) and mouse (middle button) panning,
    /// plus mouse wheel zooming with configurable bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [Tooltip("Camera pan speed in world units per second.")]
        [SerializeField] private float _panSpeed = 10f;
        
        [Header("Zoom Settings")]
        [Tooltip("Mouse-wheel zoom speed multiplier.")]
        [SerializeField] private float _zoomSpeed = 2f;

        [Tooltip("Minimum orthographic camera size.")]
        [SerializeField] private float _minZoom = 5f;

        [Tooltip("Maximum orthographic camera size.")]
        [SerializeField] private float _maxZoom = 20f;
        
        private Camera _camera;
        private Vector3 _lastMousePosition;
        private bool _isPanning;
        
        private void Awake() => Init();

        private void Init()
        {
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            _camera = GetComponent<Camera>();
        }
        
        private void Update()
        {
            HandlePan();
            HandleZoom();
        }
        
        /// <summary>
        /// Handles camera panning via arrow keys or middle mouse button drag.
        /// </summary>
        private void HandlePan()
        {
            Vector3 panDelta = Vector3.zero;
            
            // Arrow key panning
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                panDelta = new(horizontal, 0f, vertical);
                panDelta *= _panSpeed * Time.deltaTime;
            }
            
            // Middle mouse button panning
            if (Input.GetMouseButtonDown(2))
            {
                _isPanning = true;
                _lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isPanning = false;
            }
            
            if (_isPanning && Input.GetMouseButton(2))
            {
                Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = Input.mousePosition;
                
                // Convert screen space delta to world space
                // Invert Y and negate to match natural dragging behavior
                float worldDeltaX = -mouseDelta.x * (_camera.orthographicSize * 2f / Screen.height);
                float worldDeltaY = -mouseDelta.y * (_camera.orthographicSize * 2f / Screen.height);
                
                panDelta += new(worldDeltaX, 0f, worldDeltaY);
            }
            
            // Apply panning
            if (panDelta.sqrMagnitude > 0.0001f)
            {
                transform.position += panDelta;
            }
        }
        
        /// <summary>
        /// Handles camera zoom via mouse wheel scroll.
        /// Adjusts orthographic camera size within configured bounds.
        /// </summary>
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float newSize = _camera.orthographicSize - scroll * _zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
            }
        }
        
        /// <summary>
        /// Moves the camera to frame the given world-space bounding rectangle.
        /// Adjusts position to center on the rect and zoom to fit it in view.
        /// </summary>
        /// <param name="worldMin">Minimum corner of the rect in world space.</param>
        /// <param name="worldMax">Maximum corner of the rect in world space.</param>
        /// <param name="padding">Extra padding in world units around the rect.</param>
        public void FrameBounds(Vector3 worldMin, Vector3 worldMax, float padding = 2f)
        {
            // Center camera on the rect
            Vector3 center = (worldMin + worldMax) * 0.5f;
            Vector3 pos = transform.position;
            pos.x = center.x;
            pos.z = center.z;
            transform.position = pos;
            
            // Adjust zoom to fit the rect
            float rectWidth = (worldMax.x - worldMin.x) + padding * 2f;
            float rectHeight = (worldMax.z - worldMin.z) + padding * 2f;
            
            // Choose zoom based on aspect ratio
            float aspect = _camera.aspect;
            float requiredSize = Mathf.Max(rectHeight * 0.5f, rectWidth * 0.5f / aspect);
            _camera.orthographicSize = Mathf.Clamp(requiredSize, _minZoom, _maxZoom);
        }
        
        /// <summary>
        /// Resets the camera to frame the default suggested area.
        /// </summary>
        /// <param name="suggestedWidth">Width in grid cells.</param>
        /// <param name="suggestedHeight">Height in grid cells.</param>
        /// <param name="cellSize">Size of each cell in world units.</param>
        /// <param name="gridOrigin">World-space origin of the grid.</param>
        public void FrameSuggestedArea(int suggestedWidth, int suggestedHeight, float cellSize, Vector3 gridOrigin)
        {
            Vector3 worldMin = gridOrigin;
            Vector3 worldMax = gridOrigin + new(suggestedWidth * cellSize, 0f, suggestedHeight * cellSize);
            FrameBounds(worldMin, worldMax);
        }
    }
}
