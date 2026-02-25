using UnityEngine;

namespace CircuitCraft.Components
{
    /// <summary>
    /// Defines symbol drawing behavior for fallback component textures.
    /// </summary>
    internal interface ISymbolDrawer
    {
        /// <summary>
        /// Draws a symbol into the provided texture.
        /// </summary>
        /// <param name="texture">Target texture.</param>
        /// <param name="color">Drawing color.</param>
        void Draw(Texture2D texture, Color color);
    }
}
