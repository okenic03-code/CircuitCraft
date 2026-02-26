using System.Collections.Generic;
using CircuitCraft.Data;
using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Generates fallback component symbols and shared helper sprites.
    /// </summary>
    internal static class ComponentSymbolGenerator
    {
        internal const float PinDotRadius = 0.12f;
        private const int PinDotTextureSize = 32;
        private const int GlowTextureSize = 64;
        private const int FallbackSymbolTextureSize = 64;
        private static Sprite _pinDotSprite;
        private static Sprite _ledGlowSprite;
        private static Sprite _heatGlowSprite;
        private static readonly Dictionary<ComponentKind, ISymbolDrawer> _drawers = new();
        private static readonly Dictionary<ComponentKind, Sprite> _fallbackSprites = new();

        /// <summary>
        /// Returns an existing fallback sprite for a component kind or creates one.
        /// </summary>
        /// <param name="kind">Component kind to render.</param>
        /// <returns>Generated or cached fallback sprite.</returns>
        internal static Sprite GetOrCreateFallbackSprite(ComponentKind kind)
        {
            ResolveFallbackSpriteLayout(kind, out Vector2 pivot, out float pixelsPerUnit);
            if (_fallbackSprites.TryGetValue(kind, out Sprite cachedSprite)
                && cachedSprite != null
                && DoesSpriteMatchLayout(cachedSprite, pivot, pixelsPerUnit)
                && DoesSpriteMatchStyle(cachedSprite, kind))
            {
                return cachedSprite;
            }

            var texture = new Texture2D(FallbackSymbolTextureSize, FallbackSymbolTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture);
            DrawComponentSymbol(texture, kind, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, FallbackSymbolTextureSize, FallbackSymbolTextureSize),
                pivot,
                pixelsPerUnit
            );
            sprite.name = $"{kind}_FallbackSprite";
            _fallbackSprites[kind] = sprite;
            return sprite;
        }
        /// <summary>
        /// Returns the shared resistor heat glow sprite.
        /// </summary>
        internal static Sprite GetHeatGlowSprite()
        {
            if (_heatGlowSprite != null) { return _heatGlowSprite; }
            _heatGlowSprite = CreateGlowSprite();
            return _heatGlowSprite;
        }

        /// <summary>
        /// Returns the shared LED glow sprite.
        /// </summary>
        internal static Sprite GetLedGlowSprite()
        {
            if (_ledGlowSprite != null) { return _ledGlowSprite; }
            _ledGlowSprite = CreateGlowSprite();
            return _ledGlowSprite;
        }

        /// <summary>
        /// Returns the shared pin-dot sprite.
        /// </summary>
        internal static Sprite GetPinDotSprite()
        {
            if (_pinDotSprite != null) { return _pinDotSprite; }
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
            _pinDotSprite = Sprite.Create(texture, new Rect(0f, 0f, PinDotTextureSize, PinDotTextureSize), new Vector2(0.5f, 0.5f), PinDotTextureSize);
            return _pinDotSprite;
        }
        private static void DrawComponentSymbol(Texture2D texture, ComponentKind kind, Color color)
        {
            GetDrawer(kind).Draw(texture, color);
        }
        private static ISymbolDrawer GetDrawer(ComponentKind kind)
        {
            if (_drawers.TryGetValue(kind, out ISymbolDrawer drawer)) { return drawer; }
            drawer = kind switch {
                ComponentKind.Resistor => new ResistorSymbolDrawer(), ComponentKind.Capacitor => new CapacitorSymbolDrawer(),
                ComponentKind.Inductor => new InductorSymbolDrawer(), ComponentKind.Diode => new DiodeSymbolDrawer(),
                ComponentKind.LED => new LedSymbolDrawer(), ComponentKind.BJT => new BjtSymbolDrawer(), ComponentKind.MOSFET => new MosfetSymbolDrawer(),
                ComponentKind.VoltageSource => new VoltageSourceSymbolDrawer(), ComponentKind.CurrentSource => new CurrentSourceSymbolDrawer(),
                ComponentKind.Ground => new GroundSymbolDrawer(), ComponentKind.Probe => new ProbeSymbolDrawer(),
                ComponentKind.ZenerDiode => new ZenerDiodeSymbolDrawer(), _ => new DefaultSymbolDrawer()
            };
            _drawers[kind] = drawer;
            return drawer;
        }
        private static Sprite CreateGlowSprite()
        {
            var texture = new Texture2D(GlowTextureSize, GlowTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2(GlowTextureSize * 0.5f, GlowTextureSize * 0.5f);
            float radius = GlowTextureSize * 0.5f;
            for (int y = 0; y < GlowTextureSize; y++)
            {
                for (int x = 0; x < GlowTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(dist / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, GlowTextureSize, GlowTextureSize), new Vector2(0.5f, 0.5f), GlowTextureSize);
        }
        private static void ClearTexture(Texture2D texture)
        {
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        private static void ResolveFallbackSpriteLayout(ComponentKind kind, out Vector2 pivot, out float pixelsPerUnit)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                case ComponentKind.Capacitor:
                case ComponentKind.Inductor:
                case ComponentKind.Diode:
                case ComponentKind.LED:
                case ComponentKind.ZenerDiode:
                    // Horizontal two-pin symbols use local pins (0,0) and (1,0).
                    // Align pins to OUTER lead endpoints (x=4 and x=60 in a 64px texture):
                    //  pin(0,0) -> x=4, pin(1,0) -> x=60
                    // so terminal-to-terminal spacing is exactly 1 grid cell.
                    pivot = new Vector2(0.0625f, 0.5f); // 4 / 64
                    pixelsPerUnit = 56f;                // (60 - 4)
                    return;
                case ComponentKind.VoltageSource:
                case ComponentKind.CurrentSource:
                    // Two-pin vertical sources use local pins (0,0) and (0,1).
                    // Align pins to the OUTER lead endpoints (y=4 and y=60 in a 64px texture):
                    //  pin(0,0) -> y=4, pin(0,1) -> y=60
                    // so terminal-to-terminal spacing is exactly 1 grid cell.
                    pivot = new Vector2(0.5f, 0.0625f); // 4 / 64
                    pixelsPerUnit = 56f;                // (60 - 4)
                    return;
                case ComponentKind.Ground:
                    // Ground has a single pin at local (0,0):
                    // place pivot on the top lead so pin and symbol connection coincide.
                    pivot = new Vector2(0.5f, 0.9375f);
                    pixelsPerUnit = FallbackSymbolTextureSize;
                    return;
                case ComponentKind.Probe:
                    // Probe has a single pin at local (0,0):
                    // place pivot on the probe lead tip so the wiring pin aligns to the terminal connection point.
                    pivot = new Vector2(0.5f, 0.0625f);
                    pixelsPerUnit = FallbackSymbolTextureSize;
                    return;
                default:
                    pivot = new Vector2(0.5f, 0.5f);
                    pixelsPerUnit = FallbackSymbolTextureSize;
                    return;
            }
        }

        private static bool DoesSpriteMatchLayout(Sprite sprite, Vector2 expectedPivotNormalized, float expectedPixelsPerUnit)
        {
            if (sprite == null)
            {
                return false;
            }

            if (sprite.texture == null)
            {
                return false;
            }

            if (Mathf.Abs(sprite.pixelsPerUnit - expectedPixelsPerUnit) > 0.001f)
            {
                return false;
            }

            Rect rect = sprite.rect;
            if (rect.width <= Mathf.Epsilon || rect.height <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 actualPivotNormalized = new Vector2(sprite.pivot.x / rect.width, sprite.pivot.y / rect.height);
            return Vector2.Distance(actualPivotNormalized, expectedPivotNormalized) <= 0.001f;
        }

        private static bool DoesSpriteMatchStyle(Sprite sprite, ComponentKind kind)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            // Voltage/Current source terminals should align with logical pin rows (y=16, y=48),
            // therefore top outer lead pixels near y=56 on center x should be present.
            if (kind == ComponentKind.VoltageSource || kind == ComponentKind.CurrentSource)
            {
                if (sprite.texture.width <= 32 || sprite.texture.height <= 56)
                {
                    return false;
                }

                return sprite.texture.GetPixel(32, 56).a > 0.01f;
            }

            return true;
        }
    }
}
