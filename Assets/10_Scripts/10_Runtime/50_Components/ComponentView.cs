using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CircuitCraft.Data;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace CircuitCraft.Components
{
    /// <summary>
    /// Visual representation of a placed component on the grid.
    /// Displays component sprite, label, and hover/selection highlighting.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ComponentView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField]
        [Tooltip("SpriteRenderer for displaying the component's visual appearance.")]
        private SpriteRenderer _spriteRenderer;
        
        [SerializeField]
        [Tooltip("Text label for displaying component ID/value (TextMeshPro or Unity UI Text).")]
#if UNITY_TEXTMESHPRO
        private TextMeshPro _labelText;
#else
        private TextMesh _labelText;
#endif
        
        [Header("Highlight Settings")]
        [SerializeField]
        [Tooltip("Normal color when not hovered or selected.")]
        private Color _normalColor = Color.white;
        
        [SerializeField]
        [Tooltip("Highlight color when hovered (subtle).")]
        private Color _hoverColor = new Color(1f, 1f, 0.5f, 1f); // Light yellow
        
        [SerializeField]
        [Tooltip("Highlight color when selected (stronger).")]
        private Color _selectedColor = new Color(0.5f, 1f, 0.5f, 1f); // Light green
        
        [Header("Advanced")]
        [SerializeField]
        [Tooltip("Optional material override for sprite rendering.")]
        private Material _spriteMaterial;

        [Header("Simulation Visualization")]
        [SerializeField]
        [Tooltip("Offset for voltage/current overlay text.")]
        private Vector3 _simulationOverlayOffset = new Vector3(0f, 0.55f, 0f);

        [SerializeField]
        [Tooltip("Scale multiplier for the simulation overlay text object.")]
        private float _simulationOverlayScale = 0.12f;

        [SerializeField]
        [Tooltip("Color used for simulation overlay text.")]
        private Color _simulationOverlayColor = Color.white;

        [SerializeField]
        [Tooltip("Glow scale used for active LEDs.")]
        private float _ledGlowScale = 1.35f;

        [SerializeField]
        [Tooltip("Glow alpha used for active LEDs.")]
        private float _ledGlowAlpha = 0.65f;

        [SerializeField]
        [Tooltip("Default LED glow color when current is flowing.")]
        private Color _ledGlowDefaultColor = new Color(1f, 0.7f, 0.05f, 1f);

        [SerializeField]
        [Tooltip("Glow scale used for resistor heat effects.")]
        private float _heatGlowScale = 1.45f;

        [SerializeField]
        [Tooltip("Glow alpha used for resistor heat at max intensity.")]
        private float _heatGlowMaxAlpha = 0.65f;

        [SerializeField]
        [Tooltip("Default resistor heat glow color.")]
        private Color _heatGlowColor = new Color(1f, 0.35f, 0.05f, 1f);

        private static readonly int _ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color _pinDotColor = new Color(0f, 0.83f, 1f, 0.6f);
        private const float PinDotRadius = 0.18f;
        private const int PinDotTextureSize = 32;
        private static readonly int _ledGlowTextureSize = 64;
        private static readonly int _heatGlowTextureSize = 64;
        private const int FallbackSymbolTextureSize = 64;
        private const int FallbackLineThickness = 2;

        private static Sprite _pinDotSprite;
        private static Sprite _ledGlowSprite;
        private static Sprite _heatGlowSprite;
        private static readonly Dictionary<ComponentKind, Sprite> _fallbackSprites = new Dictionary<ComponentKind, Sprite>();
        private MaterialPropertyBlock _materialPropertyBlock;
        private readonly List<GameObject> _pinDots = new List<GameObject>();
        private GameObject _simulationOverlayObject;
        private GameObject _ledGlowObject;
        private SpriteRenderer _ledGlowRenderer;
        private GameObject _heatGlowObject;
        private SpriteRenderer _heatGlowRenderer;

    #if UNITY_TEXTMESHPRO
        private TextMeshPro _simulationOverlayText;
    #else
        private TextMesh _simulationOverlayText;
    #endif

        // State
        private ComponentDefinition _definition;
        private bool _isHovered;
        private bool _isSelected;
        
        /// <summary>
        /// Component definition associated with this view.
        /// </summary>
        public ComponentDefinition Definition => _definition;
        
        /// <summary>
        /// Grid position of this component (set during placement).
        /// </summary>
        public Vector2Int GridPosition { get; set; }
        
        private void Awake() => Init();

        private void OnDestroy()
        {
            ClearPinDots();
            ClearSimulationOverlay();
            ClearLEDGlow();
            ClearHeatGlow();
        }

        private void Init()
        {
            InitializeSpriteRenderer();
            InitializeLabelText();
            ApplySpriteMaterial();
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        private void InitializeSpriteRenderer()
        {
            // Auto-assign SpriteRenderer if not set in Inspector
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void InitializeLabelText()
        {
            // Auto-assign label text component if not set in Inspector
            if (_labelText == null)
            {
#if UNITY_TEXTMESHPRO
                _labelText = GetComponentInChildren<TextMeshPro>();
#else
                _labelText = GetComponentInChildren<TextMesh>();
#endif
            }
        }

        private void ApplySpriteMaterial()
        {
            // Apply custom material if provided
            if (_spriteMaterial != null && _spriteRenderer != null)
            {
                _spriteRenderer.sharedMaterial = _spriteMaterial;
            }
        }
        
        /// <summary>
        /// Initialize the component view with a ComponentDefinition.
        /// Sets sprite and label based on definition properties.
        /// </summary>
        /// <param name="definition">ComponentDefinition to visualize.</param>
        public void Initialize(ComponentDefinition definition)
        {
            ClearPinDots();
            _definition = definition;
            
            if (_definition == null)
            {
                Debug.LogWarning("ComponentView.Initialize: Null ComponentDefinition provided.", this);
                return;
            }
            
            // Set sprite from definition
            if (_spriteRenderer != null)
            {
                if (_definition.Icon != null)
                {
                    _spriteRenderer.sprite = _definition.Icon;
                }
                else
                {
                    _spriteRenderer.sprite = GetOrCreateFallbackSprite(_definition.Kind);
                }
#if UNITY_EDITOR
                Debug.Log($"ComponentView.Initialize: {_definition.DisplayName} ({_definition.Id})" + 
                    (_definition.Icon == null ? " - Using fallback sprite" : ""));
#endif
            }
            
            // Set label text
            if (_labelText != null)
            {
                // Display component value based on kind
                string label = FormatComponentLabel(_definition);
                _labelText.text = label;
            }

            CreatePinDots();
            
            // Apply normal color initially
            UpdateVisualState();
        }
        
        /// <summary>
        /// Set hover state for visual feedback.
        /// </summary>
        /// <param name="isHovered">True if hovered, false otherwise.</param>
        public void SetHovered(bool isHovered)
        {
            _isHovered = isHovered;
            UpdateVisualState();
        }
        
        /// <summary>
        /// Set selection state for visual feedback.
        /// </summary>
        /// <param name="isSelected">True if selected, false otherwise.</param>
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            UpdateVisualState();
        }
        
        /// <summary>
        /// Update visual state based on hover/selection flags.
        /// Priority: Selected > Hovered > Normal
        /// </summary>
        private void UpdateVisualState()
        {
            if (_spriteRenderer == null) return;
            
            if (_isSelected)
            {
                ApplyHighlight(_selectedColor);
            }
            else if (_isHovered)
            {
                ApplyHighlight(_hoverColor);
            }
            else
            {
                RemoveHighlight();
            }
        }
        
        /// <summary>
        /// Apply highlight color to sprite renderer.
        /// </summary>
        /// <param name="highlightColor">Color to apply.</param>
        private void ApplyHighlight(Color highlightColor)
        {
            if (_materialPropertyBlock == null) return;

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_ColorProperty, highlightColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
        
        /// <summary>
        /// Remove highlight and restore normal color.
        /// </summary>
        private void RemoveHighlight()
        {
            if (_materialPropertyBlock == null) return;

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_ColorProperty, _normalColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        /// <summary>
        /// Shows a simulation overlay label with voltage/current values.
        /// </summary>
        public void ShowSimulationOverlay(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideSimulationOverlay();
                return;
            }

            EnsureSimulationOverlayText();
            if (_simulationOverlayText == null)
            {
                return;
            }

            _simulationOverlayText.text = text;
            _simulationOverlayText.color = _simulationOverlayColor;
            _simulationOverlayObject.SetActive(true);
        }

        /// <summary>
        /// Hides any existing simulation overlay label.
        /// </summary>
        public void HideSimulationOverlay()
        {
            if (_simulationOverlayObject != null)
            {
                _simulationOverlayObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows or hides an LED glow effect.
        /// </summary>
        public void ShowLEDGlow(bool glow, Color glowColor)
        {
            if (!glow)
            {
                HideLEDGlow();
                return;
            }

            EnsureLEDGlowObject();
            if (_ledGlowRenderer == null)
            {
                return;
            }

            var color = glowColor == default ? _ledGlowDefaultColor : glowColor;
            _ledGlowRenderer.color = new Color(color.r, color.g, color.b, _ledGlowAlpha);
            _ledGlowObject.SetActive(true);
        }

        /// <summary>
        /// Hides active LED glow effect.
        /// </summary>
        public void HideLEDGlow()
        {
            if (_ledGlowObject != null)
            {
                _ledGlowObject.SetActive(false);
            }
        }

        private void EnsureSimulationOverlayText()
        {
            if (_simulationOverlayObject != null)
            {
                return;
            }

            _simulationOverlayObject = new GameObject("SimulationOverlay");
            _simulationOverlayObject.transform.SetParent(transform, false);
            _simulationOverlayObject.transform.localPosition = _simulationOverlayOffset;
            _simulationOverlayObject.transform.localScale = Vector3.one * _simulationOverlayScale;

        #if UNITY_TEXTMESHPRO
            var overlayText = _simulationOverlayObject.AddComponent<TextMeshPro>();
            overlayText.alignment = TMPro.TextAlignmentOptions.Center;
            overlayText.fontSize = 4f;
            overlayText.sortingLayerID = _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;
            overlayText.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder + 2 : 0;
            overlayText.autoSizeTextContainer = false;
            _simulationOverlayText = overlayText;
        #else
            var overlayText = _simulationOverlayObject.AddComponent<TextMesh>();
            overlayText.alignment = TextAlignment.Center;
            overlayText.anchor = TextAnchor.MiddleCenter;
            overlayText.characterSize = 0.08f;
            overlayText.fontSize = 28;
            overlayText.GetComponent<MeshRenderer>().sortingLayerID =
                _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;
            overlayText.GetComponent<MeshRenderer>().sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder + 2 : 0;
            _simulationOverlayText = overlayText;
        #endif

            _simulationOverlayObject.SetActive(false);
        }

        private void EnsureLEDGlowObject()
        {
            if (_ledGlowObject != null)
            {
                _ledGlowObject.SetActive(true);
                return;
            }

            _ledGlowObject = new GameObject("LEDGlow");
            _ledGlowObject.transform.SetParent(transform, false);
            _ledGlowObject.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            _ledGlowObject.transform.localScale = Vector3.one * _ledGlowScale;

            _ledGlowRenderer = _ledGlowObject.AddComponent<SpriteRenderer>();
            _ledGlowRenderer.sprite = GetLedGlowSprite();
            _ledGlowRenderer.sortingLayerID = _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;
            _ledGlowRenderer.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder - 1 : 0;
            _ledGlowRenderer.material = _spriteRenderer != null ? _spriteRenderer.sharedMaterial : null;
        }

        /// <summary>
        /// Shows or hides a resistor heat glow effect.
        /// </summary>
        public void ShowResistorHeatGlow(bool glow, float normalizedPower)
        {
            if (!glow)
            {
                HideResistorHeatGlow();
                return;
            }

            EnsureHeatGlowObject();
            if (_heatGlowRenderer == null)
            {
                return;
            }

            var safePower = Mathf.Clamp01(normalizedPower);
            var color = _heatGlowColor;
            color.a = _heatGlowMaxAlpha * safePower;
            _heatGlowRenderer.color = color;
            _heatGlowObject.SetActive(true);
        }

        /// <summary>
        /// Hides active resistor heat glow effect.
        /// </summary>
        public void HideResistorHeatGlow()
        {
            if (_heatGlowObject != null)
            {
                _heatGlowObject.SetActive(false);
            }
        }

        private void EnsureHeatGlowObject()
        {
            if (_heatGlowObject != null)
            {
                _heatGlowObject.SetActive(true);
                return;
            }

            _heatGlowObject = new GameObject("HeatGlow");
            _heatGlowObject.transform.SetParent(transform, false);
            _heatGlowObject.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            _heatGlowObject.transform.localScale = Vector3.one * _heatGlowScale;

            _heatGlowRenderer = _heatGlowObject.AddComponent<SpriteRenderer>();
            _heatGlowRenderer.sprite = GetHeatGlowSprite();
            _heatGlowRenderer.sortingLayerID = _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;
            _heatGlowRenderer.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder - 2 : 0;
            _heatGlowRenderer.material = _spriteRenderer != null ? _spriteRenderer.sharedMaterial : null;
        }

        private static Sprite GetOrCreateFallbackSprite(ComponentKind kind)
        {
            if (_fallbackSprites.TryGetValue(kind, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = new Texture2D(FallbackSymbolTextureSize, FallbackSymbolTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < FallbackSymbolTextureSize; y++)
            {
                for (int x = 0; x < FallbackSymbolTextureSize; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            DrawComponentSymbol(texture, kind, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, FallbackSymbolTextureSize, FallbackSymbolTextureSize),
                new Vector2(0.5f, 0.5f),
                FallbackSymbolTextureSize
            );

            sprite.name = $"{kind}_FallbackSprite";
            _fallbackSprites[kind] = sprite;
            return sprite;
        }

        private static void DrawComponentSymbol(Texture2D texture, ComponentKind kind, Color color)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    DrawResistorSymbol(texture, color);
                    break;

                case ComponentKind.Capacitor:
                    DrawCapacitorSymbol(texture, color);
                    break;

                case ComponentKind.Inductor:
                    DrawInductorSymbol(texture, color);
                    break;

                case ComponentKind.Diode:
                    DrawDiodeSymbol(texture, color);
                    break;

                case ComponentKind.LED:
                    DrawLedSymbol(texture, color);
                    break;

                case ComponentKind.BJT:
                    DrawBjtSymbol(texture, color);
                    break;

                case ComponentKind.MOSFET:
                    DrawMosfetSymbol(texture, color);
                    break;

                case ComponentKind.VoltageSource:
                    DrawVoltageSourceSymbol(texture, color);
                    break;

                case ComponentKind.CurrentSource:
                    DrawCurrentSourceSymbol(texture, color);
                    break;

                case ComponentKind.Ground:
                    DrawGroundSymbol(texture, color);
                    break;

                case ComponentKind.Probe:
                    DrawProbeSymbol(texture, color);
                    break;

                case ComponentKind.ZenerDiode:
                    DrawZenerDiodeSymbol(texture, color);
                    break;

                default:
                    DrawLineThick(texture, 16, 16, 48, 16, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 16, 48, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 48, 16, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 16, 48, 16, 16, FallbackLineThickness, color);
                    DrawLineThick(texture, 16, 16, 48, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 16, 16, 48, FallbackLineThickness, color);
                    break;
            }
        }

        private static void DrawResistorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);

            int[] xPoints = { 16, 20, 24, 28, 32, 36, 40, 44, 48 };
            int[] yPoints = { 32, 22, 42, 22, 42, 22, 42, 22, 32 };

            for (int i = 0; i < xPoints.Length - 1; i++)
            {
                DrawLineThick(texture, xPoints[i], yPoints[i], xPoints[i + 1], yPoints[i + 1], FallbackLineThickness, color);
            }
        }

        private static void DrawVoltageSourceSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 38, 32, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 28, 42, 36, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 28, 22, 36, 22, FallbackLineThickness, color);
        }

        private static void DrawCurrentSourceSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawArrow(texture, 32, 22, 32, 42, color);
        }

        private static void DrawGroundSymbol(Texture2D texture, Color color)
        {
            DrawLineThick(texture, 32, 60, 32, 38, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 38, 48, 38, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 30, 42, 30, FallbackLineThickness, color);
            DrawLineThick(texture, 27, 22, 37, 22, FallbackLineThickness, color);
        }

        private static void DrawCapacitorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 26, 38, color);
            DrawLineThick(texture, 26, 18, 26, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 38, 18, 38, 46, FallbackLineThickness, color);
        }

        private static void DrawInductorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);

            for (int i = 0; i < 4; i++)
            {
                int centerX = 20 + (i * 8);
                DrawArc(texture, centerX, 32, 4, 180f, 0f, FallbackLineThickness, color);
            }
        }

        private static void DrawDiodeSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 44, color);
            DrawLineThick(texture, 16, 20, 16, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 20, 38, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 44, 38, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 38, 32, 44, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 44, 18, 44, 46, FallbackLineThickness, color);
        }

        private static void DrawLedSymbol(Texture2D texture, Color color)
        {
            DrawDiodeSymbol(texture, color);
            DrawArrow(texture, 44, 38, 54, 48, color);
            DrawArrow(texture, 40, 32, 50, 42, color);
        }

        private static void DrawZenerDiodeSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 42, color);
            DrawLineThick(texture, 16, 20, 16, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 20, 36, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 44, 36, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 36, 32, 42, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 20, 42, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 44, 48, 47, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 20, 48, 17, FallbackLineThickness, color);
        }

        private static void DrawBjtSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);

            DrawLineThick(texture, 4, 32, 18, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 24, 22, 24, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 18, 32, 24, 32, FallbackLineThickness, color);

            DrawLineThick(texture, 24, 38, 42, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 46, 58, 56, FallbackLineThickness, color);

            DrawLineThick(texture, 24, 26, 42, 18, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 18, 58, 8, FallbackLineThickness, color);
            DrawArrow(texture, 34, 24, 42, 16, color);
        }

        private static void DrawMosfetSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);

            DrawLineThick(texture, 4, 32, 20, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 22, 22, 42, FallbackLineThickness, color);

            DrawLineThick(texture, 34, 20, 34, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 34, 40, 46, 50, FallbackLineThickness, color);
            DrawLineThick(texture, 46, 50, 58, 58, FallbackLineThickness, color);
            DrawLineThick(texture, 34, 24, 46, 14, FallbackLineThickness, color);
            DrawLineThick(texture, 46, 14, 58, 6, FallbackLineThickness, color);
            DrawArrow(texture, 40, 20, 48, 12, color);
        }

        private static void DrawProbeSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 32, 42, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 22, 32, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 4, 32, 16, FallbackLineThickness, color);
            DrawCircleThick(texture, 32, 32, 2, 1, color);
        }

        private static void DrawHorizontalLeads(Texture2D texture, int y, int leftInner, int rightInner, Color color)
        {
            DrawLineThick(texture, 4, y, leftInner, y, FallbackLineThickness, color);
            DrawLineThick(texture, rightInner, y, 60, y, FallbackLineThickness, color);
        }

        private static void DrawArrow(Texture2D texture, int startX, int startY, int endX, int endY, Color color)
        {
            DrawLineThick(texture, startX, startY, endX, endY, FallbackLineThickness, color);

            Vector2 direction = new Vector2(endX - startX, endY - startY);
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 back = -direction * 5f;
            Vector2 endPoint = new Vector2(endX, endY);

            Vector2 left = endPoint + back + (perpendicular * 3f);
            Vector2 right = endPoint + back - (perpendicular * 3f);

            DrawLineThick(texture, endX, endY, Mathf.RoundToInt(left.x), Mathf.RoundToInt(left.y), FallbackLineThickness, color);
            DrawLineThick(texture, endX, endY, Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y), FallbackLineThickness, color);
        }

        private static void DrawArc(Texture2D texture, int centerX, int centerY, int radius, float startDegrees, float endDegrees, int thickness, Color color)
        {
            int segments = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(endDegrees - startDegrees) / 6f));
            float previousAngle = startDegrees * Mathf.Deg2Rad;
            int prevX = centerX + Mathf.RoundToInt(Mathf.Cos(previousAngle) * radius);
            int prevY = centerY + Mathf.RoundToInt(Mathf.Sin(previousAngle) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                int nextX = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
                int nextY = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * radius);

                DrawLineThick(texture, prevX, prevY, nextX, nextY, thickness, color);

                prevX = nextX;
                prevY = nextY;
            }
        }

        private static void DrawLineThick(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            if (thickness <= 1)
            {
                DrawLine(texture, x0, y0, x1, y1, color);
                return;
            }

            int minOffset = -(thickness / 2);
            int maxOffset = minOffset + thickness - 1;

            for (int offsetY = minOffset; offsetY <= maxOffset; offsetY++)
            {
                for (int offsetX = minOffset; offsetX <= maxOffset; offsetX++)
                {
                    DrawLine(texture, x0 + offsetX, y0 + offsetY, x1 + offsetX, y1 + offsetY, color);
                }
            }
        }

        private static void DrawCircleThick(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            if (thickness <= 1)
            {
                DrawCircle(texture, centerX, centerY, radius, color);
                return;
            }

            int minOffset = -(thickness / 2);
            int maxOffset = minOffset + thickness - 1;

            for (int offset = minOffset; offset <= maxOffset; offset++)
            {
                int currentRadius = radius + offset;
                if (currentRadius > 0)
                {
                    DrawCircle(texture, centerX, centerY, currentRadius, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int x = radius;
            int y = 0;
            int err = 1 - radius;

            while (x >= y)
            {
                SetPixelSafe(texture, centerX + x, centerY + y, color);
                SetPixelSafe(texture, centerX + y, centerY + x, color);
                SetPixelSafe(texture, centerX - y, centerY + x, color);
                SetPixelSafe(texture, centerX - x, centerY + y, color);
                SetPixelSafe(texture, centerX - x, centerY - y, color);
                SetPixelSafe(texture, centerX - y, centerY - x, color);
                SetPixelSafe(texture, centerX + y, centerY - x, color);
                SetPixelSafe(texture, centerX + x, centerY - y, color);

                y++;
                if (err < 0)
                {
                    err += (2 * y) + 1;
                }
                else
                {
                    x--;
                    err += (2 * (y - x)) + 1;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }

        private static Sprite GetHeatGlowSprite()
        {
            if (_heatGlowSprite != null)
            {
                return _heatGlowSprite;
            }

            var texture = new Texture2D(_heatGlowTextureSize, _heatGlowTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2(_heatGlowTextureSize * 0.5f, _heatGlowTextureSize * 0.5f);
            float radius = _heatGlowTextureSize * 0.5f;

            for (int y = 0; y < _heatGlowTextureSize; y++)
            {
                for (int x = 0; x < _heatGlowTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(dist / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            _heatGlowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, _heatGlowTextureSize, _heatGlowTextureSize),
                new Vector2(0.5f, 0.5f),
                _heatGlowTextureSize
            );

            return _heatGlowSprite;
        }

        private void ClearSimulationOverlay()
        {
            if (_simulationOverlayObject != null)
            {
                Destroy(_simulationOverlayObject);
                _simulationOverlayObject = null;
                _simulationOverlayText = null;
            }
        }

        private void ClearLEDGlow()
        {
            if (_ledGlowObject != null)
            {
                Destroy(_ledGlowObject);
                _ledGlowObject = null;
                _ledGlowRenderer = null;
            }
        }

        private void ClearHeatGlow()
        {
            if (_heatGlowObject != null)
            {
                Destroy(_heatGlowObject);
                _heatGlowObject = null;
                _heatGlowRenderer = null;
            }
        }

        private static Sprite GetLedGlowSprite()
        {
            if (_ledGlowSprite != null)
            {
                return _ledGlowSprite;
            }

            var texture = new Texture2D(_ledGlowTextureSize, _ledGlowTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2(_ledGlowTextureSize * 0.5f, _ledGlowTextureSize * 0.5f);
            float radius = _ledGlowTextureSize * 0.5f;

            for (int y = 0; y < _ledGlowTextureSize; y++)
            {
                for (int x = 0; x < _ledGlowTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(dist / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            _ledGlowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, _ledGlowTextureSize, _ledGlowTextureSize),
                new Vector2(0.5f, 0.5f),
                _ledGlowTextureSize
            );

            return _ledGlowSprite;
        }

        private void CreatePinDots()
        {
            if (_definition?.Pins == null || _definition.Pins.Length == 0)
                return;

            float cellSize = ResolveGridCellSize();
            Sprite pinDotSprite = GetPinDotSprite();
            int dotSortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder + 1 : 1;
            int sortingLayerId = _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;

            for (int i = 0; i < _definition.Pins.Length; i++)
            {
                PinDefinition pinDef = _definition.Pins[i];
                if (pinDef == null)
                    continue;

                GameObject pinDot = new GameObject($"PinDot_{pinDef.PinName}");
                pinDot.transform.SetParent(transform, false);

                Vector2 localGridOffset = pinDef.LocalPosition;
                pinDot.transform.localPosition = new Vector3(
                    localGridOffset.x * cellSize,
                    localGridOffset.y * cellSize,
                    0f
                );
                pinDot.transform.localScale = Vector3.one * (PinDotRadius * 2f);

                SpriteRenderer dotRenderer = pinDot.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = pinDotSprite;
                dotRenderer.color = _pinDotColor;
                dotRenderer.sortingLayerID = sortingLayerId;
                dotRenderer.sortingOrder = dotSortingOrder;

                _pinDots.Add(pinDot);
            }
        }

        private void ClearPinDots()
        {
            for (int i = 0; i < _pinDots.Count; i++)
            {
                if (_pinDots[i] != null)
                {
                    Destroy(_pinDots[i]);
                }
            }

            _pinDots.Clear();
        }

        private float ResolveGridCellSize()
        {
            GridSettings[] settings = Resources.FindObjectsOfTypeAll<GridSettings>();
            for (int i = 0; i < settings.Length; i++)
            {
                GridSettings gridSettings = settings[i];
                if (gridSettings != null && gridSettings.CellSize > Mathf.Epsilon)
                {
                    return gridSettings.CellSize;
                }
            }

            return 1f;
        }

        private static Sprite GetPinDotSprite()
        {
            if (_pinDotSprite != null)
                return _pinDotSprite;

            Texture2D texture = new Texture2D(PinDotTextureSize, PinDotTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(PinDotTextureSize * 0.5f, PinDotTextureSize * 0.5f);
            float radius = PinDotTextureSize * 0.5f;

            for (int y = 0; y < PinDotTextureSize; y++)
            {
                for (int x = 0; x < PinDotTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist - radius + 1f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            _pinDotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, PinDotTextureSize, PinDotTextureSize),
                new Vector2(0.5f, 0.5f),
                PinDotTextureSize
            );

            return _pinDotSprite;
        }
        
        /// <summary>
        /// Format component label based on component kind and values.
        /// </summary>
        /// <param name="definition">ComponentDefinition to format.</param>
        /// <returns>Formatted label string.</returns>
        private string FormatComponentLabel(ComponentDefinition definition)
        {
            switch (definition.Kind)
            {
                case ComponentKind.Resistor:
                    return FormatResistance(definition.ResistanceOhms);
                
                case ComponentKind.Capacitor:
                    return FormatCapacitance(definition.CapacitanceFarads);
                
                case ComponentKind.Inductor:
                    return FormatInductance(definition.InductanceHenrys);
                
                case ComponentKind.VoltageSource:
                    return $"{definition.VoltageVolts}V";
                
                case ComponentKind.CurrentSource:
                    return $"{definition.CurrentAmps}A";
                
                case ComponentKind.Ground:
                    return "GND";
                
                default:
                    return definition.DisplayName;
            }
        }
        
        /// <summary>
        /// Format resistance value with engineering notation.
        /// </summary>
        private string FormatResistance(float ohms)
        {
            if (ohms >= 1_000_000)
                return $"{ohms / 1_000_000:0.##}MΩ";
            if (ohms >= 1_000)
                return $"{ohms / 1_000:0.##}kΩ";
            return $"{ohms:0.##}Ω";
        }
        
        /// <summary>
        /// Format capacitance value with engineering notation.
        /// </summary>
        private string FormatCapacitance(float farads)
        {
            if (farads >= 1)
                return $"{farads:0.##}F";
            if (farads >= 0.001)
                return $"{farads * 1_000:0.##}mF";
            if (farads >= 0.000_001)
                return $"{farads * 1_000_000:0.##}µF";
            if (farads >= 0.000_000_001)
                return $"{farads * 1_000_000_000:0.##}nF";
            return $"{farads * 1_000_000_000_000:0.##}pF";
        }
        
        /// <summary>
        /// Format inductance value with engineering notation.
        /// </summary>
        private string FormatInductance(float henrys)
        {
            if (henrys >= 1)
                return $"{henrys:0.##}H";
            if (henrys >= 0.001)
                return $"{henrys * 1_000:0.##}mH";
            if (henrys >= 0.000_001)
                return $"{henrys * 1_000_000:0.##}µH";
            return $"{henrys * 1_000_000_000:0.##}nH";
        }
    }
}
