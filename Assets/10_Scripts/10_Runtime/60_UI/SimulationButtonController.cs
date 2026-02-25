using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Managers;
using CircuitCraft.Core;
using CircuitCraft.Simulation;
using System;

namespace CircuitCraft.UI
{
    /// <summary>
    /// UI Toolkit controller for simulation button.
    /// Triggers circuit simulation and shows loading state.
    /// 
    /// Expected UXML:
    /// - Button name="simulate-button"
    /// - Label name="simulation-status" (optional)
    /// </summary>
    public class SimulationButtonController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting simulation button elements.")]
        private UIDocument _uiDocument;
        [SerializeField]
        [Tooltip("Wire in Inspector: Game manager used to observe simulation state.")]
        private GameManager _gameManager;
        [SerializeField]
        [Tooltip("Wire in Inspector: Stage manager used to run stage evaluation.")]
        private StageManager _stageManager;
        
        [Header("UI Element Names")]
        [SerializeField]
        [Tooltip("UXML name for the simulate button.")]
        private string _simulateButtonName = "simulate-button";
        [SerializeField]
        [Tooltip("UXML name for the simulation status label.")]
        private string _statusLabelName = "simulation-status";

        private Button _simulateButton;
        private Label _statusLabel;
        
        private void OnEnable()
        {
            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                if (root is not null)
                {
                    _simulateButton = root.Q<Button>(_simulateButtonName);
                    _statusLabel = root.Q<Label>(_statusLabelName);
                    
                    if (_simulateButton is not null)
                    {
                        _simulateButton.clicked += OnSimulateClicked;
                    }
                    else 
                    {
                        Debug.LogWarning($"SimulationButtonController: Button '{_simulateButtonName}' not found in UIDocument.");
                    }
                }
            }
            else
            {
                 Debug.LogError("SimulationButtonController: UIDocument reference is missing.");
            }
            
            if (_gameManager != null)
            {
                _gameManager.OnSimulationCompleted += OnSimulationCompleted;
                // Update state immediately in case we re-enabled during simulation
                UpdateButtonState();
            }
            else
            {
                Debug.LogError("SimulationButtonController: GameManager reference is missing.");
            }

            if (_stageManager == null)
            {
                Debug.LogError("SimulationButtonController: StageManager reference is missing.");
            }
            else
            {
                _stageManager.OnStageCompleted += OnStageEvaluationCompleted;
            }
        }
        
        private void OnDisable()
        {
            if (_simulateButton != null)
            {
                _simulateButton.clicked -= OnSimulateClicked;
            }
            
            if (_gameManager != null)
            {
                _gameManager.OnSimulationCompleted -= OnSimulationCompleted;
            }

            if (_stageManager != null)
            {
                _stageManager.OnStageCompleted -= OnStageEvaluationCompleted;
            }
        }
        
        private void OnSimulateClicked()
        {
            if (_gameManager == null || _stageManager == null) return;

            if (_statusLabel != null)
            {
                _statusLabel.text = "Simulating...";
            }
            
            // Disable button immediately to prevent double clicks
            if (_simulateButton is not null)
            {
                _simulateButton.SetEnabled(false);
            }

            try
            {
                _stageManager.RunSimulationAndEvaluate();
            }
            catch (Exception ex)
            {
                Debug.LogError($"SimulationButtonController: Failed to start stage simulation. {ex.Message}");
                // Re-enable button only on synchronous failure
                UpdateButtonState();
            }
        }
        
        private void OnSimulationCompleted(SimulationResult result)
        {
            UpdateButtonState();
            
            if (_statusLabel is not null)
            {
                if (result.IsSuccess)
                {
                    _statusLabel.text = "Simulation Complete";
                }
                else
                {
                    _statusLabel.text = "Simulation Failed";
                }
            }
        }

        private void OnStageEvaluationCompleted(ScoreBreakdown breakdown)
        {
            UpdateButtonState();
        }
        
        private void UpdateButtonState()
        {
            if (_simulateButton is not null && _gameManager != null)
            {
                _simulateButton.SetEnabled(!_gameManager.IsSimulating);
            }
        }
    }
}
