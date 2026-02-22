using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class MosfetSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 4, 32, 20, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 22, 22, 22, 42, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 34, 20, 34, 44, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 34, 40, 46, 50, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 46, 50, 58, 58, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 34, 24, 46, 14, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 46, 14, 58, 6, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawArrow(texture, 40, 20, 48, 12, color);
        }
    }
}
