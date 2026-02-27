using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback voltage source symbol.
    /// </summary>
    internal sealed class VoltageSourceSymbolDrawer : ISymbolDrawer
    {
        /// <summary>
        /// Draws the voltage source symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            // Logical pin rows are y=16 (bottom) and y=48 (top) when pivot=0.25, PPU=32.
            // Keep lead endpoints on those exact rows so pin center == lead endpoint.
            SymbolDrawingUtilities.DrawVerticalLeads(texture, 32, 16, 48, color);
            SymbolDrawingUtilities.DrawCircleThick(texture, 32, 32, 16, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 32, 38, 32, 46, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 28, 42, 36, 42, SymbolDrawingUtilities.FallbackLineThickness, color);
            SymbolDrawingUtilities.DrawLineThick(texture, 28, 22, 36, 22, SymbolDrawingUtilities.FallbackLineThickness, color);
        }
    }
}
