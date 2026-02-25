using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback resistor symbol.
    /// </summary>
    internal sealed class ResistorSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the resistor symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawHorizontalLeads(texture, 32, 16, 48, color);

            int[] xPoints = { 16, 20, 24, 28, 32, 36, 40, 44, 48 };
            int[] yPoints = { 32, 22, 42, 22, 42, 22, 42, 22, 32 };

            for (int i = 0; i < xPoints.Length - 1; i++)
            {
                SymbolDrawingUtilities.DrawLineThick(texture, xPoints[i], yPoints[i], xPoints[i + 1], yPoints[i + 1], SymbolDrawingUtilities.FallbackLineThickness, color);
            }
        }
    }
}
