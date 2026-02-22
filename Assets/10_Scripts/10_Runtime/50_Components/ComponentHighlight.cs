using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Applies and clears sprite highlight colors for hover and selection states.
    /// </summary>
    internal sealed class ComponentHighlight
    {
        private static readonly int _colorProperty = Shader.PropertyToID("_Color");

        private readonly SpriteRenderer _spriteRenderer;
        private readonly Color _normalColor;
        private readonly MaterialPropertyBlock _materialPropertyBlock;

        /// <summary>
        /// Creates a highlight helper for a sprite renderer.
        /// </summary>
        /// <param name="spriteRenderer">Target sprite renderer.</param>
        /// <param name="normalColor">Base color used when highlight is removed.</param>
        public ComponentHighlight(SpriteRenderer spriteRenderer, Color normalColor)
        {
            _spriteRenderer = spriteRenderer;
            _normalColor = normalColor;
            _materialPropertyBlock = new();
        }

        /// <summary>
        /// Applies a highlight color to the component sprite.
        /// </summary>
        /// <param name="highlightColor">Highlight color to apply.</param>
        public void ApplyHighlight(Color highlightColor)
        {
            if (_spriteRenderer is null || _materialPropertyBlock is null)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_colorProperty, highlightColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        /// <summary>
        /// Restores the sprite to its normal color.
        /// </summary>
        public void RemoveHighlight()
        {
            if (_spriteRenderer is null || _materialPropertyBlock is null)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_colorProperty, _normalColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        /// <summary>
        /// Updates highlight state based on hover and selection flags.
        /// </summary>
        /// <param name="isHovered">Whether the component is hovered.</param>
        /// <param name="isSelected">Whether the component is selected.</param>
        /// <param name="hoverColor">Color used for hover state.</param>
        /// <param name="selectedColor">Color used for selected state.</param>
        public void UpdateState(bool isHovered, bool isSelected, Color hoverColor, Color selectedColor)
        {
            if (_spriteRenderer is null)
            {
                return;
            }

            if (isSelected)
            {
                ApplyHighlight(selectedColor);
            }
            else if (isHovered)
            {
                ApplyHighlight(hoverColor);
            }
            else
            {
                RemoveHighlight();
            }
        }
    }
}
