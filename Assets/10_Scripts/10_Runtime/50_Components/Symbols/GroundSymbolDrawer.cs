using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class GroundSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 60, 32, 38, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 38, 48, 38, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 22, 30, 42, 30, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 27, 22, 37, 22, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
