using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback probe symbol.
    /// </summary>
    internal sealed class ProbeSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the probe symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 22, 32, 42, 32, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 22, 32, 42, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 4, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 2, 1, color);
        }
    }
}
