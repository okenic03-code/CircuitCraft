using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Data;
using CircuitCraft.Controllers;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the Property Editor panel below the component palette.
    /// Shows editable value fields for configurable components (R/L/C/V/I)
    /// and read-only spec cards for semiconductors (BJT/FET/Diode/LED).
    /// </summary>
    public class PropertyEditorController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Wire in Inspector: UIDocument hosting property editor elements.")]
        private UIDocument _uiDocument;
        [SerializeField]
        [Tooltip("Wire in Inspector: Placement controller receiving edited custom values.")]
        private PlacementController _placementController;
        [SerializeField]
        [Tooltip("Wire in Inspector: Palette controller emitting component selection events.")]
        private ComponentPaletteController _paletteController;

        // UXML element references
        private VisualElement _panel;          // name="PropertyEditor"
        private Label _title;                  // name="PropEditorTitle"
        private VisualElement _editableSection; // name="PropEditorEditable"
        private VisualElement _readOnlySection; // name="PropEditorReadOnly"
        private Label _valueLabel;             // name="PropValueLabel"
        private TextField _valueField;         // name="PropValueField"
        private Label _unitLabel;              // name="PropUnitLabel"
        private Label _defaultHint;            // name="PropDefaultHint"
        private Label _specsText;              // name="PropSpecsText"

        private ComponentDefinition _currentDef;

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root is null) return;

            // 2. Query all UXML elements
            _panel = root.Q<VisualElement>("PropertyEditor");
            _title = root.Q<Label>("PropEditorTitle");
            _editableSection = root.Q<VisualElement>("PropEditorEditable");
            _readOnlySection = root.Q<VisualElement>("PropEditorReadOnly");
            _valueLabel = root.Q<Label>("PropValueLabel");
            _valueField = root.Q<TextField>("PropValueField");
            _unitLabel = root.Q<Label>("PropUnitLabel");
            _defaultHint = root.Q<Label>("PropDefaultHint");
            _specsText = root.Q<Label>("PropSpecsText");

            // 3. Register TextField callback
            if (_valueField is not null)
                _valueField.RegisterValueChangedCallback(OnValueChanged);

            // 4. Subscribe to palette selection event
            if (_paletteController != null)
                _paletteController.OnComponentSelected += OnComponentSelected;

            // 5. Validate dependencies
            if (_placementController == null)
                Debug.LogError("PropertyEditorController: PlacementController reference is missing.");
            if (_paletteController == null)
                Debug.LogError("PropertyEditorController: ComponentPaletteController reference is missing.");
        }

        private void OnDisable()
        {
            if (_valueField is not null)
                _valueField.UnregisterValueChangedCallback(OnValueChanged);

            if (_paletteController != null)
                _paletteController.OnComponentSelected -= OnComponentSelected;
        }

        /// Called when a component is selected/deselected in the palette
        private void OnComponentSelected(ComponentDefinition def)
        {
            _currentDef = def;

            // Null = deselected
            if (def == null)
            {
                HidePanel();
                return;
            }

            // Ground and Probe have no properties to show
            if (def.Kind == ComponentKind.Ground || def.Kind == ComponentKind.Probe)
            {
                HidePanel();
                return;
            }

            // Show panel with component name
            ShowPanel();
            if (_title is not null)
                _title.text = def.DisplayName;

            if (def.Kind.SupportsCustomValue())
            {
                ShowEditableMode(def);
            }
            else
            {
                ShowReadOnlyMode(def);
            }
        }

        private void ShowEditableMode(ComponentDefinition def)
        {
            if (_editableSection is not null)
                _editableSection.style.display = DisplayStyle.Flex;
            if (_readOnlySection is not null)
                _readOnlySection.style.display = DisplayStyle.None;

            float defaultValue = GetDefaultValue(def);

            if (_valueLabel is not null)
                _valueLabel.text = def.Kind.GetValueLabel();
            if (_unitLabel is not null)
                _unitLabel.text = def.Kind.GetValueUnit();
            if (_defaultHint is not null)
                _defaultHint.text = $"Default: {FormatValue(defaultValue)}";

            // Set field to default value (PlacementController resets CustomValue on selection)
            if (_valueField is not null)
                _valueField.SetValueWithoutNotify(FormatValue(defaultValue));

            // Set default value via PlacementController
            if (_placementController != null)
                _placementController.SetCustomValue(null); // null = use SO default
        }

        private void ShowReadOnlyMode(ComponentDefinition def)
        {
            if (_editableSection is not null)
                _editableSection.style.display = DisplayStyle.None;
            if (_readOnlySection is not null)
                _readOnlySection.style.display = DisplayStyle.Flex;

            if (_specsText is not null)
                _specsText.text = GetSpecsText(def);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            if (_currentDef == null || _placementController == null) return;

            if (float.TryParse(evt.newValue, out float value))
            {
                _placementController.SetCustomValue(value);
            }
            else if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                // Empty = use default
                _placementController.SetCustomValue(null);
            }
            // If invalid non-empty input, don't change — let user keep typing
        }

        private float GetDefaultValue(ComponentDefinition def)
        {
            return def.Kind switch
            {
                ComponentKind.Resistor => def.ResistanceOhms,
                ComponentKind.Capacitor => def.CapacitanceFarads,
                ComponentKind.Inductor => def.InductanceHenrys,
                ComponentKind.VoltageSource => def.VoltageVolts,
                ComponentKind.CurrentSource => def.CurrentAmps,
                _ => 0f
            };
        }

        private string GetSpecsText(ComponentDefinition def)
        {
            return def.Kind switch
            {
                ComponentKind.BJT => $"{def.BJTPolarity}\nBeta (β) = {def.Beta}\nVA = {def.EarlyVoltage}V",
                ComponentKind.MOSFET => $"{def.FETPolarity}\nVth = {def.ThresholdVoltage}V\nKp = {def.Transconductance}",
                ComponentKind.Diode => $"Model: {def.DiodeModel}\nIs = {def.SaturationCurrent:E2}A\nVf = {def.ForwardVoltage}V",
                ComponentKind.LED => $"Forward Voltage: {def.ForwardVoltage}V\nIs = {def.SaturationCurrent:E2}A",
                _ => def.DisplayName
            };
        }

        private string FormatValue(float value)
        {
            // Use engineering-friendly format
            if (value >= 1e6f) return $"{value / 1e6f:G4}M";
            if (value >= 1e3f) return $"{value / 1e3f:G4}k";
            if (value >= 1f) return $"{value:G4}";
            if (value >= 1e-3f) return $"{value * 1e3f:G4}m";
            if (value >= 1e-6f) return $"{value * 1e6f:G4}µ";
            if (value >= 1e-9f) return $"{value * 1e9f:G4}n";
            return $"{value:G4}";
        }

        private void ShowPanel()
        {
            if (_panel is not null)
                _panel.style.display = DisplayStyle.Flex;
        }

        private void HidePanel()
        {
            if (_panel is not null)
                _panel.style.display = DisplayStyle.None;
        }
    }
}
