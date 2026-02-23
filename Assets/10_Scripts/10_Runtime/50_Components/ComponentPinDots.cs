using System.Collections.Generic;
using CircuitCraft.Core;
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

        private readonly Transform _parent;
        private readonly SpriteRenderer _parentSprite;
        private readonly GridSettings _gridSettings;
        private readonly List<GameObject> _pinDots = new();

        /// <summary>
        /// Creates a pin dot manager.
        /// </summary>
        /// <param name="parent">Transform used as dot parent.</param>
        /// <param name="parentSprite">Sprite renderer used for sorting context.</param>
        /// <param name="gridSettings">Grid settings used for pin coordinate scaling.</param>
        public ComponentPinDots(Transform parent, SpriteRenderer parentSprite, GridSettings gridSettings)
        {
            _parent = parent;
            _parentSprite = parentSprite;
            _gridSettings = gridSettings;
        }

        /// <summary>
        /// Creates pin dots from the component definition.
        /// </summary>
        /// <param name="definition">Component definition containing pin metadata.</param>
        /// <param name="cellSize">Grid cell size for coordinate conversion.</param>
        public void CreatePinDots(ComponentDefinition definition, float cellSize)
        {
            if (definition == null)
            {
                return;
            }

            var pins = definition.Pins;
            if (pins is null || pins.Length == 0)
            {
                pins = GetStandardPins(definition.Kind);
            }
            if (pins is null || pins.Length == 0)
            {
                return;
            }

            float resolvedCellSize = cellSize > Mathf.Epsilon ? cellSize : ResolveGridCellSize();
            Sprite pinDotSprite = ComponentSymbolGenerator.GetPinDotSprite();
            int dotSortingOrder = _parentSprite != null ? _parentSprite.sortingOrder + 1 : 1;
            int sortingLayerId = _parentSprite != null ? _parentSprite.sortingLayerID : 0;

            for (int i = 0; i < pins.Length; i++)
            {
                PinDefinition pinDef = pins[i];
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

                // Add collider for raycast detection during wiring
                SphereCollider pinCollider = pinDot.AddComponent<SphereCollider>();
                pinCollider.radius = 0.5f;
                pinCollider.isTrigger = false;

                _pinDots.Add(pinDot);
            }
        }

        private static PinDefinition[] GetStandardPins(ComponentKind kind)
        {
            return kind switch
            {
                ComponentKind.BJT => StandardPinDefinitions.BJT,
                ComponentKind.MOSFET => StandardPinDefinitions.MOSFET,
                ComponentKind.Diode or ComponentKind.LED or ComponentKind.ZenerDiode => StandardPinDefinitions.Diode,
                ComponentKind.VoltageSource or ComponentKind.CurrentSource => StandardPinDefinitions.VerticalTwoPin,
                _ => StandardPinDefinitions.TwoPin
            };
        }

        /// <summary>
        /// Destroys all spawned pin dot objects.
        /// </summary>
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

        /// <summary>
        /// Clears pin dot resources.
        /// </summary>
        public void Cleanup()
        {
            ClearPinDots();
        }

        private float ResolveGridCellSize()
        {
            if (_gridSettings != null && _gridSettings.CellSize > Mathf.Epsilon)
            {
                return _gridSettings.CellSize;
            }

            return 1f;
        }
    }
}
