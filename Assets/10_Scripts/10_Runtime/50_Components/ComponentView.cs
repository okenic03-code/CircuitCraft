using System.Collections.Generic;
using CircuitCraft.Utils;
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
        private ComponentOverlay _overlay;
        private ComponentEffects _effects;

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
            _overlay?.Cleanup();
            _effects?.Cleanup();
        }

        private void Init()
        {
            InitializeSpriteRenderer();
            InitializeLabelText();
            ApplySpriteMaterial();
            _materialPropertyBlock = new MaterialPropertyBlock();
            _overlay = new ComponentOverlay(
                transform,
                _spriteRenderer,
                _simulationOverlayOffset,
                _simulationOverlayScale,
                _simulationOverlayColor);
            _effects = new ComponentEffects(
                transform,
                _spriteRenderer,
                _ledGlowScale,
                _ledGlowAlpha,
                _ledGlowDefaultColor,
                _heatGlowScale,
                _heatGlowMaxAlpha,
                _heatGlowColor);
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
            _overlay?.ShowSimulationOverlay(text);
        }

        /// <summary>
        /// Hides any existing simulation overlay label.
        /// </summary>
        public void HideSimulationOverlay()
        {
            _overlay?.HideSimulationOverlay();
        }

        /// <summary>
        /// Shows or hides an LED glow effect.
        /// </summary>
        public void ShowLEDGlow(bool glow, Color glowColor)
        {
            _effects?.ShowLEDGlow(glow, glowColor);
        }

        /// <summary>
        /// Hides active LED glow effect.
        /// </summary>
        public void HideLEDGlow()
        {
            _effects?.HideLEDGlow();
        }

        /// <summary>
        /// Shows or hides a resistor heat glow effect.
        /// </summary>
        public void ShowResistorHeatGlow(bool glow, float normalizedPower)
        {
            _effects?.ShowResistorHeatGlow(glow, normalizedPower);
        }

        /// <summary>
        /// Hides active resistor heat glow effect.
        /// </summary>
        public void HideResistorHeatGlow()
        {
            _effects?.HideResistorHeatGlow();
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
                    return CircuitUnitFormatter.FormatResistance(definition.ResistanceOhms);
                
                case ComponentKind.Capacitor:
                    return CircuitUnitFormatter.FormatCapacitance(definition.CapacitanceFarads);
                
                case ComponentKind.Inductor:
                    return CircuitUnitFormatter.FormatInductance(definition.InductanceHenrys);
                
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
        
    }
}
