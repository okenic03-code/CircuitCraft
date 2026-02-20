using System.Collections.Generic;
using CircuitCraft.Data;
using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Provides cached runtime-generated sprites for component symbols and visual effects.
    /// </summary>
    internal static class ComponentSymbolGenerator
    {
        internal const float PinDotRadius = 0.18f;

        private const int PinDotTextureSize = 32;
        private static readonly int _ledGlowTextureSize = 64;
        private static readonly int _heatGlowTextureSize = 64;
        private const int FallbackSymbolTextureSize = 64;
        private const int FallbackLineThickness = 2;

        private static Sprite _pinDotSprite;
        private static Sprite _ledGlowSprite;
        private static Sprite _heatGlowSprite;
        private static readonly Dictionary<ComponentKind, Sprite> _fallbackSprites = new Dictionary<ComponentKind, Sprite>();

        internal static Sprite GetOrCreateFallbackSprite(ComponentKind kind)
        {
            if (_fallbackSprites.TryGetValue(kind, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = new Texture2D(FallbackSymbolTextureSize, FallbackSymbolTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < FallbackSymbolTextureSize; y++)
            {
                for (int x = 0; x < FallbackSymbolTextureSize; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            DrawComponentSymbol(texture, kind, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, FallbackSymbolTextureSize, FallbackSymbolTextureSize),
                new Vector2(0.5f, 0.5f),
                FallbackSymbolTextureSize
            );

            sprite.name = $"{kind}_FallbackSprite";
            _fallbackSprites[kind] = sprite;
            return sprite;
        }

        internal static Sprite GetHeatGlowSprite()
        {
            if (_heatGlowSprite != null)
            {
                return _heatGlowSprite;
            }

            var texture = new Texture2D(_heatGlowTextureSize, _heatGlowTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2(_heatGlowTextureSize * 0.5f, _heatGlowTextureSize * 0.5f);
            float radius = _heatGlowTextureSize * 0.5f;

            for (int y = 0; y < _heatGlowTextureSize; y++)
            {
                for (int x = 0; x < _heatGlowTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(dist / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            _heatGlowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, _heatGlowTextureSize, _heatGlowTextureSize),
                new Vector2(0.5f, 0.5f),
                _heatGlowTextureSize
            );

            return _heatGlowSprite;
        }

        internal static Sprite GetLedGlowSprite()
        {
            if (_ledGlowSprite != null)
            {
                return _ledGlowSprite;
            }

            var texture = new Texture2D(_ledGlowTextureSize, _ledGlowTextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2(_ledGlowTextureSize * 0.5f, _ledGlowTextureSize * 0.5f);
            float radius = _ledGlowTextureSize * 0.5f;

            for (int y = 0; y < _ledGlowTextureSize; y++)
            {
                for (int x = 0; x < _ledGlowTextureSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(dist / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            _ledGlowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, _ledGlowTextureSize, _ledGlowTextureSize),
                new Vector2(0.5f, 0.5f),
                _ledGlowTextureSize
            );

            return _ledGlowSprite;
        }

        internal static Sprite GetPinDotSprite()
        {
            if (_pinDotSprite != null)
            {
                return _pinDotSprite;
            }

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

        private static void DrawComponentSymbol(Texture2D texture, ComponentKind kind, Color color)
        {
            switch (kind)
            {
                case ComponentKind.Resistor:
                    DrawResistorSymbol(texture, color);
                    break;

                case ComponentKind.Capacitor:
                    DrawCapacitorSymbol(texture, color);
                    break;

                case ComponentKind.Inductor:
                    DrawInductorSymbol(texture, color);
                    break;

                case ComponentKind.Diode:
                    DrawDiodeSymbol(texture, color);
                    break;

                case ComponentKind.LED:
                    DrawLedSymbol(texture, color);
                    break;

                case ComponentKind.BJT:
                    DrawBjtSymbol(texture, color);
                    break;

                case ComponentKind.MOSFET:
                    DrawMosfetSymbol(texture, color);
                    break;

                case ComponentKind.VoltageSource:
                    DrawVoltageSourceSymbol(texture, color);
                    break;

                case ComponentKind.CurrentSource:
                    DrawCurrentSourceSymbol(texture, color);
                    break;

                case ComponentKind.Ground:
                    DrawGroundSymbol(texture, color);
                    break;

                case ComponentKind.Probe:
                    DrawProbeSymbol(texture, color);
                    break;

                case ComponentKind.ZenerDiode:
                    DrawZenerDiodeSymbol(texture, color);
                    break;

                default:
                    DrawLineThick(texture, 16, 16, 48, 16, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 16, 48, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 48, 16, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 16, 48, 16, 16, FallbackLineThickness, color);
                    DrawLineThick(texture, 16, 16, 48, 48, FallbackLineThickness, color);
                    DrawLineThick(texture, 48, 16, 16, 48, FallbackLineThickness, color);
                    break;
            }
        }

        private static void DrawResistorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);

            int[] xPoints = { 16, 20, 24, 28, 32, 36, 40, 44, 48 };
            int[] yPoints = { 32, 22, 42, 22, 42, 22, 42, 22, 32 };

            for (int i = 0; i < xPoints.Length - 1; i++)
            {
                DrawLineThick(texture, xPoints[i], yPoints[i], xPoints[i + 1], yPoints[i + 1], FallbackLineThickness, color);
            }
        }

        private static void DrawVoltageSourceSymbol(Texture2D texture, Color color)
        {
            DrawVerticalLeads(texture, 32, 16, 48, color);
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 38, 32, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 28, 42, 36, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 28, 22, 36, 22, FallbackLineThickness, color);
        }

        private static void DrawCurrentSourceSymbol(Texture2D texture, Color color)
        {
            DrawVerticalLeads(texture, 32, 16, 48, color);
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawArrow(texture, 32, 22, 32, 42, color);
        }

        private static void DrawGroundSymbol(Texture2D texture, Color color)
        {
            DrawLineThick(texture, 32, 60, 32, 38, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 38, 48, 38, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 30, 42, 30, FallbackLineThickness, color);
            DrawLineThick(texture, 27, 22, 37, 22, FallbackLineThickness, color);
        }

        private static void DrawCapacitorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 26, 38, color);
            DrawLineThick(texture, 26, 18, 26, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 38, 18, 38, 46, FallbackLineThickness, color);
        }

        private static void DrawInductorSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 48, color);

            for (int i = 0; i < 4; i++)
            {
                int centerX = 20 + (i * 8);
                DrawArc(texture, centerX, 32, 4, 180f, 0f, FallbackLineThickness, color);
            }
        }

        private static void DrawDiodeSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 44, color);
            DrawLineThick(texture, 16, 20, 16, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 20, 38, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 44, 38, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 38, 32, 44, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 44, 18, 44, 46, FallbackLineThickness, color);
        }

        private static void DrawLedSymbol(Texture2D texture, Color color)
        {
            DrawDiodeSymbol(texture, color);
            DrawArrow(texture, 44, 38, 54, 48, color);
            DrawArrow(texture, 40, 32, 50, 42, color);
        }

        private static void DrawZenerDiodeSymbol(Texture2D texture, Color color)
        {
            DrawHorizontalLeads(texture, 32, 16, 42, color);
            DrawLineThick(texture, 16, 20, 16, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 20, 36, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 16, 44, 36, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 36, 32, 42, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 20, 42, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 44, 48, 47, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 20, 48, 17, FallbackLineThickness, color);
        }

        private static void DrawBjtSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);

            DrawLineThick(texture, 4, 32, 18, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 24, 22, 24, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 18, 32, 24, 32, FallbackLineThickness, color);

            DrawLineThick(texture, 24, 38, 42, 46, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 46, 58, 56, FallbackLineThickness, color);

            DrawLineThick(texture, 24, 26, 42, 18, FallbackLineThickness, color);
            DrawLineThick(texture, 42, 18, 58, 8, FallbackLineThickness, color);
            DrawArrow(texture, 34, 24, 42, 16, color);
        }

        private static void DrawMosfetSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);

            DrawLineThick(texture, 4, 32, 20, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 22, 22, 42, FallbackLineThickness, color);

            DrawLineThick(texture, 34, 20, 34, 44, FallbackLineThickness, color);
            DrawLineThick(texture, 34, 40, 46, 50, FallbackLineThickness, color);
            DrawLineThick(texture, 46, 50, 58, 58, FallbackLineThickness, color);
            DrawLineThick(texture, 34, 24, 46, 14, FallbackLineThickness, color);
            DrawLineThick(texture, 46, 14, 58, 6, FallbackLineThickness, color);
            DrawArrow(texture, 40, 20, 48, 12, color);
        }

        private static void DrawProbeSymbol(Texture2D texture, Color color)
        {
            DrawCircleThick(texture, 32, 32, 16, FallbackLineThickness, color);
            DrawLineThick(texture, 22, 32, 42, 32, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 22, 32, 42, FallbackLineThickness, color);
            DrawLineThick(texture, 32, 4, 32, 16, FallbackLineThickness, color);
            DrawCircleThick(texture, 32, 32, 2, 1, color);
        }

        private static void DrawHorizontalLeads(Texture2D texture, int y, int leftInner, int rightInner, Color color)
        {
            DrawLineThick(texture, 4, y, leftInner, y, FallbackLineThickness, color);
            DrawLineThick(texture, rightInner, y, 60, y, FallbackLineThickness, color);
        }

        private static void DrawVerticalLeads(Texture2D texture, int x, int bottomInner, int topInner, Color color)
        {
            DrawLineThick(texture, x, 4, x, bottomInner, FallbackLineThickness, color);
            DrawLineThick(texture, x, topInner, x, 60, FallbackLineThickness, color);
        }

        private static void DrawArrow(Texture2D texture, int startX, int startY, int endX, int endY, Color color)
        {
            DrawLineThick(texture, startX, startY, endX, endY, FallbackLineThickness, color);

            Vector2 direction = new Vector2(endX - startX, endY - startY);
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 back = -direction * 5f;
            Vector2 endPoint = new Vector2(endX, endY);

            Vector2 left = endPoint + back + (perpendicular * 3f);
            Vector2 right = endPoint + back - (perpendicular * 3f);

            DrawLineThick(texture, endX, endY, Mathf.RoundToInt(left.x), Mathf.RoundToInt(left.y), FallbackLineThickness, color);
            DrawLineThick(texture, endX, endY, Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y), FallbackLineThickness, color);
        }

        private static void DrawArc(Texture2D texture, int centerX, int centerY, int radius, float startDegrees, float endDegrees, int thickness, Color color)
        {
            int segments = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(endDegrees - startDegrees) / 6f));
            float previousAngle = startDegrees * Mathf.Deg2Rad;
            int prevX = centerX + Mathf.RoundToInt(Mathf.Cos(previousAngle) * radius);
            int prevY = centerY + Mathf.RoundToInt(Mathf.Sin(previousAngle) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                int nextX = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
                int nextY = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * radius);

                DrawLineThick(texture, prevX, prevY, nextX, nextY, thickness, color);

                prevX = nextX;
                prevY = nextY;
            }
        }

        private static void DrawLineThick(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            if (thickness <= 1)
            {
                DrawLine(texture, x0, y0, x1, y1, color);
                return;
            }

            int minOffset = -(thickness / 2);
            int maxOffset = minOffset + thickness - 1;

            for (int offsetY = minOffset; offsetY <= maxOffset; offsetY++)
            {
                for (int offsetX = minOffset; offsetX <= maxOffset; offsetX++)
                {
                    DrawLine(texture, x0 + offsetX, y0 + offsetY, x1 + offsetX, y1 + offsetY, color);
                }
            }
        }

        private static void DrawCircleThick(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            if (thickness <= 1)
            {
                DrawCircle(texture, centerX, centerY, radius, color);
                return;
            }

            int minOffset = -(thickness / 2);
            int maxOffset = minOffset + thickness - 1;

            for (int offset = minOffset; offset <= maxOffset; offset++)
            {
                int currentRadius = radius + offset;
                if (currentRadius > 0)
                {
                    DrawCircle(texture, centerX, centerY, currentRadius, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int x = radius;
            int y = 0;
            int err = 1 - radius;

            while (x >= y)
            {
                SetPixelSafe(texture, centerX + x, centerY + y, color);
                SetPixelSafe(texture, centerX + y, centerY + x, color);
                SetPixelSafe(texture, centerX - y, centerY + x, color);
                SetPixelSafe(texture, centerX - x, centerY + y, color);
                SetPixelSafe(texture, centerX - x, centerY - y, color);
                SetPixelSafe(texture, centerX - y, centerY - x, color);
                SetPixelSafe(texture, centerX + y, centerY - x, color);
                SetPixelSafe(texture, centerX + x, centerY - y, color);

                y++;
                if (err < 0)
                {
                    err += (2 * y) + 1;
                }
                else
                {
                    x--;
                    err += (2 * (y - x)) + 1;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
