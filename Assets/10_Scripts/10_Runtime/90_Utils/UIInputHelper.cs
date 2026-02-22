using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.Utils
{
    /// <summary>
    /// Utility helpers for querying pointer interactions against runtime UI documents.
    /// </summary>
    public static class UIInputHelper
    {
        /// <summary>
        /// Checks whether the pointer is currently over any non-root UI element in any provided document.
        /// </summary>
        /// <param name="uiDocuments">The UI documents to test.</param>
        /// <returns>True if the pointer is over a visible UI element.</returns>
        public static bool IsPointerOverUI(UIDocument[] uiDocuments)
        {
            if (uiDocuments is null)
                return false;

            foreach (var doc in uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null)
                    continue;

                var panel = doc.rootVisualElement.panel;
                if (panel is null)
                    continue;

                Vector2 screenPos = Input.mousePosition;
                Vector2 panelPos = new(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);

                var picked = panel.Pick(panelPos);
                if (picked is not null
                    && picked != doc.rootVisualElement
                    && !(picked is TemplateContainer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the pointer is over interactive UI that is outside the main game view container.
        /// </summary>
        /// <param name="uiDocuments">The UI documents to test.</param>
        /// <returns>True if the pointer is over UI and not inside the game view area.</returns>
        public static bool IsPointerOverRealUI(UIDocument[] uiDocuments)
        {
            if (uiDocuments is null)
                return false;

            foreach (var doc in uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null)
                    continue;

                var panel = doc.rootVisualElement.panel;
                if (panel is null)
                    continue;

                Vector2 screenPos = Input.mousePosition;
                Vector2 panelPos = new(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);

                var picked = panel.Pick(panelPos);
                if (picked is null || picked == doc.rootVisualElement || picked is TemplateContainer)
                    continue;

                var gameView = doc.rootVisualElement.Q<VisualElement>("GameView");
                if (gameView is not null && IsChildOf(picked, gameView))
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsChildOf(VisualElement element, VisualElement parent)
        {
            if (element is null || parent is null)
                return false;

            var current = element;
            while (current is not null)
            {
                if (current == parent)
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
