using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Draws the fallback LED symbol.
    /// </summary>
    internal sealed class LedSymbolDrawer : ISymbolDrawer
    {
        private readonly ISymbolDrawer _diodeSymbolDrawer;

        /// <summary>
        /// Creates an LED drawer with the default diode drawer implementation.
        /// </summary>
        internal LedSymbolDrawer()
            : this(new DiodeSymbolDrawer())
        {
        }

        /// <summary>
        /// Creates an LED drawer with an injected diode drawer.
        /// </summary>
        /// <param name="diodeSymbolDrawer">Diode drawer used as the base symbol.</param>
        internal LedSymbolDrawer(ISymbolDrawer diodeSymbolDrawer)
        {
            _diodeSymbolDrawer = diodeSymbolDrawer;
        }

        /// <summary>
        /// Draws the LED symbol into the texture.
        /// </summary>
        public void Draw(Texture2D texture, Color color)
        {
            _diodeSymbolDrawer.Draw(texture, color);
            SymbolDrawingUtilities.DrawArrow(texture, 44, 38, 54, 48, color);
            SymbolDrawingUtilities.DrawArrow(texture, 40, 32, 50, 42, color);
        }
    }
}
