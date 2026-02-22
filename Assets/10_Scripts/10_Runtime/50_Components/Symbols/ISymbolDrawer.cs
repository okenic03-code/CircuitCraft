using UnityEngine;

namespace CircuitCraft.Components
{
    internal interface ISymbolDrawer
    {
        void Draw(Texture2D texture, Color color);
    }
}
