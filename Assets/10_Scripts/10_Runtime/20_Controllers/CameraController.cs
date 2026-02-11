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
        [SerializeField] private float _panSpeed = 10f;
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 30f;
        [SerializeField] private float _minY = -10f;
        [SerializeField] private float _maxY = 30f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 20f;
        
        private Camera _camera;
        private Vector3 _lastMousePosition;
        private bool _isPanning;
        
        private void Awake()
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
                panDelta = new Vector3(horizontal, vertical, 0f) * _panSpeed * Time.deltaTime;
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
                
                panDelta += new Vector3(worldDeltaX, worldDeltaY, 0f);
            }
            
            // Apply panning with bounds clamping
            if (panDelta.sqrMagnitude > 0.0001f)
            {
                Vector3 newPosition = transform.position + panDelta;
                newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
                newPosition.y = Mathf.Clamp(newPosition.y, _minY, _maxY);
                transform.position = newPosition;
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
    }
}
