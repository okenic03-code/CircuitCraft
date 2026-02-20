using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Managers;
using CircuitCraft.Data;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the circuit diagram modal overlay.
    /// Shows/hides the example circuit description when the player clicks the Circuit button.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CircuitDiagramController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private StageManager _stageManager;

        private UIDocument _uiDocument;
        private VisualElement _modal;
        private Label _titleLabel;
        private Label _textLabel;
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

            _modal = root.Q<VisualElement>("circuit-diagram-modal");
            _titleLabel = root.Q<Label>("circuit-diagram-title");
            _textLabel = root.Q<Label>("circuit-diagram-text");
            _openButton = root.Q<Button>("CircuitDiagramButton");
            _closeButton = root.Q<Button>("circuit-diagram-close");

            if (_openButton != null)
                _openButton.clicked += ToggleModal;
            if (_closeButton != null)
                _closeButton.clicked += HideModal;

            if (_stageManager != null)
                _stageManager.OnStageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (_openButton != null)
                _openButton.clicked -= ToggleModal;
            if (_closeButton != null)
                _closeButton.clicked -= HideModal;

            if (_stageManager != null)
                _stageManager.OnStageLoaded -= OnStageLoaded;
        }

        private void OnStageLoaded()
        {
            if (_stageManager == null || _stageManager.CurrentStage == null) return;

            var stage = _stageManager.CurrentStage;
            if (_titleLabel != null)
                _titleLabel.text = $"Circuit: {stage.DisplayName}";

            if (_textLabel != null)
            {
                _textLabel.text = string.IsNullOrEmpty(stage.CircuitDiagramDescription)
                    ? "No circuit description available for this stage."
                    : stage.CircuitDiagramDescription;
            }
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
