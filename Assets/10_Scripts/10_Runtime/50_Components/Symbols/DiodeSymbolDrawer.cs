using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class DiodeSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawHorizontalLeads(texture, 32, 16, 44, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 20, 16, 44, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 20, 38, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 44, 38, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 38, 32, 44, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 44, 18, 44, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
