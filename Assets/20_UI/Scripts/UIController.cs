using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the main UI elements of the CircuitCraft application.
    /// Handles interaction with the Toolbar, Component Palette, Game View, and Status Bar.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private Label _statusLabel;
        private VisualElement _componentPalette;
        private VisualElement _toolbar;
        private VisualElement _gameView;

        private Button _clearBoardButton;
        private Button _runSimulationButton;
        private Button _saveCircuitButton;
        private Button _loadCircuitButton;
        
        private string _selectedComponent = "None";

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

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

        private void Update()
        {
            // Update status based on current state
            // For now, just maintain current status or add placeholder logic
            string status = _selectedComponent == "None" ? "Ready" : $"Selected: {_selectedComponent}";
            
            // Placeholder for grid position (using mouse position for now)
            Vector3 mousePos = Input.mousePosition;
            string gridPos = $"({(int)mousePos.x}, {(int)mousePos.y})";
            
            SetStatusText($"{status} | Pos: {gridPos}");
        }

        private void QueryVisualElements()
        {
            _statusLabel = _root.Q<Label>("StatusText");
            _componentPalette = _root.Q<VisualElement>("ComponentPalette");
            _toolbar = _root.Q<VisualElement>("Toolbar");
            _gameView = _root.Q<VisualElement>("GameView");

            if (_toolbar != null)
            {
                _clearBoardButton = _toolbar.Q<Button>("ClearBoardButton");
                _runSimulationButton = _toolbar.Q<Button>("RunSimulationButton");
                _saveCircuitButton = _toolbar.Q<Button>("SaveCircuitButton");
                _loadCircuitButton = _toolbar.Q<Button>("LoadCircuitButton");
            }

            if (_statusLabel == null) Debug.LogWarning("UIController: StatusText label not found.");
            if (_componentPalette == null) Debug.LogWarning("UIController: ComponentPalette element not found.");
            if (_toolbar == null) Debug.LogWarning("UIController: Toolbar element not found.");
            if (_gameView == null) Debug.LogWarning("UIController: GameView element not found.");
        }

        private void RegisterCallbacks()
        {
            // Register click handlers for component palette elements
            if (_componentPalette != null)
            {
                var resistorBtn = _componentPalette.Q<Button>("ComponentButton_Resistor");
                resistorBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Resistor"));

                var capacitorBtn = _componentPalette.Q<Button>("ComponentButton_Capacitor");
                capacitorBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Capacitor"));

                var inductorBtn = _componentPalette.Q<Button>("ComponentButton_Inductor");
                inductorBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Inductor"));

                var voltageSourceBtn = _componentPalette.Q<Button>("ComponentButton_VoltageSource");
                voltageSourceBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Voltage Source"));

                var currentSourceBtn = _componentPalette.Q<Button>("ComponentButton_CurrentSource");
                currentSourceBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Current Source"));

                var wireBtn = _componentPalette.Q<Button>("ComponentButton_Wire");
                wireBtn?.RegisterCallback<ClickEvent>(evt => OnComponentSelected("Wire"));
            }

            // Register click handlers for toolbar elements
            if (_toolbar != null)
            {
                _clearBoardButton?.RegisterCallback<ClickEvent>(evt => OnClearBoard());
                _runSimulationButton?.RegisterCallback<ClickEvent>(evt => OnRunSimulation());
                _saveCircuitButton?.RegisterCallback<ClickEvent>(evt => OnSaveCircuit());
                _loadCircuitButton?.RegisterCallback<ClickEvent>(evt => OnLoadCircuit());
            }
        }

        /// <summary>
        /// Updates the text displayed in the status bar.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public void SetStatusText(string text)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = text;
            }
        }

        /// <summary>
        /// Handles the selection of a component type.
        /// </summary>
        /// <param name="componentType">The type of component selected (e.g., "Resistor", "Capacitor").</param>
        public void OnComponentSelected(string componentType)
        {
            _selectedComponent = componentType;
            // Placeholder logic for component selection
            SetStatusText($"Selected component: {componentType}");
            Debug.Log($"UIController: Component selected: {componentType}");
        }

        /// <summary>
        /// Highlights the specified tool in the toolbar.
        /// </summary>
        /// <param name="toolName">The name of the tool to highlight.</param>
        public void HighlightTool(string toolName)
        {
            // Placeholder logic for tool highlighting
            // In a real implementation, we would find the tool button and apply a "selected" style class
            SetStatusText($"Tool selected: {toolName}");
            Debug.Log($"UIController: Tool highlighted: {toolName}");
        }

        private void OnClearBoard()
        {
            Debug.Log("UIController: Clear Board clicked.");
            SetStatusText("Board cleared.");
        }

        private void OnRunSimulation()
        {
            Debug.Log("UIController: Run Simulation clicked.");
            SetStatusText("Simulation started.");
        }

        private void OnSaveCircuit()
        {
            Debug.Log("UIController: Save Circuit clicked.");
            SetStatusText("Circuit saved.");
        }

        private void OnLoadCircuit()
        {
            Debug.Log("UIController: Load Circuit clicked.");
            SetStatusText("Circuit loaded.");
        }
    }
}
