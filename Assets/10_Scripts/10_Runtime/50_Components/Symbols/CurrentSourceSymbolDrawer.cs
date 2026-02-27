using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback current source symbol.
    /// </summary>
    internal sealed class CurrentSourceSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the current source symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            // Logical pin rows are y=16 (bottom) and y=48 (top) when pivot=0.25, PPU=32.
            // Keep lead endpoints on those exact rows so pin center == lead endpoint.
            SymbolDrawingUtilities.DrawVerticalLeads(texture, 32, 16, 48, color);
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawArrow(texture, 32, 22, 32, 42, color);
        }
    }
}
