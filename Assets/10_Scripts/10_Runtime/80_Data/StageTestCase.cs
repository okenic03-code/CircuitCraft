using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("testName")]
        private string _testName;

        [SerializeField]
        [Tooltip("The expected voltage value at the measured point.")]
        [FormerlySerializedAs("expectedVoltage")]
        private float _expectedVoltage;

        [SerializeField]
        [Tooltip("The net/node name in the circuit netlist to probe (e.g., 'NET1'). Falls back to TestName if empty.")]
        private string _probeNode;

        [SerializeField]
        [Tooltip("The allowable error margin for the measured voltage.")]
        [FormerlySerializedAs("tolerance")]
        private float _tolerance;

        /// <summary>Name of the test case.</summary>
        public string TestName => _testName;

        /// <summary>The expected voltage value at the measured point.</summary>
        public float ExpectedVoltage => _expectedVoltage;

        /// <summary>The net/node name in the circuit netlist to probe.</summary>
        public string ProbeNode => string.IsNullOrEmpty(_probeNode) ? _testName : _probeNode;

        /// <summary>The allowable error margin for the measured voltage.</summary>
        public float Tolerance => _tolerance;
    }
}
