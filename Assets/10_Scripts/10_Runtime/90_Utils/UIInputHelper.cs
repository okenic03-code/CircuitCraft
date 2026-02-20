using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.Utils
{
    public static class UIInputHelper
    {
        public static bool IsPointerOverUI(UIDocument[] uiDocuments)
        {
            if (uiDocuments == null)
                return false;

            foreach (var doc in uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null)
                    continue;

                var panel = doc.rootVisualElement.panel;
                if (panel == null)
                    continue;

                Vector2 screenPos = Input.mousePosition;
                Vector2 panelPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);

                var picked = panel.Pick(panelPos);
                if (picked != null
                    && picked != doc.rootVisualElement
                    && !(picked is TemplateContainer))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPointerOverRealUI(UIDocument[] uiDocuments)
        {
            if (uiDocuments == null)
                return false;

            foreach (var doc in uiDocuments)
            {
                if (doc == null || doc.rootVisualElement == null)
                    continue;

                var panel = doc.rootVisualElement.panel;
                if (panel == null)
                    continue;

                Vector2 screenPos = Input.mousePosition;
                Vector2 panelPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                panelPos = RuntimePanelUtils.ScreenToPanel(panel, panelPos);

                var picked = panel.Pick(panelPos);
                if (picked == null || picked == doc.rootVisualElement || picked is TemplateContainer)
                    continue;

                var gameView = doc.rootVisualElement.Q<VisualElement>("GameView");
                if (gameView != null && IsChildOf(picked, gameView))
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsChildOf(VisualElement element, VisualElement parent)
        {
            var current = element;
            while (current != null)
            {
                if (current == parent)
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
