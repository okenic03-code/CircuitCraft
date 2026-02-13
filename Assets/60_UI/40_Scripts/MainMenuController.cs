using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace CircuitCraft.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private Button _playButton;
        private Button _settingsButton;
        private Button _quitButton;

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            _playButton = root.Q<Button>("btn-play");
            _settingsButton = root.Q<Button>("btn-settings");
            _quitButton = root.Q<Button>("btn-quit");

            _playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);
        }

        private void OnDisable()
        {
            _playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
            _settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
            _quitButton?.UnregisterCallback<ClickEvent>(OnQuitClicked);
        }

        private void OnPlayClicked(ClickEvent evt)
        {
            SceneManager.LoadScene("GamePlay");
        }

        private void OnSettingsClicked(ClickEvent evt)
        {
            Debug.Log("Settings clicked - Placeholder");
        }

        private void OnQuitClicked(ClickEvent evt)
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
