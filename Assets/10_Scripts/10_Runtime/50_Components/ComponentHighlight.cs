using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class ComponentHighlight
    {
        private static readonly int _ColorProperty = Shader.PropertyToID("_Color");

        private readonly SpriteRenderer _spriteRenderer;
        private readonly Color _normalColor;
        private readonly MaterialPropertyBlock _materialPropertyBlock;

        public ComponentHighlight(SpriteRenderer spriteRenderer, Color normalColor)
        {
            _spriteRenderer = spriteRenderer;
            _normalColor = normalColor;
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        public void ApplyHighlight(Color highlightColor)
        {
            if (_spriteRenderer == null || _materialPropertyBlock == null)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_ColorProperty, highlightColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public void RemoveHighlight()
        {
            if (_spriteRenderer == null || _materialPropertyBlock == null)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_ColorProperty, _normalColor);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public void UpdateState(bool isHovered, bool isSelected, Color hoverColor, Color selectedColor)
        {
            if (_spriteRenderer == null)
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
