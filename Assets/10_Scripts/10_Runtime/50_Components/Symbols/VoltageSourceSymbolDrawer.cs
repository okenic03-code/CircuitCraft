using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class VoltageSourceSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawVerticalLeads(texture, 32, 16, 48, color);
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 38, 32, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 28, 42, 36, 42, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 28, 22, 36, 22, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
