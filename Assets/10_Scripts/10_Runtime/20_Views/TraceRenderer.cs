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
        [SerializeField] private StageManager _stageManager;
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
        private Material _lineMaterial;
        private Material _flowLineMaterial;
        private Texture2D _flowTexture;
        private readonly Dictionary<int, LineRenderer> _traceLines = new Dictionary<int, LineRenderer>();
        private readonly Dictionary<int, LineRenderer> _flowLines = new Dictionary<int, LineRenderer>();
        private readonly Dictionary<int, float> _segmentCurrents = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _segmentFlowOffsets = new Dictionary<int, float>();

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
            if (_boardState == null)
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

            if (_stageManager == null)
                _stageManager = FindFirstObjectByType<StageManager>();
            if (_stageManager != null)
                _stageManager.OnStageLoaded += HandleBoardReset;
        }

        private void OnDestroy()
        {
            StopCurrentFlow();
            Unsubscribe();

            if (_stageManager != null)
                _stageManager.OnStageLoaded -= HandleBoardReset;

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

                var direction = Mathf.Sign(current);
                if (Mathf.Approximately(direction, 0f))
                {
                    continue;
                }

                var speed = Mathf.Abs(current) * _flowAnimationSpeedScale + _flowAnimationBaseSpeed;
                speed = Mathf.Min(speed, _flowAnimationMaxSpeed);

                float offset = 0f;
                if (_segmentFlowOffsets.TryGetValue(segmentId, out var currentOffset))
                {
                    offset = currentOffset;
                }

                offset += direction * speed * Time.deltaTime;
                offset = Mathf.Repeat(offset, 1f);
                _segmentFlowOffsets[segmentId] = offset;

                var flowMaterial = flowLine.material;
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
            if (_boardState == null)
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
                    Destroy(pair.Value.gameObject);
            }
            _flowLines.Clear();

            _segmentCurrents.Clear();
            _segmentFlowOffsets.Clear();

            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;
                if (_boardState != null)
                {
                    Subscribe();
                    foreach (var trace in _boardState.Traces)
                        CreateTraceLine(trace);
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
            if (_boardState == null)
            {
                return;
            }

            if (nodeVoltages == null)
            {
                ResetColors();
                return;
            }

            foreach (var pair in _traceLines)
            {
                if (pair.Value == null)
                    continue;

                TraceSegment targetTrace = null;
                foreach (var trace in _boardState.Traces)
                {
                    if (trace.SegmentId == pair.Key)
                    {
                        targetTrace = trace;
                        break;
                    }
                }

                if (targetTrace == null)
                {
                    pair.Value.startColor = _wireColor;
                    pair.Value.endColor = _wireColor;
                    continue;
                }

                var net = _boardState.GetNet(targetTrace.NetId);
                if (net == null || string.IsNullOrWhiteSpace(net.NetName))
                {
                    pair.Value.startColor = _wireColor;
                    pair.Value.endColor = _wireColor;
                    continue;
                }

                if (!nodeVoltages.TryGetValue(net.NetName, out var voltage))
                {
                    pair.Value.startColor = _wireColor;
                    pair.Value.endColor = _wireColor;
                    continue;
                }

                var color = Color.Lerp(_voltageMinColor, _voltageMaxColor,
                    NormalizeVoltage((float)voltage, minVoltage, maxVoltage));
                pair.Value.startColor = color;
                pair.Value.endColor = color;
            }
        }

        /// <summary>
        /// Applies animated current-flow visualization based on segment currents.
        /// </summary>
        /// <param name="segmentCurrents">Mapping from segment id to current (amps).</param>
        public void ApplyCurrentFlow(Dictionary<int, float> segmentCurrents)
        {
            StopCurrentFlow();

            if (segmentCurrents == null || segmentCurrents.Count == 0)
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
        }

        private static float NormalizeVoltage(float voltage, float minVoltage, float maxVoltage)
        {
            float range = maxVoltage - minVoltage;
            if (range <= float.Epsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01((voltage - minVoltage) / range);
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

            HideFlowLine(segmentId);
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

            for (int x = 0; x < FlowTextureWidth; x++)
            {
                float cycle = (x / (float)FlowTextureWidth) * 4f;
                float saw = Mathf.Abs((cycle % 2f) - 1f);
                float alpha = Mathf.Max(0f, 1f - saw * 2f);

                for (int y = 0; y < FlowTextureHeight; y++)
                {
                    float distanceToCenter = Mathf.Abs(y - (FlowTextureHeight - 1f) * 0.5f);
                    float vertical = Mathf.Clamp01(1f - (distanceToCenter / ((FlowTextureHeight - 1f) * 0.5f)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * vertical));
                }
            }

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
