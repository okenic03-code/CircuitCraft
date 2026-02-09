using UnityEngine;

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
        private string id;

        [SerializeField]
        [Tooltip("User-facing name of the component.")]
        private string displayName;

        [SerializeField]
        [Tooltip("The category of electronic component.")]
        private ComponentKind kind;

        [SerializeField]
        [Tooltip("Connection pins defined for this component.")]
        private PinDefinition[] pins;

        [SerializeField]
        [Tooltip("The physical footprint of the component on the grid.")]
        private FootprintDefinition footprint;

        [SerializeField]
        [Tooltip("The base cost to place this component.")]
        private float baseCost;

        [SerializeField]
        [Tooltip("The visual prefab used when this component is placed.")]
        private GameObject prefab;

        // Electrical value fields for simulation
        [Header("Electrical Values")]
        [SerializeField]
        [Tooltip("Resistance in Ohms (for Resistor components).")]
        private float resistanceOhms;

        [SerializeField]
        [Tooltip("Voltage in Volts (for VoltageSource components).")]
        private float voltageVolts;

        [SerializeField]
        [Tooltip("Capacitance in Farads (for Capacitor components).")]
        private float capacitanceFarads;

        [SerializeField]
        [Tooltip("Inductance in Henrys (for Inductor components).")]
        private float inductanceHenrys;

        [SerializeField]
        [Tooltip("Current in Amperes (for CurrentSource components).")]
        private float currentAmps;

        /// <summary>Unique internal identifier for the component.</summary>
        public string Id => id;

        /// <summary>User-facing name of the component.</summary>
        public string DisplayName => displayName;

        /// <summary>The category of electronic component.</summary>
        public ComponentKind Kind => kind;

        /// <summary>Connection pins defined for this component.</summary>
        public PinDefinition[] Pins => pins;

        /// <summary>The physical footprint of the component on the grid.</summary>
        public FootprintDefinition Footprint => footprint;

        /// <summary>The base cost to place this component.</summary>
        public float BaseCost => baseCost;

        /// <summary>The visual prefab used when this component is placed.</summary>
        public GameObject Prefab => prefab;

        // Electrical value properties for simulation
        /// <summary>Resistance in Ohms (for Resistor components).</summary>
        public float ResistanceOhms => resistanceOhms;

        /// <summary>Voltage in Volts (for VoltageSource components).</summary>
        public float VoltageVolts => voltageVolts;

        /// <summary>Capacitance in Farads (for Capacitor components).</summary>
        public float CapacitanceFarads => capacitanceFarads;

        /// <summary>Inductance in Henrys (for Inductor components).</summary>
        public float InductanceHenrys => inductanceHenrys;

        /// <summary>Current in Amperes (for CurrentSource components).</summary>
        public float CurrentAmps => currentAmps;
    }
}
