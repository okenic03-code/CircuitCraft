using System.Collections.Generic;
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
        // Shared cache to avoid expensive scene-wide GridSettings lookups per ComponentView instance.
        private static GridSettings _cachedGridSettings;
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
                    _spriteRenderer.sprite = ComponentSymbolGenerator.GetOrCreateFallbackSprite(_definition.Kind);
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
            _ledGlowRenderer.sprite = ComponentSymbolGenerator.GetLedGlowSprite();
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
            _heatGlowRenderer.sprite = ComponentSymbolGenerator.GetHeatGlowSprite();
            _heatGlowRenderer.sortingLayerID = _spriteRenderer != null ? _spriteRenderer.sortingLayerID : 0;
            _heatGlowRenderer.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder - 2 : 0;
            _heatGlowRenderer.material = _spriteRenderer != null ? _spriteRenderer.sharedMaterial : null;
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

        private void CreatePinDots()
        {
            if (_definition?.Pins == null || _definition.Pins.Length == 0)
                return;

            float cellSize = ResolveGridCellSize();
            Sprite pinDotSprite = ComponentSymbolGenerator.GetPinDotSprite();
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
                pinDot.transform.localScale = Vector3.one * (ComponentSymbolGenerator.PinDotRadius * 2f);

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
            if (_cachedGridSettings != null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            if (_cachedGridSettings == null)
            {
                _cachedGridSettings = FindFirstObjectByType<GridSettings>();
            }

            if (_cachedGridSettings != null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            return 1f;
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
