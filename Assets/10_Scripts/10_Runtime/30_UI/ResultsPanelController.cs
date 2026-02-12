using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Managers;
using CircuitCraft.Simulation;

namespace CircuitCraft.UI
{
    /// <summary>
    /// UI Toolkit controller for the simulation results panel.
    /// Displays probe results (voltage/current) and simulation status.
    /// </summary>
    /// <remarks>
    /// Expected UXML Structure:
    /// - VisualElement name="results-panel" (The main container)
    ///   - Label name="results-text" (For displaying the text output)
    ///   - Button name="btn-clear-results" (To clear/hide results)
    ///   - Button name="btn-toggle-results" (To toggle panel visibility manually)
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ResultsPanelController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        
        private UIDocument _uiDocument;
        private VisualElement _panel;
        private Label _resultsText;
        private Button _clearButton;
        private Button _toggleButton;
        
        private void Awake() => Init();

        private void Init()
        {
            InitializeUIDocument();
            ValidateGameManager();
        }

        private void InitializeUIDocument()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void ValidateGameManager()
        {
            if (_gameManager == null)
            {
                Debug.LogError("ResultsPanelController: GameManager reference is missing. Assign via Inspector.");
            }
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Query elements
            _panel = root.Q<VisualElement>("results-panel");
            _resultsText = root.Q<Label>("results-text");
            _clearButton = root.Q<Button>("btn-clear-results");
            _toggleButton = root.Q<Button>("btn-toggle-results");
            
            // Bind events
            if (_clearButton != null)
                _clearButton.clicked += OnClearClicked;
            
            if (_toggleButton != null)
                _toggleButton.clicked += OnToggleClicked;
            
            if (_gameManager != null)
                _gameManager.OnSimulationCompleted += OnSimulationCompleted;
                
            // Initial state
            UpdateToggleText();
        }
        
        private void OnDisable()
        {
            if (_clearButton != null)
                _clearButton.clicked -= OnClearClicked;
            
            if (_toggleButton != null)
                _toggleButton.clicked -= OnToggleClicked;
            
            if (_gameManager != null)
                _gameManager.OnSimulationCompleted -= OnSimulationCompleted;
        }
        
        private void OnSimulationCompleted(SimulationResult result)
        {
            DisplayResults(result);
            ShowPanel();
        }
        
        private void DisplayResults(SimulationResult result)
        {
            if (_resultsText == null) return;
            
            StringBuilder sb = new StringBuilder();
            
            if (result.IsSuccess)
            {
                sb.AppendLine("✓ Simulation Successful");
                sb.AppendLine("----------------------");
                
                if (result.ProbeResults.Count == 0)
                {
                    sb.AppendLine("No probes placed.");
                }
                else
                {
                    foreach (var probe in result.ProbeResults)
                    {
                        // E.g., "[Voltage] Node 'out': 3.300 V"
                        sb.AppendLine($"[{probe.Type}] {probe.Target}: {probe.GetFormattedValue()}");
                    }
                }
            }
            else
            {
                sb.AppendLine("✗ Simulation Failed");
                sb.AppendLine("-------------------");
                sb.AppendLine(result.StatusMessage);
                
                if (result.Issues != null && result.Issues.Count > 0)
                {
                    foreach (var issue in result.Issues)
                    {
                        sb.AppendLine($"- {issue}");
                    }
                }
            }
            
            _resultsText.text = sb.ToString();
        }
        
        private void OnClearClicked()
        {
            if (_resultsText != null)
            {
                _resultsText.text = "";
            }
            HidePanel();
        }
        
        private void OnToggleClicked()
        {
            if (_panel != null)
            {
                bool isVisible = _panel.style.display != DisplayStyle.None;
                if (isVisible)
                    HidePanel();
                else
                    ShowPanel();
            }
        }
        
        private void ShowPanel()
        {
            if (_panel != null)
            {
                _panel.style.display = DisplayStyle.Flex;
            }
            UpdateToggleText();
        }
        
        private void HidePanel()
        {
            if (_panel != null)
            {
                _panel.style.display = DisplayStyle.None;
            }
            UpdateToggleText();
        }

        private void UpdateToggleText()
        {
            if (_toggleButton == null) return;
            
            bool isVisible = _panel != null && _panel.style.display != DisplayStyle.None;
            _toggleButton.text = isVisible ? "Hide Results" : "Show Results";
        }
    }
}
