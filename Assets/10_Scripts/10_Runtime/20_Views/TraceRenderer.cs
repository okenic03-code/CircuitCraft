using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Utils;
using UnityEngine;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Renders board trace segments using LineRenderer components.
    /// </summary>
    public class TraceRenderer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridSettings _gridSettings;

        [Header("Visual Settings")]
        [SerializeField] private Color _wireColor = new Color(0.1f, 0.85f, 1f, 1f);
        [SerializeField] private float _wireWidth = 0.08f;
        [SerializeField] private float _wireY = 0.05f;

        private BoardState _boardState;
        private Material _lineMaterial;
        private readonly Dictionary<int, LineRenderer> _traceLines = new Dictionary<int, LineRenderer>();

        private void Start()
        {
            if (_gameManager == null)
            {
                Debug.LogError("TraceRenderer: GameManager reference is missing!");
                return;
            }

            if (_gridSettings == null)
            {
                Debug.LogError("TraceRenderer: GridSettings reference is missing!");
                return;
            }

            _boardState = _gameManager.BoardState;
            if (_boardState == null)
            {
                Debug.LogWarning("TraceRenderer: BoardState is not available.");
                return;
            }

            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            Subscribe();

            foreach (var trace in _boardState.Traces)
            {
                CreateTraceLine(trace);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();

            foreach (var pair in _traceLines)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }
            _traceLines.Clear();

            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }

        private void Subscribe()
        {
            _boardState.OnTraceAdded += HandleTraceAdded;
            _boardState.OnTraceRemoved += HandleTraceRemoved;
        }

        private void Unsubscribe()
        {
            if (_boardState == null)
                return;

            _boardState.OnTraceAdded -= HandleTraceAdded;
            _boardState.OnTraceRemoved -= HandleTraceRemoved;
        }

        private void HandleTraceAdded(TraceSegment trace)
        {
            CreateTraceLine(trace);
        }

        private void HandleTraceRemoved(int segmentId)
        {
            if (_traceLines.TryGetValue(segmentId, out var line))
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }

                _traceLines.Remove(segmentId);
            }
        }

        private void CreateTraceLine(TraceSegment trace)
        {
            if (trace == null || _traceLines.ContainsKey(trace.SegmentId))
                return;

            var lineObject = new GameObject($"Trace_{trace.SegmentId}");
            lineObject.transform.SetParent(transform, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = _wireWidth;
            line.endWidth = _wireWidth;
            line.startColor = _wireColor;
            line.endColor = _wireColor;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = _lineMaterial;

            line.SetPosition(0, GridToWireWorld(trace.Start));
            line.SetPosition(1, GridToWireWorld(trace.End));

            _traceLines[trace.SegmentId] = line;
        }

        private Vector3 GridToWireWorld(GridPosition pos)
        {
            Vector3 world = GridUtility.GridToWorldPosition(
                new Vector2Int(pos.X, pos.Y),
                _gridSettings.CellSize,
                _gridSettings.GridOrigin
            );
            world.y += _wireY;
            return world;
        }
    }
}
