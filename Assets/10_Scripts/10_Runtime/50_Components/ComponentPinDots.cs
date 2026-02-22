using System.Collections.Generic;
using CircuitCraft.Data;
using CircuitCraft.Utils;
using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Creates and manages pin marker dots for component visuals.
    /// </summary>
    internal sealed class ComponentPinDots
    {
        private static readonly Color _pinDotColor = new Color(0f, 0.83f, 1f, 0.6f);
        private static GridSettings _cachedGridSettings;

        private readonly Transform _parent;
        private readonly SpriteRenderer _parentSprite;
        private readonly List<GameObject> _pinDots = new();

        /// <summary>
        /// Creates a pin dot manager.
        /// </summary>
        /// <param name="parent">Transform used as dot parent.</param>
        /// <param name="parentSprite">Sprite renderer used for sorting context.</param>
        public ComponentPinDots(Transform parent, SpriteRenderer parentSprite)
        {
            _parent = parent;
            _parentSprite = parentSprite;
        }

        /// <summary>
        /// Creates pin dots from the component definition.
        /// </summary>
        /// <param name="definition">Component definition containing pin metadata.</param>
        /// <param name="cellSize">Grid cell size for coordinate conversion.</param>
        public void CreatePinDots(ComponentDefinition definition, float cellSize)
        {
            if (definition?.Pins is null || definition.Pins.Length == 0)
            {
                return;
            }

            float resolvedCellSize = cellSize > Mathf.Epsilon ? cellSize : ResolveGridCellSize();
            Sprite pinDotSprite = ComponentSymbolGenerator.GetPinDotSprite();
            int dotSortingOrder = _parentSprite is not null ? _parentSprite.sortingOrder + 1 : 1;
            int sortingLayerId = _parentSprite is not null ? _parentSprite.sortingLayerID : 0;

            for (int i = 0; i < definition.Pins.Length; i++)
            {
                PinDefinition pinDef = definition.Pins[i];
                if (pinDef is null)
                {
                    continue;
                }

                GameObject pinDot = new GameObject($"PinDot_{pinDef.PinName}");
                pinDot.transform.SetParent(_parent, false);

                Vector2 localGridOffset = pinDef.LocalPosition;
                pinDot.transform.localPosition = new Vector3(
                    localGridOffset.x * resolvedCellSize,
                    localGridOffset.y * resolvedCellSize,
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

        /// <summary>
        /// Destroys all spawned pin dot objects.
        /// </summary>
        public void ClearPinDots()
        {
            for (int i = 0; i < _pinDots.Count; i++)
            {
                if (_pinDots[i] is not null)
                {
                    Object.Destroy(_pinDots[i]);
                }
            }

            _pinDots.Clear();
        }

        /// <summary>
        /// Clears pin dot resources.
        /// </summary>
        public void Cleanup()
        {
            ClearPinDots();
        }

        private static float ResolveGridCellSize()
        {
            if (_cachedGridSettings is not null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            if (_cachedGridSettings is null)
            {
                _cachedGridSettings = Object.FindFirstObjectByType<GridSettings>();
            }

            if (_cachedGridSettings is not null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            return 1f;
        }
    }
}
