using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the ending screen interactions and total star display.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting ending screen elements.")]
        private UIDocument _uiDocument;

        /// <summary>
        /// Raised when the player requests returning to the main menu.
        /// </summary>
        public event Action OnBackToMenu;

        private Button _backButton;
        private Label _totalStarsLabel;

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root is null) return;

            // Query elements
            _backButton = root.Q<Button>("btn-back-menu");
            _totalStarsLabel = root.Q<Label>("total-stars");

            // Register callbacks
            if (_backButton is not null)
                _backButton.clicked += HandleBackClicked;
        }

        private void OnDisable()
        {
            // Unregister callbacks
            if (_backButton is not null)
                _backButton.clicked -= HandleBackClicked;
        }

        private void HandleBackClicked()
        {
            OnBackToMenu?.Invoke();
            StageSelectionContext.Clear();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        /// <summary>
        /// Updates the total stars display.
        /// </summary>
        /// <param name="earnedStars">Total stars earned by player.</param>
        /// <param name="maxStars">Maximum possible stars.</param>
        public void SetTotalStars(int earnedStars, int maxStars)
        {
            if (_totalStarsLabel is not null)
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
