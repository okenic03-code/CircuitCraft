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
    }
}
