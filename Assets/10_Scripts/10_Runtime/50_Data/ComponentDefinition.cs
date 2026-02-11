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
    }
}
