using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class BjtSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 4, 32, 18, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 24, 22, 24, 42, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 18, 32, 24, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 24, 38, 42, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 42, 46, 58, 56, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 24, 26, 42, 18, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 42, 18, 58, 8, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawArrow(texture, 34, 24, 42, 16, color);
        }
    }
}
