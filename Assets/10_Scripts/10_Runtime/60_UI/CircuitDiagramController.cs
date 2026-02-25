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
        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting circuit diagram elements.")]
        private UIDocument _uiDocument;

        [SerializeField]
        [Tooltip("Wire in Inspector: Stage manager providing current stage data.")]
        private StageManager _stageManager;
        private VisualElement _modal;
        private Label _titleLabel;
        private Label _textLabel;
        private Button _openButton;
        private Button _closeButton;

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root is null) return;

            _modal = root.Q<VisualElement>("circuit-diagram-modal");
            _titleLabel = root.Q<Label>("circuit-diagram-title");
            _textLabel = root.Q<Label>("circuit-diagram-text");
            _openButton = root.Q<Button>("CircuitDiagramButton");
            _closeButton = root.Q<Button>("circuit-diagram-close");

            if (_openButton is not null)
                _openButton.clicked += ToggleModal;
            if (_closeButton is not null)
                _closeButton.clicked += HideModal;

            if (_stageManager != null)
                _stageManager.OnStageLoaded += OnStageLoaded;
        }

        private void OnDisable()
        {
            if (_openButton is not null)
                _openButton.clicked -= ToggleModal;
            if (_closeButton is not null)
                _closeButton.clicked -= HideModal;

            if (_stageManager != null)
                _stageManager.OnStageLoaded -= OnStageLoaded;
        }

        private void OnStageLoaded()
        {
            if (_stageManager == null || _stageManager.CurrentStage == null) return;

            var stage = _stageManager.CurrentStage;
            if (_titleLabel is not null)
                _titleLabel.text = $"Circuit: {stage.DisplayName}";

            if (_textLabel is not null)
            {
                _textLabel.text = string.IsNullOrEmpty(stage.CircuitDiagramDescription)
                    ? "No circuit description available for this stage."
                    : stage.CircuitDiagramDescription;
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
        }

        private void HideModal()
        {
            if (_modal is not null)
                _modal.style.display = DisplayStyle.None;
        }
    }
}
