using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Data;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the Stage Select screen, managing the display of stage cards and user interactions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StageSelectController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] 
        [Tooltip("The stages to display in this world (World 1).")]
        private StageDefinition[] _stages;

        [Header("UI References")]
        [SerializeField] 
        [Tooltip("Reference to the UIDocument component.")]
        private UIDocument _uiDocument;

        // Events
        public event Action<StageDefinition> OnStageSelected;
        public event Action OnBackToMenu;

        // Internal State
        private VisualElement _root;
        private List<VisualElement> _stageCards = new List<VisualElement>();
        
        // Callback references for cleanup
        private Button _btnBack;
        private readonly List<(VisualElement card, EventCallback<ClickEvent> callback)> _cardCallbacks 
            = new List<(VisualElement, EventCallback<ClickEvent>)>();
        
        // Progression State (Simulated here until ProgressionManager is integrated)
        // Index matches _stages array.
        private bool[] _unlockedStages;
        private int[] _stageStars;

        private void Awake()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            
            // Initialize default state if not set externally
            // Default: 5 stages supported by UI
            int capacity = (_stages != null && _stages.Length > 0) ? _stages.Length : 5;
            _unlockedStages = new bool[capacity];
            _stageStars = new int[capacity];

            // Default: First stage unlocked
            if (capacity > 0) _unlockedStages[0] = true;
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            FindAndSetupUI();
            UpdateStageDisplay();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void FindAndSetupUI()
        {
            // Clean up previous callbacks
            UnregisterCallbacks();
            
            _stageCards.Clear();

            // Setup Back Button
            _btnBack = _root.Q<Button>("btn-back");
            if (_btnBack != null)
            {
                _btnBack.clicked += HandleBackClicked;
            }

            // Setup Stage Cards (assuming max 5 for now as per UXML)
            for (int i = 0; i < 5; i++)
            {
                int stageIndex = i; // Capture for lambda
                string cardName = $"stage-{stageIndex + 1}";
                var card = _root.Q<VisualElement>(cardName);

                if (card != null)
                {
                    _stageCards.Add(card);
                    
                    // Create and register click event callback
                    EventCallback<ClickEvent> callback = evt => HandleStageClick(stageIndex);
                    card.RegisterCallback(callback);
                    _cardCallbacks.Add((card, callback));
                }
            }
        }

        private void UnregisterCallbacks()
        {
            if (_btnBack != null)
            {
                _btnBack.clicked -= HandleBackClicked;
                _btnBack = null;
            }
            
            foreach (var (card, callback) in _cardCallbacks)
            {
                card?.UnregisterCallback(callback);
            }
            _cardCallbacks.Clear();
        }

        private void HandleBackClicked()
        {
            OnBackToMenu?.Invoke();
        }

        private void HandleStageClick(int index)
        {
            // Check bounds
            if (_stages == null || index < 0 || index >= _stages.Length) return;
            
            // Check locked state
            if (!_unlockedStages[index]) return;

            // Fire event
            OnStageSelected?.Invoke(_stages[index]);
        }

        /// <summary>
        /// Refresh the UI based on current state and StageDefinitions.
        /// </summary>
        public void UpdateStageDisplay()
        {
            if (_root == null) return;

            // If no stage data is assigned, leave UXML defaults visible
            if (_stages == null || _stages.Length == 0) return;

            for (int i = 0; i < _stageCards.Count; i++)
            {
                var card = _stageCards[i];
                
                // If we have data for this card
                if (i < _stages.Length)
                {
                    var stageDef = _stages[i];
                    bool isUnlocked = _unlockedStages[i];
                    int stars = _stageStars[i];

                    // Find internal labels
                    var lblNumber = card.Q<Label>($"stage-number-{i + 1}");
                    var lblName = card.Q<Label>($"stage-name-{i + 1}");
                    var lblStars = card.Q<Label>($"stage-stars-{i + 1}");

                    // Update Text
                    if (lblNumber != null) lblNumber.text = $"{stageDef.WorldId}-{stageDef.StageNumber}"; // Fallback format, ideally "1-1"
                    // If stageDef.WorldId is "w1" or similar, we might want to format it nicer. 
                    // Assuming StageDefinition has what we need. 
                    // Let's use simple formatting: "1-{i+1}" if WorldId is complex. 
                    // Actually, let's just use data from StageDefinition if available, else fallback.
                    if (lblNumber != null) lblNumber.text = $"{stageDef.StageNumber}"; // Just the number or 1-1? 
                                                                                       // Prompt requested "1-1". 
                                                                                       // Let's assume World 1.
                    if (lblNumber != null) lblNumber.text = $"1-{stageDef.StageNumber}";

                    if (lblName != null) lblName.text = stageDef.DisplayName;
                    
                    if (lblStars != null) lblStars.text = GetStarString(stars);

                    // Update Styles
                    if (isUnlocked)
                    {
                        card.RemoveFromClassList("locked");
                        if (stars > 0) card.AddToClassList("completed");
                        else card.RemoveFromClassList("completed");
                    }
                    else
                    {
                        card.AddToClassList("locked");
                        card.RemoveFromClassList("completed");
                    }
                    
                    card.SetEnabled(isUnlocked); // Optional: Disables interaction on UI level too
                }
                else
                {
                    // Hide extra cards if we define fewer stages than UI slots
                    card.style.display = DisplayStyle.None;
                }
            }
        }

        private string GetStarString(int starCount)
        {
            // Simple mapping
            switch (starCount)
            {
                case 1: return "★☆☆";
                case 2: return "★★☆";
                case 3: return "★★★";
                default: return "☆☆☆";
            }
        }

        /// <summary>
        /// Unlocks a specific stage by index (0-based).
        /// </summary>
        public void UnlockStage(int stageIndex)
        {
            if (stageIndex >= 0 && stageIndex < _unlockedStages.Length)
            {
                _unlockedStages[stageIndex] = true;
                UpdateStageDisplay();
            }
        }

        /// <summary>
        /// Sets the star rating for a specific stage.
        /// </summary>
        public void SetStageStars(int stageIndex, int stars)
        {
            if (stageIndex >= 0 && stageIndex < _stageStars.Length)
            {
                _stageStars[stageIndex] = Mathf.Clamp(stars, 0, 3);
                // Usually earning stars implies unlocking, but we'll keep it separate or handled by manager
                UpdateStageDisplay();
            }
        }
    }
}
