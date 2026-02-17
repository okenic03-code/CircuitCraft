using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Managers;
using CircuitCraft.Simulation;
using CircuitCraft.Core;

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
        [SerializeField] private StageManager _stageManager;
        
        private UIDocument _uiDocument;
        private VisualElement _panel;
        private Label _resultsText;
        private Button _modifyButton;
        private Button _retryButton;
        private Button _nextStageButton;
        private Button _toggleButton;

        // New Score Elements
        private Label _starDisplay;
        private Label _resultStatus;
        private Label _scoreBreakdown;
        private Label _totalScore;
        private Label _rechallengePrompt;

        public event System.Action OnRetryRequested;
        public event System.Action OnNextStageRequested;
        
        private void Awake() => Init();

        private void Init()
        {
            InitializeUIDocument();
            ValidateDependencies();
        }

        private void InitializeUIDocument()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void ValidateDependencies()
        {
            if (_gameManager == null)
                Debug.LogError("ResultsPanelController: GameManager reference is missing.");
            if (_stageManager == null)
                Debug.LogError("ResultsPanelController: StageManager reference is missing.");
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Query elements
            _panel = root.Q<VisualElement>("results-panel");
            _resultsText = root.Q<Label>("results-text");
            _modifyButton = root.Q<Button>("btn-modify-circuit");
            _retryButton = root.Q<Button>("btn-retry-stage");
            _nextStageButton = root.Q<Button>("btn-next-stage");
            _toggleButton = root.Q<Button>("btn-toggle-results");

            _starDisplay = root.Q<Label>("star-display");
            _resultStatus = root.Q<Label>("result-status");
            _scoreBreakdown = root.Q<Label>("score-breakdown");
            _totalScore = root.Q<Label>("total-score");
            _rechallengePrompt = root.Q<Label>("rechallenge-prompt");
            
            // Bind events
            if (_modifyButton != null)
                _modifyButton.clicked += OnModifyCircuit;

            if (_retryButton != null)
                _retryButton.clicked += OnRetryStage;

            if (_nextStageButton != null)
                _nextStageButton.clicked += OnNextStage;
            
            if (_toggleButton != null)
                _toggleButton.clicked += OnToggleClicked;
            
            if (_gameManager != null)
                _gameManager.OnSimulationCompleted += OnSimulationCompleted;

            if (_stageManager != null)
                _stageManager.OnStageCompleted += OnStageCompleted;
                
            // Initial state
            UpdateToggleText();
        }
        
        private void OnDisable()
        {
            if (_modifyButton != null)
                _modifyButton.clicked -= OnModifyCircuit;

            if (_retryButton != null)
                _retryButton.clicked -= OnRetryStage;

            if (_nextStageButton != null)
                _nextStageButton.clicked -= OnNextStage;
            
            if (_toggleButton != null)
                _toggleButton.clicked -= OnToggleClicked;
            
            if (_gameManager != null)
                _gameManager.OnSimulationCompleted -= OnSimulationCompleted;

            if (_stageManager != null)
                _stageManager.OnStageCompleted -= OnStageCompleted;
        }
        
        private void OnStageCompleted(ScoreBreakdown breakdown)
        {
            if (_nextStageButton != null)
                _nextStageButton.style.display = breakdown.Passed ? DisplayStyle.Flex : DisplayStyle.None;

            DisplayScoreBreakdown(breakdown);
            ShowPanel();
        }

        public void DisplayScoreBreakdown(ScoreBreakdown breakdown)
        {
            if (_resultStatus != null)
            {
                _resultStatus.text = breakdown.Passed ? "PASSED" : "FAILED";
                _resultStatus.style.color = breakdown.Passed ? new StyleColor(new Color(0.2f, 0.8f, 0.2f)) : new StyleColor(new Color(0.8f, 0.2f, 0.2f));
            }

            if (_starDisplay != null)
            {
                _starDisplay.text = GetStarString(breakdown.Stars);
            }

            if (_scoreBreakdown != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in breakdown.LineItems)
                {
                    sb.AppendLine($"{item.Label}: {item.Points}");
                }
                _scoreBreakdown.text = sb.ToString();
            }

            if (_totalScore != null)
            {
                _totalScore.text = $"Total Score: {breakdown.TotalScore}";
            }

            if (_rechallengePrompt != null)
            {
                _rechallengePrompt.text = breakdown.Stars switch
                {
                    0 => "Tip: Check your connections and try again!",
                    1 => "Tip: Can you solve it with fewer components?",
                    2 => "Tip: Stay under budget for a perfect score!",
                    3 => "Perfect! You're a circuit master!",
                    _ => ""
                };
                _rechallengePrompt.style.display = DisplayStyle.Flex;
            }
        }

        private string GetStarString(int starCount)
        {
            return starCount switch
            {
                3 => "★★★",
                2 => "★★☆",
                1 => "★☆☆",
                _ => "☆☆☆"
            };
        }

        private void OnSimulationCompleted(SimulationResult result)
        {
            ClearScoreDisplay();
            DisplayResults(result);
            ShowPanel();
        }

        private void ClearScoreDisplay()
        {
            if (_resultStatus != null) _resultStatus.text = "";
            if (_starDisplay != null) _starDisplay.text = "";
            if (_scoreBreakdown != null) _scoreBreakdown.text = "";
            if (_totalScore != null) _totalScore.text = "";
            if (_rechallengePrompt != null) _rechallengePrompt.style.display = DisplayStyle.None;
            if (_nextStageButton != null) _nextStageButton.style.display = DisplayStyle.None;
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
        
        private void OnModifyCircuit()
        {
            HidePanel();
        }

        private void OnRetryStage()
        {
            HidePanel();
            OnRetryRequested?.Invoke();
        }

        private void OnNextStage()
        {
            HidePanel();
            OnNextStageRequested?.Invoke();
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
