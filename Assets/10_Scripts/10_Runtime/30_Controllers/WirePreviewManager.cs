using System.Collections.Generic;
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
            => UpdatePath(null, startPinPos, currentMouseGridPos, cellSize, gridOrigin);

        /// <summary>
        /// Updates the preview path with locked segments plus a live segment from current anchor to mouse.
        /// </summary>
        /// <param name="lockedSegments">Segments already fixed by intermediate grid clicks.</param>
        /// <param name="currentAnchorPos">Current route anchor position.</param>
        /// <param name="currentMouseGridPos">Current mouse grid position.</param>
        /// <param name="cellSize">Grid cell size in world units.</param>
        /// <param name="gridOrigin">Grid origin in world space.</param>
        public void UpdatePath(
            IReadOnlyList<(GridPosition start, GridPosition end)> lockedSegments,
            GridPosition currentAnchorPos,
            GridPosition currentMouseGridPos,
            float cellSize,
            Vector3 gridOrigin)
        {
            if (_previewLine == null)
                return;

            var points = new List<GridPosition>(8);
            AppendSegmentsAsPoints(lockedSegments, points);

            var liveSegments = WirePathCalculator.BuildManhattanSegments(currentAnchorPos, currentMouseGridPos);
            AppendSegmentsAsPoints(liveSegments, points);

            if (points.Count < 2)
            {
                _previewLine.positionCount = 0;
                return;
            }

            _previewLine.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                SetPosition(i, points[i], cellSize, gridOrigin);
            }
        }

        private static void AppendSegmentsAsPoints(
            IReadOnlyList<(GridPosition start, GridPosition end)> segments,
            List<GridPosition> points)
        {
            if (segments == null || points == null)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.start == segment.end)
                {
                    continue;
                }

                if (points.Count == 0 || points[points.Count - 1] != segment.start)
                {
                    points.Add(segment.start);
                }

                if (points[points.Count - 1] != segment.end)
                {
                    points.Add(segment.end);
                }
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
