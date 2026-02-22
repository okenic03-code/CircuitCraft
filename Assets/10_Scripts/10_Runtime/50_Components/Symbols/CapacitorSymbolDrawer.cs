using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class CapacitorSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawHorizontalLeads(texture, 32, 26, 38, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 26, 18, 26, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 38, 18, 38, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
