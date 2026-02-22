using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws a generic fallback symbol for unknown component kinds.
    /// </summary>
    internal sealed class DefaultSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the default symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 16, 48, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 48, 16, 48, 48, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 48, 48, 16, 48, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 48, 16, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 16, 48, 48, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 48, 16, 16, 48, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
