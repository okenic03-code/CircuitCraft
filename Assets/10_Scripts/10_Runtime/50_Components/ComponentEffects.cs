using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Manages LED and resistor heat glow effects for component visuals.
    /// </summary>
    internal sealed class ComponentEffects
    {
        private readonly Transform _parent;
        private readonly SpriteRenderer _parentSprite;
        private readonly float _ledGlowScale;
        private readonly float _ledGlowAlpha;
        private readonly Color _ledGlowDefaultColor;
        private readonly float _heatGlowScale;
        private readonly float _heatGlowMaxAlpha;
        private readonly Color _heatGlowColor;

        private GameObject _ledGlowObject;
        private SpriteRenderer _ledGlowRenderer;
        private GameObject _heatGlowObject;
        private SpriteRenderer _heatGlowRenderer;

        /// <summary>
        /// Creates a visual effects helper for a component.
        /// </summary>
        /// <param name="parent">Transform used as effect parent.</param>
        /// <param name="parentSprite">Parent sprite renderer for sorting and material context.</param>
        /// <param name="ledGlowScale">LED glow local scale multiplier.</param>
        /// <param name="ledGlowAlpha">LED glow alpha value.</param>
        /// <param name="ledGlowDefaultColor">Default LED glow color.</param>
        /// <param name="heatGlowScale">Heat glow local scale multiplier.</param>
        /// <param name="heatGlowMaxAlpha">Maximum alpha for heat glow.</param>
        /// <param name="heatGlowColor">Base heat glow color.</param>
        public ComponentEffects(
            Transform parent,
            SpriteRenderer parentSprite,
            float ledGlowScale,
            float ledGlowAlpha,
            Color ledGlowDefaultColor,
            float heatGlowScale,
            float heatGlowMaxAlpha,
            Color heatGlowColor)
        {
            _parent = parent;
            _parentSprite = parentSprite;
            _ledGlowScale = ledGlowScale;
            _ledGlowAlpha = ledGlowAlpha;
            _ledGlowDefaultColor = ledGlowDefaultColor;
            _heatGlowScale = heatGlowScale;
            _heatGlowMaxAlpha = heatGlowMaxAlpha;
            _heatGlowColor = heatGlowColor;
        }

        /// <summary>
        /// Shows or hides the LED glow effect.
        /// </summary>
        /// <param name="glow">Whether glow should be visible.</param>
        /// <param name="glowColor">Glow color override.</param>
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
        /// Hides the LED glow effect.
        /// </summary>
        public void HideLEDGlow()
        {
            if (_ledGlowObject != null)
            {
                _ledGlowObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows or hides resistor heat glow based on normalized power.
        /// </summary>
        /// <param name="glow">Whether glow should be visible.</param>
        /// <param name="normalizedPower">Normalized power value in range 0..1.</param>
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
        /// Hides the resistor heat glow effect.
        /// </summary>
        public void HideResistorHeatGlow()
        {
            if (_heatGlowObject != null)
            {
                _heatGlowObject.SetActive(false);
            }
        }

        /// <summary>
        /// Clears all instantiated effect objects.
        /// </summary>
        public void Cleanup()
        {
            ClearLEDGlow();
            ClearHeatGlow();
        }

        private void EnsureLEDGlowObject()
        {
            if (_ledGlowObject != null)
            {
                _ledGlowObject.SetActive(true);
                return;
            }

            _ledGlowObject = new GameObject("LEDGlow");
            _ledGlowObject.transform.SetParent(_parent, false);
            _ledGlowObject.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            _ledGlowObject.transform.localScale = Vector3.one * _ledGlowScale;

            _ledGlowRenderer = _ledGlowObject.AddComponent<SpriteRenderer>();
            _ledGlowRenderer.sprite = ComponentSymbolGenerator.GetLedGlowSprite();
            _ledGlowRenderer.sortingLayerID = _parentSprite != null ? _parentSprite.sortingLayerID : 0;
            _ledGlowRenderer.sortingOrder = _parentSprite != null ? _parentSprite.sortingOrder - 1 : 0;
            _ledGlowRenderer.material = _parentSprite != null ? _parentSprite.sharedMaterial : null;
        }

        private void ClearLEDGlow()
        {
            if (_ledGlowObject != null)
            {
                Object.Destroy(_ledGlowObject);
                _ledGlowObject = null;
                _ledGlowRenderer = null;
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
            _heatGlowObject.transform.SetParent(_parent, false);
            _heatGlowObject.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            _heatGlowObject.transform.localScale = Vector3.one * _heatGlowScale;

            _heatGlowRenderer = _heatGlowObject.AddComponent<SpriteRenderer>();
            _heatGlowRenderer.sprite = ComponentSymbolGenerator.GetHeatGlowSprite();
            _heatGlowRenderer.sortingLayerID = _parentSprite != null ? _parentSprite.sortingLayerID : 0;
            _heatGlowRenderer.sortingOrder = _parentSprite != null ? _parentSprite.sortingOrder - 2 : 0;
            _heatGlowRenderer.material = _parentSprite != null ? _parentSprite.sharedMaterial : null;
        }

        private void ClearHeatGlow()
        {
            if (_heatGlowObject != null)
            {
                Object.Destroy(_heatGlowObject);
                _heatGlowObject = null;
                _heatGlowRenderer = null;
            }
        }
    }
}
