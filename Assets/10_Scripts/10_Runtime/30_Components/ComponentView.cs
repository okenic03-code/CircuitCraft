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
        
        private void Awake()
        {
            // Auto-assign components if not set in Inspector
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (_labelText == null)
            {
#if UNITY_TEXTMESHPRO
                _labelText = GetComponentInChildren<TextMeshPro>();
#else
                _labelText = GetComponentInChildren<TextMesh>();
#endif
            }
            
            // Apply material if provided
            if (_spriteMaterial != null && _spriteRenderer != null)
            {
                _spriteRenderer.material = _spriteMaterial;
            }
        }
        
        /// <summary>
        /// Initialize the component view with a ComponentDefinition.
        /// Sets sprite and label based on definition properties.
        /// </summary>
        /// <param name="definition">ComponentDefinition to visualize.</param>
        public void Initialize(ComponentDefinition definition)
        {
            _definition = definition;
            
            if (_definition == null)
            {
                Debug.LogWarning("ComponentView.Initialize: Null ComponentDefinition provided.", this);
                return;
            }
            
            // Set sprite from definition prefab (if available)
            // Note: Prefab field contains the full prefab - we'll assume it has a SpriteRenderer
            // For now, we'll log and wait for proper sprite asset integration
            if (_spriteRenderer != null)
            {
                // TODO: Extract sprite from definition.Prefab or add Sprite field to ComponentDefinition
                Debug.Log($"ComponentView.Initialize: {_definition.DisplayName} ({_definition.Id})");
            }
            
            // Set label text
            if (_labelText != null)
            {
                // Display component value based on kind
                string label = FormatComponentLabel(_definition);
                _labelText.text = label;
            }
            
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
            _spriteRenderer.color = highlightColor;
        }
        
        /// <summary>
        /// Remove highlight and restore normal color.
        /// </summary>
        private void RemoveHighlight()
        {
            _spriteRenderer.color = _normalColor;
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
