using System;
using UnityEngine;

namespace CircuitCraft.Data
{
    /// <summary>
    /// Represents a single test case for a stage, specifying the expected simulation outcome.
    /// </summary>
    [Serializable]
    public class StageTestCase
    {
        [SerializeField]
        [Tooltip("Name of the test case (e.g., 'DC Output Check').")]
        private string testName;

        [SerializeField]
        [Tooltip("The expected voltage value at the measured point.")]
        private float expectedVoltage;

        [SerializeField]
        [Tooltip("The allowable error margin for the measured voltage.")]
        private float tolerance;

        /// <summary>Name of the test case.</summary>
        public string TestName => testName;

        /// <summary>The expected voltage value at the measured point.</summary>
        public float ExpectedVoltage => expectedVoltage;

        /// <summary>The allowable error margin for the measured voltage.</summary>
        public float Tolerance => tolerance;
    }
}
