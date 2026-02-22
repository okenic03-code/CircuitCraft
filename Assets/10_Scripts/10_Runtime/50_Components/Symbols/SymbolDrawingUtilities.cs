using UnityEngine;

namespace CircuitCraft.Components
{
    internal static class SymbolDrawingUtilities
    {
        internal const int FallbackLineThickness = 2;

        internal static void DrawHorizontalLeads(Texture2D texture, int y, int leftInner, int rightInner, Color color)
        {
            DrawLineThick(texture, 4, y, leftInner, y, FallbackLineThickness, color);
            DrawLineThick(texture, rightInner, y, 60, y, FallbackLineThickness, color);
        }

        internal static void DrawVerticalLeads(Texture2D texture, int x, int bottomInner, int topInner, Color color)
        {
            DrawLineThick(texture, x, 4, x, bottomInner, FallbackLineThickness, color);
            DrawLineThick(texture, x, topInner, x, 60, FallbackLineThickness, color);
        }

        internal static void DrawArrow(Texture2D texture, int startX, int startY, int endX, int endY, Color color)
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

        internal static void DrawArc(Texture2D texture, int centerX, int centerY, int radius, float startDegrees, float endDegrees, int thickness, Color color)
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

        internal static void DrawLineThick(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
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

        internal static void DrawCircleThick(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
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

        internal static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
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

        internal static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
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

        internal static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
