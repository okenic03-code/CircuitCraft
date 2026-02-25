using UnityEngine;
using CircuitCraft.Controllers;
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
        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting Help overlay elements.")]
        private UIDocument _uiDocument;
        [SerializeField]
        [Tooltip("Wire in Inspector: CameraController to disable while help overlay is open.")]
        private CameraController _cameraController;
        [SerializeField]
        [Tooltip("Wire in Inspector: WireRoutingController to disable while help overlay is open.")]
        private WireRoutingController _wireRoutingController;
        private VisualElement _modal;
        private Button _openButton;
        private Button _closeButton;

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root is null) return;

            _modal = root.Q<VisualElement>("help-overlay");
            _openButton = root.Q<Button>("HelpButton");
            _closeButton = root.Q<Button>("help-overlay-close");

            if (_openButton is not null)
                _openButton.clicked += ToggleModal;
            if (_closeButton is not null)
                _closeButton.clicked += HideModal;
        }

        private void OnDisable()
        {
            if (_openButton is not null)
                _openButton.clicked -= ToggleModal;
            if (_closeButton is not null)
                _closeButton.clicked -= HideModal;
        }

        private void Update()
        {
            // F1 key toggles help
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleModal();
                return;
            }

            // ? key (Shift + Slash) toggles help
            if (Input.GetKeyDown(KeyCode.Slash) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                ToggleModal();
            }
        }

        private void ToggleModal()
        {
            if (_modal is null) return;
            bool isVisible = _modal.style.display != DisplayStyle.None;
            if (isVisible) HideModal();
            else ShowModal();
        }

        private void ShowModal()
        {
            if (_modal is not null)
                _modal.style.display = DisplayStyle.Flex;

            if (_cameraController != null)
                _cameraController.enabled = false;
            if (_wireRoutingController != null)
                _wireRoutingController.enabled = false;
        }

        private void HideModal()
        {
            if (_modal is not null)
                _modal.style.display = DisplayStyle.None;

            if (_cameraController != null)
                _cameraController.enabled = true;
            if (_wireRoutingController != null)
                _wireRoutingController.enabled = true;
        }
    }
}
