using System.Collections.Generic;
using System.Reflection;
using CircuitCraft.Components;
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
        [Tooltip("GameManager that owns the active BoardState.")]
        [SerializeField] private GameManager _gameManager;
        [Tooltip("StageManager used to rebuild traces when a stage loads.")]
        [SerializeField] private StageManager _stageManager;
        [Tooltip("Grid settings asset used for grid-to-world conversion.")]
        [SerializeField] private GridSettings _gridSettings;

        [Header("Visual Settings")]
        [SerializeField] private Color _wireColor = new Color(0.1f, 0.85f, 1f, 1f);
        [SerializeField] private float _wireWidth = 0.08f;
        [SerializeField] private float _wireY = 0.05f;
        [SerializeField] private Shader _lineShader;

        [Header("Voltage Colors")]
        [SerializeField] private Color _voltageMinColor = Color.blue;
        [SerializeField] private Color _voltageMaxColor = Color.yellow;

        [Header("Current Flow")]
        [SerializeField] private float _flowAnimationBaseSpeed = 0.2f;
        [SerializeField] private float _flowAnimationSpeedScale = 2f;
        [SerializeField] private float _flowAnimationMaxSpeed = 2.2f;
        [SerializeField] private float _flowMinVisibleCurrent = 1e-6f;
        [SerializeField] private float _flowWidthMultiplier = 0.8f;
        [SerializeField] private Color _flowColor = new Color(1f, 1f, 1f, 0.85f);

        private BoardState _boardState;
        private System.Action<string> _onBoardLoadedHandler;
        private Material _lineMaterial;
        private Material _flowLineMaterial;
        private Texture2D _flowTexture;
        private readonly Dictionary<int, LineRenderer> _traceLines = new();
        private readonly Dictionary<int, LineRenderer> _flowLines = new();
        private readonly Dictionary<int, float> _segmentCurrents = new();
        private readonly Dictionary<int, float> _segmentFlowOffsets = new();
        private readonly Dictionary<GridPosition, GameObject> _junctionDots = new();
        private Sprite _junctionDotSprite;

        private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private const int FlowTextureWidth = 64;
        private const int FlowTextureHeight = 8;

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
            if (_boardState is null)
            {
                Debug.LogWarning("TraceRenderer: BoardState is not available.");
                return;
            }

            var shader = _lineShader != null ? _lineShader : Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader);
            _flowLineMaterial = new Material(shader);
            _flowTexture = CreateFlowTexture();
            Subscribe();

            foreach (var trace in _boardState.Traces)
            {
                CreateTraceLine(trace);
            }

            RecalculateJunctions();

            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleBoardReset;

            _onBoardLoadedHandler = _ => HandleBoardReset();
            _gameManager.OnBoardLoaded += _onBoardLoadedHandler;
        }

        private void OnDestroy()
        {
            ClearAllJunctionDots();
            StopCurrentFlow();
            Unsubscribe();

            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;

            if (_gameManager != null)
                _gameManager.OnBoardLoaded -= _onBoardLoadedHandler;

            foreach (var pair in _traceLines)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }
            _traceLines.Clear();

            foreach (var pair in _flowLines)
            {
                if (pair.Value != null)
                {
                    if (pair.Value.sharedMaterial != null && pair.Value.sharedMaterial != _flowLineMaterial)
                    {
                        Destroy(pair.Value.sharedMaterial);
                    }
                    Destroy(pair.Value.gameObject);
                }
            }
            _flowLines.Clear();

            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }

            if (_flowLineMaterial != null)
            {
                Destroy(_flowLineMaterial);
            }

            if (_flowTexture != null)
            {
                Destroy(_flowTexture);
            }
        }

        private void Update()
        {
            if (_segmentCurrents.Count == 0)
            {
                return;
            }

            foreach (var pair in _segmentCurrents)
            {
                int segmentId = pair.Key;
                float current = pair.Value;
                if (!_flowLines.TryGetValue(segmentId, out var flowLine) || flowLine == null)
                {
                    continue;
                }

                float offset = 0f;
                if (_segmentFlowOffsets.TryGetValue(segmentId, out var currentOffset))
                {
                    offset = currentOffset;
                }

                offset = TraceGeometryBuilder.CalculateFlowOffset(
                    offset,
                    current,
                    _flowAnimationBaseSpeed,
                    _flowAnimationSpeedScale,
                    _flowAnimationMaxSpeed,
                    Time.deltaTime);
                _segmentFlowOffsets[segmentId] = offset;

                var flowMaterial = flowLine.sharedMaterial;
                if (flowMaterial != null)
                {
                    flowMaterial.SetTextureOffset(MainTexProperty, new Vector2(offset, 0f));
                }
            }
        }

        private void Subscribe()
        {
            _boardState.OnTraceAdded += HandleTraceAdded;
            _boardState.OnTraceRemoved += HandleTraceRemoved;
        }

        private void Unsubscribe()
        {
            if (_boardState is null)
                return;

            _boardState.OnTraceAdded -= HandleTraceAdded;
            _boardState.OnTraceRemoved -= HandleTraceRemoved;
        }

        private void HandleBoardReset()
        {
            Unsubscribe();

            StopCurrentFlow();

            foreach (var pair in _traceLines)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }
            _traceLines.Clear();

            foreach (var pair in _flowLines)
            {
                if (pair.Value != null)
                {
                    if (pair.Value.sharedMaterial != null && pair.Value.sharedMaterial != _flowLineMaterial)
                    {
                        Destroy(pair.Value.sharedMaterial);
                    }
                    Destroy(pair.Value.gameObject);
                }
            }
            _flowLines.Clear();

            _segmentCurrents.Clear();
            _segmentFlowOffsets.Clear();
            ClearAllJunctionDots();

            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                if (_boardState is not null)
                {
                    Subscribe();
                    foreach (var trace in _boardState.Traces)
                        CreateTraceLine(trace);

                    RecalculateJunctions();
                }
            }
        }

        /// <summary>
        /// Applies voltage-based colors to each trace using per-LineRenderer color values.
        /// </summary>
        /// <param name="nodeVoltages">Node name -> voltage lookup map.</param>
        /// <param name="minVoltage">Lowest voltage in the map used for normalization.</param>
        /// <param name="maxVoltage">Highest voltage in the map used for normalization.</param>
        public void ApplyVoltageColors(Dictionary<string, double> nodeVoltages, float minVoltage, float maxVoltage)
        {
            if (_boardState is null)
            {
                return;
            }

            if (nodeVoltages is null)
            {
                ResetColors();
                return;
            }

            var traceColors = TraceGeometryBuilder.ComputeVoltageColors(
                _boardState.Traces,
                _boardState.GetNet,
                nodeVoltages,
                minVoltage,
                maxVoltage,
                _voltageMinColor,
                _voltageMaxColor,
                _wireColor);

            foreach (var pair in _traceLines)
            {
                if (pair.Value == null)
                    continue;

                var color = traceColors.TryGetValue(pair.Key, out var mappedColor)
                    ? mappedColor
                    : _wireColor;

                pair.Value.startColor = color;
                pair.Value.endColor = color;
            }

            foreach (var pair in _junctionDots)
            {
                if (pair.Value == null)
                    continue;

                var dotRenderer = pair.Value.GetComponent<SpriteRenderer>();
                if (dotRenderer == null)
                    continue;

                Color dotColor = _wireColor;
                for (int i = 0; i < _boardState.Traces.Count; i++)
                {
                    var trace = _boardState.Traces[i];
                    if (trace.Start != pair.Key && trace.End != pair.Key)
                        continue;

                    if (traceColors.TryGetValue(trace.SegmentId, out var mappedColor))
                        dotColor = mappedColor;
                    break;
                }

                dotRenderer.color = dotColor;
            }
        }

        /// <summary>
        /// Applies animated current-flow visualization based on segment currents.
        /// </summary>
        /// <param name="segmentCurrents">Mapping from segment id to current (amps).</param>
        public void ApplyCurrentFlow(Dictionary<int, float> segmentCurrents)
        {
            StopCurrentFlow();

            if (segmentCurrents is null || segmentCurrents.Count == 0)
            {
                return;
            }

            foreach (var pair in _traceLines)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (!segmentCurrents.TryGetValue(pair.Key, out var current))
                {
                    continue;
                }

                if (Mathf.Abs(current) < _flowMinVisibleCurrent)
                {
                    continue;
                }

                CreateFlowLine(pair.Key, pair.Value);
                _segmentCurrents[pair.Key] = current;
            }

            UpdateFlowLineVisibility();
        }

        /// <summary>
        /// Stops current-flow animation and hides all flow overlays.
        /// </summary>
        public void StopCurrentFlow()
        {
            foreach (var pair in _flowLines)
            {
                if (pair.Value != null)
                {
                    pair.Value.enabled = false;
                }
            }

            _segmentCurrents.Clear();
            _segmentFlowOffsets.Clear();
        }

        /// <summary>
        /// Restores all trace colors to their default wire color.
        /// </summary>
        public void ResetColors()
        {
            foreach (var line in _traceLines.Values)
            {
                if (line == null)
                    continue;

                line.startColor = _wireColor;
                line.endColor = _wireColor;
            }

            foreach (var pair in _junctionDots)
            {
                if (pair.Value == null)
                    continue;

                var dotRenderer = pair.Value.GetComponent<SpriteRenderer>();
                if (dotRenderer == null)
                    continue;

                dotRenderer.color = _wireColor;
            }
        }

        private void HandleTraceAdded(TraceSegment trace)
        {
            CreateTraceLine(trace);
            RecalculateJunctions();
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

            HideFlowLine(segmentId);
            RecalculateJunctions();
        }

        private void RecalculateJunctions()
        {
            if (_boardState is null)
            {
                ClearAllJunctionDots();
                return;
            }

            var endpointDegreeByNetAndPosition = new Dictionary<(int netId, GridPosition pos), int>();
            foreach (var trace in _boardState.Traces)
            {
                var startKey = (trace.NetId, trace.Start);
                if (!endpointDegreeByNetAndPosition.TryAdd(startKey, 1))
                    endpointDegreeByNetAndPosition[startKey]++;

                var endKey = (trace.NetId, trace.End);
                if (!endpointDegreeByNetAndPosition.TryAdd(endKey, 1))
                    endpointDegreeByNetAndPosition[endKey]++;
            }

            var junctionPositions = new HashSet<GridPosition>();
            foreach (var pair in endpointDegreeByNetAndPosition)
            {
                int netId = pair.Key.netId;
                GridPosition position = pair.Key.pos;
                int totalDegree = pair.Value;

                var netTraces = _boardState.GetTraces(netId);
                for (int i = 0; i < netTraces.Count; i++)
                {
                    var trace = netTraces[i];
                    if (trace.Start == position || trace.End == position)
                        continue;

                    if (IsPointOnTraceInterior(position, trace))
                        totalDegree += 2;
                }

                if (totalDegree >= 3)
                    junctionPositions.Add(position);
            }

            var existingJunctionPositions = new List<GridPosition>(_junctionDots.Keys);
            for (int i = 0; i < existingJunctionPositions.Count; i++)
            {
                if (!junctionPositions.Contains(existingJunctionPositions[i]))
                    RemoveJunctionDot(existingJunctionPositions[i]);
            }

            foreach (var position in junctionPositions)
            {
                if (!_junctionDots.ContainsKey(position))
                    CreateJunctionDot(position);
            }
        }

        private void ClearAllJunctionDots()
        {
            foreach (var pair in _junctionDots)
            {
                if (pair.Value != null)
                    Destroy(pair.Value);
            }

            _junctionDots.Clear();
        }

        private void CreateJunctionDot(GridPosition pos)
        {
            if (_junctionDots.ContainsKey(pos))
                return;

            var junctionDot = new GameObject($"Junction_{pos.X}_{pos.Y}");
            junctionDot.transform.SetParent(transform, false);

            Vector3 worldPosition = GridToWireWorld(pos);
            worldPosition.y += 0.001f;
            junctionDot.transform.position = worldPosition;
            junctionDot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            junctionDot.transform.localScale = Vector3.one * (_wireWidth * 2.5f);

            var dotRenderer = junctionDot.AddComponent<SpriteRenderer>();
            dotRenderer.sprite = GetJunctionDotSprite();
            dotRenderer.color = _wireColor;

            _junctionDots[pos] = junctionDot;
        }

        private void RemoveJunctionDot(GridPosition pos)
        {
            if (!_junctionDots.TryGetValue(pos, out var junctionDot))
                return;

            if (junctionDot != null)
                Destroy(junctionDot);

            _junctionDots.Remove(pos);
        }

        private static bool IsPointOnTraceInterior(GridPosition point, TraceSegment trace)
        {
            if (trace.Start.Y == trace.End.Y)
            {
                if (point.Y != trace.Start.Y)
                    return false;

                int minX = Mathf.Min(trace.Start.X, trace.End.X);
                int maxX = Mathf.Max(trace.Start.X, trace.End.X);
                return point.X > minX && point.X < maxX;
            }

            if (trace.Start.X == trace.End.X)
            {
                if (point.X != trace.Start.X)
                    return false;

                int minY = Mathf.Min(trace.Start.Y, trace.End.Y);
                int maxY = Mathf.Max(trace.Start.Y, trace.End.Y);
                return point.Y > minY && point.Y < maxY;
            }

            return false;
        }

        private Sprite GetJunctionDotSprite()
        {
            if (_junctionDotSprite != null)
                return _junctionDotSprite;

            var symbolGeneratorType = typeof(ComponentView).Assembly.GetType("CircuitCraft.Components.ComponentSymbolGenerator");
            var getPinDotSpriteMethod = symbolGeneratorType?.GetMethod(
                "GetPinDotSprite",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            _junctionDotSprite = getPinDotSpriteMethod?.Invoke(null, null) as Sprite;
            return _junctionDotSprite;
        }

        private void CreateTraceLine(TraceSegment trace)
        {
            if (trace is null || _traceLines.ContainsKey(trace.SegmentId))
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

            if (_segmentCurrents.TryGetValue(trace.SegmentId, out var current)
                && Mathf.Abs(current) >= _flowMinVisibleCurrent)
            {
                CreateFlowLine(trace.SegmentId, line);
            }
        }

        private void CreateFlowLine(int segmentId, LineRenderer baseLine)
        {
            if (baseLine == null)
            {
                return;
            }

            if (_flowLines.TryGetValue(segmentId, out var existing) && existing != null)
            {
                existing.enabled = true;
                CopyLinePositions(baseLine, existing);
                _segmentFlowOffsets[segmentId] = 0f;
                return;
            }

            var flowLineObject = new GameObject($"TraceFlow_{segmentId}");
            flowLineObject.transform.SetParent(transform, false);

            var flowLine = flowLineObject.AddComponent<LineRenderer>();
            flowLine.useWorldSpace = true;
            flowLine.positionCount = 2;
            flowLine.startWidth = _wireWidth * _flowWidthMultiplier;
            flowLine.endWidth = _wireWidth * _flowWidthMultiplier;
            flowLine.startColor = _flowColor;
            flowLine.endColor = _flowColor;
            flowLine.textureMode = LineTextureMode.Tile;
            flowLine.alignment = LineAlignment.View;
            flowLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flowLine.receiveShadows = false;
            flowLine.sortingLayerID = baseLine.sortingLayerID;
            flowLine.sortingOrder = baseLine.sortingOrder + 1;
            flowLine.material = new Material(_flowLineMaterial)
            {
                mainTexture = _flowTexture
            };
            flowLine.enabled = true;

            CopyLinePositions(baseLine, flowLine);
            flowLine.textureScale = new Vector2(2f, 1f);

            _flowLines[segmentId] = flowLine;
            _segmentFlowOffsets[segmentId] = 0f;
        }

        private void HideFlowLine(int segmentId)
        {
            if (_flowLines.TryGetValue(segmentId, out var flowLine))
            {
                if (flowLine != null)
                {
                    if (flowLine.sharedMaterial != null && flowLine.sharedMaterial != _flowLineMaterial)
                    {
                        Destroy(flowLine.sharedMaterial);
                    }
                    flowLine.enabled = false;
                    Destroy(flowLine.gameObject);
                }

                _segmentCurrents.Remove(segmentId);
                _segmentFlowOffsets.Remove(segmentId);
                _flowLines.Remove(segmentId);
            }
        }

        private void UpdateFlowLineVisibility()
        {
            foreach (var pair in _flowLines)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.enabled = _segmentCurrents.ContainsKey(pair.Key);
            }
        }

        private static void CopyLinePositions(LineRenderer source, LineRenderer target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (source.positionCount >= 2 && target.positionCount >= 2)
            {
                target.SetPosition(0, source.GetPosition(0));
                target.SetPosition(1, source.GetPosition(1));
            }
        }

        private Texture2D CreateFlowTexture()
        {
            var texture = new Texture2D(FlowTextureWidth, FlowTextureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var pixels = TraceGeometryBuilder.GenerateFlowTexturePixels(FlowTextureWidth, FlowTextureHeight);
            texture.SetPixels(pixels);

            texture.Apply();
            return texture;
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
