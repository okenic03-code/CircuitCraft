using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class EndingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument _uiDocument;

        public event Action OnBackToMenu;

        private Button _backButton;
        private Label _totalStarsLabel;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // Query elements
            _backButton = root.Q<Button>("btn-back-menu");
            _totalStarsLabel = root.Q<Label>("total-stars");

            // Register callbacks
            if (_backButton != null)
                _backButton.clicked += HandleBackClicked;
        }

        private void OnDisable()
        {
            // Unregister callbacks
            if (_backButton != null)
                _backButton.clicked -= HandleBackClicked;
        }

        private void HandleBackClicked()
        {
            OnBackToMenu?.Invoke();
            // Fallback: if SceneFlowManager handles this, great.
            // But also ensure we go back to MainMenu scene
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Updates the total stars display.
        /// </summary>
        /// <param name="earnedStars">Total stars earned by player.</param>
        /// <param name="maxStars">Maximum possible stars.</param>
        public void SetTotalStars(int earnedStars, int maxStars)
        {
            if (_totalStarsLabel != null)
            {
                // Format: "★★★★★ (15/15)" - using stars just as decoration or based on %?
                // The prompt example was "★★★★★ (15/15)"
                // Let's make a simple star string based on ratio or just fixed.
                // Given the prompt "★★★★★ (15/15)", I'll just use fixed 5 stars + score
                // OR better, show actual earned vs max.
                // Let's stick to the prompt's visual suggestion.
                _totalStarsLabel.text = $"★★★★★ ({earnedStars}/{maxStars})";
            }
        }
    }
}
