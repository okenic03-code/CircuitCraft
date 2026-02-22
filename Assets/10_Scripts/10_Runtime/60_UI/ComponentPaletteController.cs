using System;
using System.Collections.Generic;
using System.Linq;
using CircuitCraft.Controllers;
using CircuitCraft.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// UI Toolkit controller for the component palette.
    /// Manages button interactions to select components for placement.
    /// </summary>
    public class ComponentPaletteController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Reference to the UIDocument component.")]
        private UIDocument _uiDocument;

        [SerializeField]
        [Tooltip("Reference to the PlacementController for handling component selection.")]
        private PlacementController _placementController;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("List of component definitions available in the palette.")]
        private ComponentDefinition[] _componentDefinitions;

        private readonly struct PaletteSection
        {
            public readonly string Header;
            public readonly string HeaderClass;
            public readonly ComponentKind[] Kinds;

            public PaletteSection(string header, string headerClass, params ComponentKind[] kinds)
            {
                Header = header;
                HeaderClass = headerClass;
                Kinds = kinds;
            }
        }

        private static readonly PaletteSection[] SectionOrder =
        {
            new PaletteSection("Sources", "section-header", ComponentKind.VoltageSource, ComponentKind.CurrentSource, ComponentKind.Ground),
            new PaletteSection("Passive", "section-header", ComponentKind.Resistor, ComponentKind.Capacitor, ComponentKind.Inductor),
            new PaletteSection("Semiconductors", "section-header",
                ComponentKind.Diode,
                ComponentKind.LED,
                ComponentKind.ZenerDiode,
                ComponentKind.BJT,
                ComponentKind.MOSFET)
        };

        // Store registered callbacks to properly unregister them
        private List<(Button btn, Action action)> _registeredCallbacks;

        private VisualElement _root;
        private ScrollView _paletteScroll;
        private Button _selectedButton;

        /// <summary>
        /// Fired when a component is selected or deselected in the palette.
        /// The argument is the selected ComponentDefinition, or null if deselected.
        /// </summary>
        public event Action<ComponentDefinition> OnComponentSelected;

        private void Awake() => Init();

        private void Init()
        {
            InitializeUIDocument();
            ValidatePlacementController();
            InitializeCallbacksList();
        }

        private void InitializeUIDocument()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("ComponentPaletteController: UIDocument reference is missing!");
            }
        }

        private void ValidatePlacementController()
        {
            if (_placementController == null)
            {
                Debug.LogError("ComponentPaletteController: PlacementController reference is missing. Assign via Inspector.");
            }
        }

        private void InitializeCallbacksList()
        {
            _registeredCallbacks = new();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root is null) return;

            _paletteScroll = FindPaletteScroll();

            RegisterCallbacks();
        }

        private ScrollView FindPaletteScroll()
        {
            if (_root is null) return null;

            return _root.Q<ScrollView>("palette-scroll")
                ?? _root.Q<ScrollView>(null, "palette-scroll");
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            _selectedButton = null;
            _paletteScroll = null;
        }

        /// <summary>
        /// Clears and rebuilds palette buttons from runtime component definitions.
        /// </summary>
        private void RegisterCallbacks()
        {
            UnregisterCallbacks();

            if (_paletteScroll is null && _root is not null)
            {
                _paletteScroll = FindPaletteScroll();
            }

            if (_paletteScroll is null)
            {
                return;
            }

            _paletteScroll.Clear();
            _selectedButton = null;

            if (_componentDefinitions == null)
            {
                return;
            }

            HashSet<ComponentKind> knownKinds = new();

            foreach (var section in SectionOrder)
            {
                var sectionKinds = new HashSet<ComponentKind>(section.Kinds);
                var sectionDefinitions = _componentDefinitions
                    .Where(def => def != null && sectionKinds.Contains(def.Kind))
                    .ToList();

                if (sectionDefinitions.Count == 0)
                {
                    continue;
                }

                AddSectionHeader(section.Header, section.HeaderClass);

                foreach (var def in sectionDefinitions)
                {
                    RegisterComponentButton(def);
                    knownKinds.Add(def.Kind);
                }
            }

            // Include any unsupported kinds under an "Other" header.
            var remainingDefinitions = _componentDefinitions
                .Where(def => def != null && !knownKinds.Contains(def.Kind))
                .ToList();

            if (remainingDefinitions.Count > 0)
            {
                AddSectionHeader("Other", "section-header");

                foreach (var def in remainingDefinitions)
                {
                    RegisterComponentButton(def);
                }
            }
        }

        private void AddSectionHeader(string title, string headerClass)
        {
            var header = new Label(title);
            header.AddToClassList(headerClass);
            _paletteScroll.Add(header);
        }

        private void RegisterComponentButton(ComponentDefinition def)
        {
            var button = new Button
            {
                name = $"btn-{def.Id}"
            };

            button.AddToClassList("component-button");
            button.Add(new Label(string.IsNullOrWhiteSpace(def.DisplayName) ? def.Id : def.DisplayName));

            Action action = () => OnComponentButtonClicked(def);
            button.clicked += action;
            _registeredCallbacks.Add((button, action));
            _paletteScroll.Add(button);
        }

        /// <summary>
        /// Unregisters all tracked callbacks to prevent memory leaks or double-invocation.
        /// </summary>
        private void UnregisterCallbacks()
        {
            foreach (var (btn, action) in _registeredCallbacks)
            {
                if (btn != null)
                {
                    btn.clicked -= action;
                }
            }
            _registeredCallbacks.Clear();
        }

        /// <summary>
        /// Sets the available component definitions at runtime (e.g. when loading a new stage).
        /// Replaces the current definitions and regenerates palette buttons.
        /// </summary>
        /// <param name="components">The component definitions to make available in the palette.</param>
        public void SetAvailableComponents(ComponentDefinition[] components)
        {
            _componentDefinitions = components;
            
            // Clear active placement when palette is replaced to prevent placing components from prior stage
            if (_placementController != null)
            {
                _placementController.SetSelectedComponent(null);
            }
            _selectedButton = null;

            // Rebuild palette if UI has been initialized.
            if (_root != null)
            {
                RegisterCallbacks();
            }
        }

        /// <summary>
        /// Handler for component button clicks.
        /// Notifies PlacementController to select the component.
        /// </summary>
        private void OnComponentButtonClicked(ComponentDefinition def)
        {
            if (_placementController == null) return;

            if (_selectedButton is not null)
            {
                _selectedButton.RemoveFromClassList("selected");
            }

            var button = _paletteScroll != null
                ? _paletteScroll.Q<Button>($"btn-{def.Id}")
                : _root?.Q<Button>($"btn-{def.Id}");

            button?.AddToClassList("selected");
            _selectedButton = button;

            _placementController.SetSelectedComponent(def);
            OnComponentSelected?.Invoke(def);
        }

        /// <summary>
        /// Deselects the current component in the palette and fires OnComponentSelected with null.
        /// </summary>
        public void DeselectComponent()
        {
            if (_selectedButton is not null)
            {
                _selectedButton.RemoveFromClassList("selected");
                _selectedButton = null;
            }
            OnComponentSelected?.Invoke(null);
        }
    }
}
