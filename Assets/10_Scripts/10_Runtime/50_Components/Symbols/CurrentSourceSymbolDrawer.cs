using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class CurrentSourceSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawVerticalLeads(texture, 32, 16, 48, color);
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawArrow(texture, 32, 22, 32, 42, color);
        }
    }
}
