using UnityEngine;

namespace CircuitCraft.Components
{
    internal sealed class LedSymbolDrawer : ISymbolDrawer
    {
        private readonly ISymbolDrawer _diodeSymbolDrawer;

        internal LedSymbolDrawer()
            : this(new DiodeSymbolDrawer())
        {
        }

        internal LedSymbolDrawer(ISymbolDrawer diodeSymbolDrawer)
        {
            _diodeSymbolDrawer = diodeSymbolDrawer;
        }

        public void Draw(Texture2D texture, Color color)
        {
            _diodeSymbolDrawer.Draw(texture, color);
            SymbolDrawingUtilities.DrawArrow(texture, 44, 38, 54, 48, color);
            SymbolDrawingUtilities.DrawArrow(texture, 40, 32, 50, 42, color);
        }
    }
}
