using CircuitCraft.Core;
using CircuitCraft.Utils;
using UnityEngine;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Manages LineRenderer preview for wire routing visualization.
    /// </summary>
    public class WirePreviewManager : MonoBehaviour
    {
        [Header("Preview Settings")]
        [SerializeField] private Color _previewColor = Color.yellow;
        [SerializeField] private float _previewWidth = 0.08f;
        [SerializeField] private float _previewY = 0.06f;
        [SerializeField] private Shader _previewShader;

        private LineRenderer _previewLine;

        /// <summary>
        /// Creates the LineRenderer preview object. Call once during initialization.
        /// </summary>
        public void Initialize()
        {
            if (_previewLine != null)
                return;

            var previewObject = new GameObject("WirePreview");
            previewObject.transform.SetParent(transform, false);

            _previewLine = previewObject.AddComponent<LineRenderer>();
            _previewLine.useWorldSpace = true;
            _previewLine.positionCount = 0;
            _previewLine.startWidth = _previewWidth;
            _previewLine.endWidth = _previewWidth;
            _previewLine.startColor = _previewColor;
            _previewLine.endColor = _previewColor;
            _previewLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _previewLine.receiveShadows = false;
            var shader = _previewShader != null ? _previewShader : Shader.Find("Sprites/Default");
            _previewLine.material = new Material(shader);
        }

        /// <summary>
        /// Shows the preview LineRenderer.
        /// </summary>
        public void Show()
        {
            if (_previewLine != null)
                _previewLine.enabled = true;
        }

        /// <summary>
        /// Hides the preview LineRenderer and resets positions.
        /// </summary>
        public void Hide()
        {
            if (_previewLine != null)
            {
                _previewLine.positionCount = 0;
                _previewLine.enabled = false;
            }
        }

        /// <summary>
        /// Updates the preview path from start pin to current mouse grid position.
        /// Uses Manhattan routing (horizontal-first corner).
        /// </summary>
        /// <param name="startPinPos">Start pin grid position.</param>
        /// <param name="currentMouseGridPos">Current mouse grid position.</param>
        /// <param name="cellSize">Grid cell size in world units.</param>
        /// <param name="gridOrigin">Grid origin in world space.</param>
        public void UpdatePath(GridPosition startPinPos, GridPosition currentMouseGridPos, float cellSize, Vector3 gridOrigin)
        {
            if (_previewLine == null)
                return;

            if (startPinPos.X == currentMouseGridPos.X || startPinPos.Y == currentMouseGridPos.Y)
            {
                _previewLine.positionCount = 2;
                SetPosition(0, startPinPos, cellSize, gridOrigin);
                SetPosition(1, currentMouseGridPos, cellSize, gridOrigin);
            }
            else
            {
                var corner = new GridPosition(currentMouseGridPos.X, startPinPos.Y);
                _previewLine.positionCount = 3;
                SetPosition(0, startPinPos, cellSize, gridOrigin);
                SetPosition(1, corner, cellSize, gridOrigin);
                SetPosition(2, currentMouseGridPos, cellSize, gridOrigin);
            }
        }

        private void SetPosition(int index, GridPosition gridPos, float cellSize, Vector3 gridOrigin)
        {
            Vector3 worldPos = GridUtility.GridToWorldPosition(new(gridPos.X, gridPos.Y), cellSize, gridOrigin);
            worldPos.y += _previewY;
            _previewLine.SetPosition(index, worldPos);
        }

        private void OnDestroy()
        {
            if (_previewLine != null && _previewLine.material != null)
                Destroy(_previewLine.material);
        }
    }
}
