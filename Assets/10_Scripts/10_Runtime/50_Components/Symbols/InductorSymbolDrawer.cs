using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class InductorSymbolDrawer : ISymbolDrawer
    {
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawHorizontalLeads(texture, 32, 16, 48, color);

            for (int i = 0; i < 4; i++)
            {
                int centerX = 20 + (i * 8);
                SymbolDrawingUtilities.DrawArc(texture, centerX, 32, 4, 180f, 0f, SymbolDrawingUtilities.FallbackLineThickness, color);
            }
        }
    }
}
