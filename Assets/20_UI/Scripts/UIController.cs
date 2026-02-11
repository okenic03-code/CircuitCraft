using System;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Views;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the main HUD layout: Toolbar, GameView, StatusBar.
    /// Component palette, simulation button, and results panel are handled
    /// by their own dedicated controllers.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GridCursor _gridCursor;

        private VisualElement _root;
        private VisualElement _toolbar;
        private VisualElement _gameView;
        private VisualElement _statusBar;

        private Label _statusText;
        private Label _statusCoords;
        private Label _statusGrid;
        private Label _statusZoom;

        private Button _clearBoardButton;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_gridCursor == null)
                _gridCursor = FindFirstObjectByType<GridCursor>();
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UIController: No UIDocument component found.");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("UIController: UIDocument has no root visual element.");
                return;
            }

            QueryVisualElements();
            RegisterCallbacks();
            SetStatusText("Ready");
        }

        private void OnDisable()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked -= OnClearBoard;
        }

        private void Update()
        {
            UpdateStatusBar();
        }

        private void QueryVisualElements()
        {
            _toolbar = _root.Q<VisualElement>("Toolbar");
            _gameView = _root.Q<VisualElement>("GameView");
            _statusBar = _root.Q<VisualElement>("StatusBar");

            _statusText = _root.Q<Label>("StatusText");
            _statusCoords = _root.Q<Label>("StatusCoords");
            _statusGrid = _root.Q<Label>("StatusGrid");
            _statusZoom = _root.Q<Label>("StatusZoom");

            _clearBoardButton = _root.Q<Button>("ClearBoardButton");

            if (_statusText == null) Debug.LogWarning("UIController: StatusText not found.");
            if (_toolbar == null) Debug.LogWarning("UIController: Toolbar not found.");
            if (_gameView == null) Debug.LogWarning("UIController: GameView not found.");
            if (_statusBar == null) Debug.LogWarning("UIController: StatusBar not found.");
        }

        private void RegisterCallbacks()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked += OnClearBoard;
        }

        private void UpdateStatusBar()
        {
            // Grid coordinates from GridCursor
            if (_statusCoords != null && _gridCursor != null)
            {
                Vector2Int gridPos = _gridCursor.GetCurrentGridPosition();
                _statusCoords.text = _gridCursor.IsOverValidGrid()
                    ? $"Pos: ({gridPos.x}, {gridPos.y})"
                    : "Pos: --";
            }

            // Zoom percentage from camera orthographic size
            if (_statusZoom != null && _mainCamera != null && _mainCamera.orthographic)
            {
                // Default ortho size = 12, treat as 100%
                float zoomPercent = (12f / _mainCamera.orthographicSize) * 100f;
                _statusZoom.text = $"{Mathf.RoundToInt(zoomPercent)}%";
            }
        }

        /// <summary>
        /// Updates the status text in the bottom-left of the status bar.
        /// </summary>
        public void SetStatusText(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        private void OnClearBoard()
        {
            Debug.Log("UIController: Clear Board requested.");
            SetStatusText("Board cleared.");
        }
    }
}
