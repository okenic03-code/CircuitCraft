using UnityEngine;
using UnityEngine.Serialization;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Base definition for an electronic component in the game.
    /// This ScriptableObject acts as a data container for component properties.
    /// </summary>
    [CreateAssetMenu(fileName = "NewComponent", menuName = "CircuitCraft/Component Definition")]
    public class ComponentDefinition : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Unique internal identifier for the component (e.g., 'resistor_1k').")]
        [FormerlySerializedAs("id")]
        private string _id;

        [SerializeField]
        [Tooltip("User-facing name of the component.")]
        [FormerlySerializedAs("displayName")]
        private string _displayName;

        [SerializeField]
        [Tooltip("The category of electronic component.")]
        [FormerlySerializedAs("kind")]
        private ComponentKind _kind;

        [SerializeField]
        [Tooltip("Connection pins defined for this component.")]
        [FormerlySerializedAs("pins")]
        private PinDefinition[] _pins;

        [SerializeField]
        [Tooltip("The physical footprint of the component on the grid.")]
        [FormerlySerializedAs("footprint")]
        private FootprintDefinition _footprint;

        [SerializeField]
        [Tooltip("The base cost to place this component.")]
        [FormerlySerializedAs("baseCost")]
        private float _baseCost;

        [SerializeField]
        [Tooltip("The visual prefab used when this component is placed.")]
        [FormerlySerializedAs("prefab")]
        private GameObject _prefab;

        // Electrical value fields for simulation
        [Header("Electrical Values")]
        [SerializeField]
        [Tooltip("Resistance in Ohms (for Resistor components).")]
        [FormerlySerializedAs("resistanceOhms")]
        private float _resistanceOhms;

        [SerializeField]
        [Tooltip("Voltage in Volts (for VoltageSource components).")]
        [FormerlySerializedAs("voltageVolts")]
        private float _voltageVolts;

        [SerializeField]
        [Tooltip("Capacitance in Farads (for Capacitor components).")]
        [FormerlySerializedAs("capacitanceFarads")]
        private float _capacitanceFarads;

        [SerializeField]
        [Tooltip("Inductance in Henrys (for Inductor components).")]
        [FormerlySerializedAs("inductanceHenrys")]
        private float _inductanceHenrys;

        [SerializeField]
        [Tooltip("Current in Amperes (for CurrentSource components).")]
        [FormerlySerializedAs("currentAmps")]
        private float _currentAmps;

        [Header("Semiconductor Parameters - BJT")]
        [SerializeField]
        private BJTPolarity _bjtPolarity = BJTPolarity.NPN;

        [SerializeField]
        private BJTModel _bjtModel = BJTModel._2N2222;

        [SerializeField]
        [Tooltip("Beta (Bf) - DC current gain")]
        private float _beta = 100f;

        [SerializeField]
        [Tooltip("Vaf - Early voltage in Volts")]
        private float _earlyVoltage = 75f;

        [Header("Semiconductor Parameters - MOSFET")]
        [SerializeField]
        private FETPolarity _fetPolarity = FETPolarity.NChannel;

        [SerializeField]
        private MOSFETModel _mosfetModel = MOSFETModel._2N7000;

        [SerializeField]
        [Tooltip("Vto - Threshold voltage in Volts")]
        private float _thresholdVoltage = 2.0f;

        [SerializeField]
        [Tooltip("Kp - Transconductance parameter")]
        private float _transconductance = 0.3f;

        [Header("Semiconductor Parameters - Diode")]
        [SerializeField]
        private DiodeModel _diodeModel = DiodeModel._1N4148;

        [SerializeField]
        [Tooltip("Is - Saturation current in Amps")]
        private float _saturationCurrent = 1e-9f;

        [SerializeField]
        [Tooltip("N - Emission coefficient")]
        private float _emissionCoefficient = 1.8f;

        [SerializeField]
        [Tooltip("Vf - Forward voltage in Volts (for LEDs)")]
        private float _forwardVoltage = 0.7f;

        /// <summary>Unique internal identifier for the component.</summary>
        public string Id => _id;

        /// <summary>User-facing name of the component.</summary>
        public string DisplayName => _displayName;

        /// <summary>The category of electronic component.</summary>
        public ComponentKind Kind => _kind;

        /// <summary>Connection pins defined for this component.</summary>
        public PinDefinition[] Pins => _pins;

        /// <summary>The physical footprint of the component on the grid.</summary>
        public FootprintDefinition Footprint => _footprint;

        /// <summary>The base cost to place this component.</summary>
        public float BaseCost => _baseCost;

        /// <summary>The visual prefab used when this component is placed.</summary>
        public GameObject Prefab => _prefab;

        // Electrical value properties for simulation
        /// <summary>Resistance in Ohms (for Resistor components).</summary>
        public float ResistanceOhms => _resistanceOhms;

        /// <summary>Voltage in Volts (for VoltageSource components).</summary>
        public float VoltageVolts => _voltageVolts;

        /// <summary>Capacitance in Farads (for Capacitor components).</summary>
        public float CapacitanceFarads => _capacitanceFarads;

        /// <summary>Inductance in Henrys (for Inductor components).</summary>
        public float InductanceHenrys => _inductanceHenrys;

        /// <summary>Current in Amperes (for CurrentSource components).</summary>
        public float CurrentAmps => _currentAmps;

        // Semiconductor properties for simulation
        /// <summary>BJT polarity (NPN or PNP).</summary>
        public BJTPolarity BJTPolarity => _bjtPolarity;

        /// <summary>BJT transistor model.</summary>
        public BJTModel BJTModel => _bjtModel;

        /// <summary>Beta (Bf) - DC current gain.</summary>
        public float Beta => _beta;

        /// <summary>Vaf - Early voltage in Volts.</summary>
        public float EarlyVoltage => _earlyVoltage;

        /// <summary>FET channel polarity (N-Channel or P-Channel).</summary>
        public FETPolarity FETPolarity => _fetPolarity;

        /// <summary>MOSFET transistor model.</summary>
        public MOSFETModel MOSFETModel => _mosfetModel;

        /// <summary>Vto - Threshold voltage in Volts.</summary>
        public float ThresholdVoltage => _thresholdVoltage;

        /// <summary>Kp - Transconductance parameter.</summary>
        public float Transconductance => _transconductance;

        /// <summary>Diode model.</summary>
        public DiodeModel DiodeModel => _diodeModel;

        /// <summary>Is - Saturation current in Amps.</summary>
        public float SaturationCurrent => _saturationCurrent;

        /// <summary>N - Emission coefficient.</summary>
        public float EmissionCoefficient => _emissionCoefficient;

        /// <summary>Vf - Forward voltage in Volts (for LEDs).</summary>
        public float ForwardVoltage => _forwardVoltage;
    }
}
