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

        private static readonly int _ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color _pinDotColor = new Color(0f, 0.83f, 1f, 0.6f);
        private const float PinDotRadius = 0.18f;
        private const int PinDotTextureSize = 32;

        private static Sprite _pinDotSprite;
        private MaterialPropertyBlock _materialPropertyBlock;
        private readonly List<GameObject> _pinDots = new List<GameObject>();
        
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

        private void OnDestroy() => ClearPinDots();

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
#if UNITY_EDITOR
                Debug.Log($"ComponentView.Initialize: {_definition.DisplayName} ({_definition.Id})" + 
                    (_definition.Icon == null ? " - Warning: Icon is null" : ""));
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
                    0f,
                    localGridOffset.y * cellSize
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
