using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the help overlay modal.
    /// Shows/hides game controls and placement instructions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HelpOverlayController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _modal;
        private Button _openButton;
        private Button _closeButton;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _modal = root.Q<VisualElement>("help-overlay");
            _openButton = root.Q<Button>("HelpButton");
            _closeButton = root.Q<Button>("help-overlay-close");

            if (_openButton != null)
                _openButton.clicked += ToggleModal;
            if (_closeButton != null)
                _closeButton.clicked += HideModal;
        }

        private void OnDisable()
        {
            if (_openButton != null)
                _openButton.clicked -= ToggleModal;
            if (_closeButton != null)
                _closeButton.clicked -= HideModal;
        }

        private void ToggleModal()
        {
            if (_modal == null) return;
            bool isVisible = _modal.style.display != DisplayStyle.None;
            if (isVisible) HideModal();
            else ShowModal();
        }

        private void ShowModal()
        {
            if (_modal != null)
                _modal.style.display = DisplayStyle.Flex;
        }

        private void HideModal()
        {
            if (_modal != null)
                _modal.style.display = DisplayStyle.None;
        }
    }
}
