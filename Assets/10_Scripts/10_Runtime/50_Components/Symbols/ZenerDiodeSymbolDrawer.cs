using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback Zener diode symbol.
    /// </summary>
    internal sealed class ZenerDiodeSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the Zener diode symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawHorizontalLeads(texture, 32, 16, 42, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 20, 16, 44, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 20, 36, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 44, 36, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 36, 32, 42, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 42, 20, 42, 44, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 42, 44, 48, 47, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 42, 20, 48, 17, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
