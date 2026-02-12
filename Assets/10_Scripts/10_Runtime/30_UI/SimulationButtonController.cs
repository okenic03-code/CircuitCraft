using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using CircuitCraft.Managers;
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
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private GameManager _gameManager;
        
        [Header("UI Element Names")]
        [SerializeField] private string _simulateButtonName = "simulate-button";
        [SerializeField] private string _statusLabelName = "simulation-status";

        private Button _simulateButton;
        private Label _statusLabel;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                if (root != null)
                {
                    _simulateButton = root.Q<Button>(_simulateButtonName);
                    _statusLabel = root.Q<Label>(_statusLabelName);
                    
                    if (_simulateButton != null)
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
                 Debug.LogError("SimulationButtonController: UIDocument not assigned and not found on GameObject.");
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
        }
        
        private void OnSimulateClicked()
        {
            OnSimulateClickedAsync().Forget();
        }

        private async UniTaskVoid OnSimulateClickedAsync()
        {
            if (_gameManager == null) return;

            if (_statusLabel != null)
            {
                _statusLabel.text = "Simulating...";
            }
            
            // Disable button immediately to prevent double clicks
            if (_simulateButton != null)
            {
                _simulateButton.SetEnabled(false);
            }

            try
            {
                await _gameManager.RunSimulationAsync(this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                if (_statusLabel != null)
                {
                    _statusLabel.text = "Simulation Cancelled";
                }
            }
            finally
            {
                UpdateButtonState();
            }
        }
        
        private void OnSimulationCompleted(SimulationResult result)
        {
            UpdateButtonState();
            
            if (_statusLabel != null)
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
        
        private void UpdateButtonState()
        {
            if (_simulateButton != null && _gameManager != null)
            {
                _simulateButton.SetEnabled(!_gameManager.IsSimulating);
            }
        }
    }
}
