using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback ground symbol.
    /// </summary>
    internal sealed class GroundSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the ground symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 60, 32, 38, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 16, 38, 48, 38, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 22, 30, 42, 30, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 27, 22, 37, 22, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
