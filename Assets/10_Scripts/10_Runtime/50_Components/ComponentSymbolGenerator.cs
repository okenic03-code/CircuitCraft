using System.Collections.Generic;
using CircuitCraft.Data;
using UnityEngine;

namespace CircuitCraft.Components
{
    internal static class ComponentSymbolGenerator
    {
        internal const float PinDotRadius = 0.18f;
        private const int PinDotTextureSize = 32;
        private const int GlowTextureSize = 64;
        private const int FallbackSymbolTextureSize = 64;
        private static Sprite _pinDotSprite;
        private static Sprite _ledGlowSprite;
        private static Sprite _heatGlowSprite;
        private static readonly Dictionary<ComponentKind, ISymbolDrawer> _drawers = new Dictionary<ComponentKind, ISymbolDrawer>();
        private static readonly Dictionary<ComponentKind, Sprite> _fallbackSprites = new Dictionary<ComponentKind, Sprite>();
        internal static Sprite GetOrCreateFallbackSprite(ComponentKind kind)
        {
            if (_fallbackSprites.TryGetValue(kind, out Sprite cachedSprite) && cachedSprite != null) { return cachedSprite; }
            var texture = new Texture2D(FallbackSymbolTextureSize, FallbackSymbolTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture);
            DrawComponentSymbol(texture, kind, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, FallbackSymbolTextureSize, FallbackSymbolTextureSize), new Vector2(0.5f, 0.5f), FallbackSymbolTextureSize);
            sprite.name = $"{kind}_FallbackSprite";
            _fallbackSprites[kind] = sprite;
            return sprite;
        }
        internal static Sprite GetHeatGlowSprite()
        {
            if (_heatGlowSprite != null) { return _heatGlowSprite; }
            _heatGlowSprite = CreateGlowSprite();
            return _heatGlowSprite;
        }
        internal static Sprite GetLedGlowSprite()
        {
            if (_ledGlowSprite != null) { return _ledGlowSprite; }
            _ledGlowSprite = CreateGlowSprite();
            return _ledGlowSprite;
        }
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
    }
}
