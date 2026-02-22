using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback inductor symbol.
    /// </summary>
    internal sealed class InductorSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the inductor symbol into the texture.
        /// </summary>
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
