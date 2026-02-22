using System.Collections.Generic;
using CircuitCraft.Data;
using CircuitCraft.Utils;
using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class ComponentPinDots
    {
        private static readonly Color PinDotColor = new Color(0f, 0.83f, 1f, 0.6f);
        private static GridSettings _cachedGridSettings;

        private readonly Transform _parent;
        private readonly SpriteRenderer _parentSprite;
        private readonly List<GameObject> _pinDots = new List<GameObject>();

        public ComponentPinDots(Transform parent, SpriteRenderer parentSprite)
        {
            _parent = parent;
            _parentSprite = parentSprite;
        }

        public void CreatePinDots(ComponentDefinition definition, float cellSize)
        {
            if (definition?.Pins == null || definition.Pins.Length == 0)
            {
                return;
            }

            float resolvedCellSize = cellSize > Mathf.Epsilon ? cellSize : ResolveGridCellSize();
            Sprite pinDotSprite = ComponentSymbolGenerator.GetPinDotSprite();
            int dotSortingOrder = _parentSprite != null ? _parentSprite.sortingOrder + 1 : 1;
            int sortingLayerId = _parentSprite != null ? _parentSprite.sortingLayerID : 0;

            for (int i = 0; i < definition.Pins.Length; i++)
            {
                PinDefinition pinDef = definition.Pins[i];
                if (pinDef == null)
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
                dotRenderer.color = PinDotColor;
                dotRenderer.sortingLayerID = sortingLayerId;
                dotRenderer.sortingOrder = dotSortingOrder;

                _pinDots.Add(pinDot);
            }
        }

        public void ClearPinDots()
        {
            for (int i = 0; i < _pinDots.Count; i++)
            {
                if (_pinDots[i] != null)
                {
                    Object.Destroy(_pinDots[i]);
                }
            }

            _pinDots.Clear();
        }

        public void Cleanup()
        {
            ClearPinDots();
        }

        private static float ResolveGridCellSize()
        {
            if (_cachedGridSettings != null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            if (_cachedGridSettings == null)
            {
                _cachedGridSettings = Object.FindFirstObjectByType<GridSettings>();
            }

            if (_cachedGridSettings != null && _cachedGridSettings.CellSize > Mathf.Epsilon)
            {
                return _cachedGridSettings.CellSize;
            }

            return 1f;
        }
    }
}
