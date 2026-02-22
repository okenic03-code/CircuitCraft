using CircuitCraft.Data;
using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace CircuitCraft.Components
{
    /// <summary>
    /// Renders and updates the visual state of a placed component.
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
        private Color _hoverColor = new Color(1f, 1f, 0.5f, 1f);

        [SerializeField]
        [Tooltip("Highlight color when selected (stronger).")]
        private Color _selectedColor = new Color(0.5f, 1f, 0.5f, 1f);

        [Header("Advanced")]
        [SerializeField]
        [Tooltip("Optional material override for sprite rendering.")]
        private Material _spriteMaterial;

        [SerializeField]
        [Tooltip("Grid settings used for pin dot placement sizing.")]
        private GridSettings _gridSettings;

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

        private ComponentHighlight _highlight;
        private ComponentPinDots _pinDots;
        private ComponentOverlay _overlay;
        private ComponentEffects _effects;
        private ComponentDefinition _definition;
        private bool _isHovered;
        private bool _isSelected;

        /// <summary>
        /// Gets the currently bound component definition.
        /// </summary>
        public ComponentDefinition Definition => _definition;

        /// <summary>
        /// Gets or sets the grid position used by controller-side bookkeeping.
        /// </summary>
        public Vector2Int GridPosition { get; set; }

        private void Awake() => Init();

        private void OnDestroy()
        {
            _pinDots?.Cleanup();
            _overlay?.Cleanup();
            _effects?.Cleanup();
        }

        private void Init()
        {
            InitializeSpriteRenderer();
            InitializeLabelText();
            ApplySpriteMaterial();

            _highlight = new ComponentHighlight(_spriteRenderer, _normalColor);
            _pinDots = new ComponentPinDots(transform, _spriteRenderer, _gridSettings);
            _overlay = new ComponentOverlay(transform, _spriteRenderer, _simulationOverlayOffset, _simulationOverlayScale, _simulationOverlayColor);
            _effects = new ComponentEffects(transform, _spriteRenderer, _ledGlowScale, _ledGlowAlpha, _ledGlowDefaultColor, _heatGlowScale, _heatGlowMaxAlpha, _heatGlowColor);
        }

        private void InitializeSpriteRenderer()
        {
            if (_spriteRenderer is null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void InitializeLabelText()
        {
            if (_labelText is null)
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
            if (_spriteMaterial is not null && _spriteRenderer is not null)
            {
                _spriteRenderer.sharedMaterial = _spriteMaterial;
            }
        }

        /// <summary>
        /// Initializes visual data from a component definition.
        /// </summary>
        /// <param name="definition">Component data used for icon, label, and pin rendering.</param>
        public void Initialize(ComponentDefinition definition)
        {
            _pinDots?.ClearPinDots();
            _definition = definition;
            if (_definition is null)
            {
                Debug.LogWarning("ComponentView.Initialize: Null ComponentDefinition provided.", this);
                return;
            }

            if (_spriteRenderer is not null)
            {
                _spriteRenderer.sprite = _definition.Icon is not null
                    ? _definition.Icon
                    : ComponentSymbolGenerator.GetOrCreateFallbackSprite(_definition.Kind);
#if UNITY_EDITOR
                Debug.Log($"ComponentView.Initialize: {_definition.DisplayName} ({_definition.Id})" + (_definition.Icon is null ? " - Using fallback sprite" : ""));
#endif
            }

            if (_labelText is not null)
            {
                _labelText.text = ComponentLabelFormatter.FormatLabel(_definition);
            }

            _pinDots?.CreatePinDots(_definition, 0f);
            UpdateVisualState();
        }

        /// <summary>
        /// Updates hover highlighting state.
        /// </summary>
        /// <param name="isHovered">Whether the component is currently hovered.</param>
        public void SetHovered(bool isHovered)
        {
            _isHovered = isHovered;
            UpdateVisualState();
        }

        /// <summary>
        /// Updates selection highlighting state.
        /// </summary>
        /// <param name="isSelected">Whether the component is currently selected.</param>
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _highlight?.UpdateState(_isHovered, _isSelected, _hoverColor, _selectedColor);
        }

        /// <summary>
        /// Shows simulation text above the component.
        /// </summary>
        /// <param name="text">Overlay text to display.</param>
        public void ShowSimulationOverlay(string text)
        {
            _overlay?.ShowSimulationOverlay(text);
        }

        /// <summary>
        /// Hides simulation text overlay.
        /// </summary>
        public void HideSimulationOverlay()
        {
            _overlay?.HideSimulationOverlay();
        }

        /// <summary>
        /// Shows or hides LED glow effect.
        /// </summary>
        /// <param name="glow">Whether glow should be visible.</param>
        /// <param name="glowColor">Glow color override.</param>
        public void ShowLEDGlow(bool glow, Color glowColor)
        {
            _effects?.ShowLEDGlow(glow, glowColor);
        }

        /// <summary>
        /// Hides LED glow effect.
        /// </summary>
        public void HideLEDGlow()
        {
            _effects?.HideLEDGlow();
        }

        /// <summary>
        /// Shows or hides resistor heat glow effect.
        /// </summary>
        /// <param name="glow">Whether glow should be visible.</param>
        /// <param name="normalizedPower">Normalized power value in range 0..1.</param>
        public void ShowResistorHeatGlow(bool glow, float normalizedPower)
        {
            _effects?.ShowResistorHeatGlow(glow, normalizedPower);
        }

        /// <summary>
        /// Hides resistor heat glow effect.
        /// </summary>
        public void HideResistorHeatGlow()
        {
            _effects?.HideResistorHeatGlow();
        }
    }
}
