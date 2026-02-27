using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Simulation;
using UnityEngine;

namespace CircuitCraft.Tests.Managers
{
    /// <summary>
    /// Characterization tests for StageManager — captures ALL observable public-surface behaviors
    /// as a safety net before refactoring. Tests use only the synchronous API surface and
    /// event-firing verification; async UniTask flows are not tested here.
    /// </summary>
    [TestFixture]
    public class StageManagerTests
    {
        // Track all created Unity objects for cleanup
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        private GameObject _stageManagerGo;
        private GameObject _gameManagerGo;
        private GameObject _simulationManagerGo;

        private StageManager _stageManager;
        private GameManager _gameManager;
        private SimulationManager _simulationManager;

        [SetUp]
        public void SetUp()
        {
            // Create GameObjects and add components
            _gameManagerGo = new GameObject("TestGameManager");
            _createdObjects.Add(_gameManagerGo);

            _simulationManagerGo = new GameObject("TestSimulationManager");
            _createdObjects.Add(_simulationManagerGo);

            _stageManagerGo = new GameObject("TestStageManager");
            _createdObjects.Add(_stageManagerGo);

            // Add components (Awake fires during AddComponent)
            _simulationManager = _simulationManagerGo.AddComponent<SimulationManager>();
            _gameManager = _gameManagerGo.AddComponent<GameManager>();
            _stageManager = _stageManagerGo.AddComponent<StageManager>();

            // Wire serialized dependencies via reflection
            SetPrivateField(_stageManager, "_gameManager", _gameManager);
            SetPrivateField(_stageManager, "_simulationManager", _simulationManager);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        // ------------------------------------------------------------------
        // Helper Methods
        // ------------------------------------------------------------------

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private StageDefinition CreateStageDefinition(
            string stageId,
            string displayName,
            string worldId,
            int stageNumber,
            int targetArea,
            float budgetLimit = 0f)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            _createdObjects.Add(stage);
            SetPrivateField(stage, "_stageId", stageId);
            SetPrivateField(stage, "_displayName", displayName);
            SetPrivateField(stage, "_worldId", worldId);
            SetPrivateField(stage, "_stageNumber", stageNumber);
            SetPrivateField(stage, "_targetArea", targetArea);
            SetPrivateField(stage, "_budgetLimit", budgetLimit);
            return stage;
        }

        private ComponentDefinition CreateComponentDefinition(string id, ComponentKind kind)
        {
            var def = ScriptableObject.CreateInstance<ComponentDefinition>();
            _createdObjects.Add(def);
            SetPrivateField(def, "_id", id);
            SetPrivateField(def, "_displayName", id);
            SetPrivateField(def, "_kind", kind);
            // Explicitly set empty pins array so PinInstanceFactory uses standard pins
            SetPrivateField(def, "_pins", new PinDefinition[0]);
            return def;
        }

        private StageTestCase CreateStageTestCase(string testName, float expectedVoltage, float tolerance, string probeNode)
        {
            var testCase = new StageTestCase();
            SetPrivateField(testCase, "_testName", testName);
            SetPrivateField(testCase, "_expectedVoltage", expectedVoltage);
            SetPrivateField(testCase, "_tolerance", tolerance);
            SetPrivateField(testCase, "_probeNode", probeNode);
            return testCase;
        }

        private static T InvokeStageManagerStatic<T>(string methodName, params object[] args)
        {
            var method = typeof(StageManager).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on StageManager.");
            return (T)method.Invoke(null, args);
        }

        private static string GetAutoOutputProbeComponentId()
        {
            var field = typeof(StageManager).GetField(
                "AutoOutputProbeComponentId",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Field 'AutoOutputProbeComponentId' not found on StageManager.");
            return (string)field.GetValue(null);
        }

        // ------------------------------------------------------------------
        // CurrentStage property
        // ------------------------------------------------------------------

        [Test]
        public void CurrentStage_BeforeAnyLoad_ReturnsNull()
        {
            Assert.IsNull(_stageManager.CurrentStage);
        }

        [Test]
        public void CurrentStage_AfterLoadStage_ReturnsLoadedStage()
        {
            var stage = CreateStageDefinition("s1", "Stage 1", "world1", 1, 9);

            _stageManager.LoadStage(stage);

            Assert.AreSame(stage, _stageManager.CurrentStage);
        }

        // ------------------------------------------------------------------
        // LoadStage — null guard
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_NullStage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _stageManager.LoadStage(null));
        }

        // ------------------------------------------------------------------
        // LoadStage — board reset dimensions
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_ResetsBoard_ToDerivedSquareSize()
        {
            // TargetArea = 9 -> side = ceil(sqrt(9)) = 3
            var stage = CreateStageDefinition("s1", "Stage 1", "world1", 1, 9);

            _stageManager.LoadStage(stage);

            var bounds = _gameManager.BoardState.SuggestedBounds;
            Assert.AreEqual(3, bounds.Width, "Board width should equal ceil(sqrt(TargetArea))");
            Assert.AreEqual(3, bounds.Height, "Board height should equal ceil(sqrt(TargetArea))");
        }

        [Test]
        public void LoadStage_ResetsBoard_RoundsUpForNonPerfectSquare()
        {
            // TargetArea = 10 -> side = ceil(sqrt(10)) = 4 (sqrt(10) ~ 3.16)
            var stage = CreateStageDefinition("s-nps", "Non-perfect", "world1", 1, 10);

            _stageManager.LoadStage(stage);

            var bounds = _gameManager.BoardState.SuggestedBounds;
            Assert.AreEqual(4, bounds.Width, "Board width should be ceiling of sqrt(10)");
            Assert.AreEqual(4, bounds.Height, "Board height should be ceiling of sqrt(10)");
        }

        // ------------------------------------------------------------------
        // LoadStage — event firing
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_FiresOnStageLoadedEvent()
        {
            var stage = CreateStageDefinition("s1", "Stage 1", "world1", 1, 4);
            bool eventFired = false;
            _stageManager.OnStageLoaded += () => eventFired = true;

            _stageManager.LoadStage(stage);

            Assert.IsTrue(eventFired, "OnStageLoaded should fire after LoadStage");
        }

        [Test]
        public void LoadStage_OnStageLoaded_FiredBeforePlacingFixedComponents()
        {
            // Event should fire before components are placed, but board should still reflect reset
            var stage = CreateStageDefinition("s1", "Stage 1", "world1", 1, 4);
            int componentCountAtEvent = -1;
            _stageManager.OnStageLoaded += () =>
            {
                componentCountAtEvent = _gameManager.BoardState.Components.Count;
            };

            _stageManager.LoadStage(stage);

            // No fixed placements on this stage — board should be empty at event time
            Assert.AreEqual(0, componentCountAtEvent, "Board should be empty when OnStageLoaded fires (before fixed placements)");
        }

        // ------------------------------------------------------------------
        // LoadStage — fixed placements
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_EmptyFixedPlacements_DoesNotThrow()
        {
            var stage = CreateStageDefinition("s-empty", "Empty", "world1", 1, 4);
            SetPrivateField(stage, "_fixedPlacements", new FixedPlacement[0]);

            Assert.DoesNotThrow(() => _stageManager.LoadStage(stage));
            Assert.AreEqual(0, _gameManager.BoardState.Components.Count);
        }

        [Test]
        public void LoadStage_PlacesFixedComponentsOnBoard()
        {
            var stage = CreateStageDefinition("s-fixed", "Fixed", "world1", 1, 16);

            var resistorDef = CreateComponentDefinition("test_resistor", ComponentKind.Resistor);
            var placements = new FixedPlacement[]
            {
                new FixedPlacement
                {
                    component = resistorDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            };
            SetPrivateField(stage, "_fixedPlacements", placements);

            _stageManager.LoadStage(stage);

            Assert.AreEqual(1, _gameManager.BoardState.Components.Count,
                "One fixed component should be placed on board");
        }

        [Test]
        public void LoadStage_NullComponentInFixedPlacements_SkipsWithoutCrashing()
        {
            var stage = CreateStageDefinition("s-null-fp", "NullFP", "world1", 1, 16);

            // FixedPlacement is a struct; component field defaults to null
            var placements = new FixedPlacement[]
            {
                new FixedPlacement { component = null, position = Vector2Int.zero }
            };
            SetPrivateField(stage, "_fixedPlacements", placements);

            Assert.DoesNotThrow(() => _stageManager.LoadStage(stage),
                "Null component in FixedPlacements should be skipped without throwing");
            Assert.AreEqual(0, _gameManager.BoardState.Components.Count,
                "No components should be placed when fixed placement has null component");
        }

        [Test]
        public void LoadStage_FixedComponent_IsMarkedAsFixed()
        {
            var stage = CreateStageDefinition("s-fixed-flag", "FixedFlag", "world1", 1, 16);

            var resistorDef = CreateComponentDefinition("test_resistor_fixed", ComponentKind.Resistor);
            var placements = new FixedPlacement[]
            {
                new FixedPlacement
                {
                    component = resistorDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            };
            SetPrivateField(stage, "_fixedPlacements", placements);

            _stageManager.LoadStage(stage);

            var placed = _gameManager.BoardState.Components[0];
            Assert.IsTrue(placed.IsFixed, "Fixed placement component should have IsFixed=true");
        }

        // ------------------------------------------------------------------
        // LoadStage — Probe component creates OUT net
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_ProbeFixedComponent_CreatesOutNet()
        {
            var stage = CreateStageDefinition("s-probe", "Probe", "world1", 1, 16);

            var probeDef = CreateComponentDefinition("test_probe", ComponentKind.Probe);
            var placements = new FixedPlacement[]
            {
                new FixedPlacement
                {
                    component = probeDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            };
            SetPrivateField(stage, "_fixedPlacements", placements);

            _stageManager.LoadStage(stage);

            var boardState = _gameManager.BoardState;
            bool hasOutNet = false;
            foreach (var net in boardState.Nets)
            {
                if (net.NetName == "OUT")
                {
                    hasOutNet = true;
                    break;
                }
            }

            Assert.IsTrue(hasOutNet, "LoadStage should create an 'OUT' net for fixed Probe components");
        }

        [Test]
        public void LoadStage_ProbeFixedComponent_ConnectsPinToOutNet()
        {
            var stage = CreateStageDefinition("s-probe-conn", "ProbeConn", "world1", 1, 16);

            var probeDef = CreateComponentDefinition("test_probe_conn", ComponentKind.Probe);
            var placements = new FixedPlacement[]
            {
                new FixedPlacement
                {
                    component = probeDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            };
            SetPrivateField(stage, "_fixedPlacements", placements);

            _stageManager.LoadStage(stage);

            var boardState = _gameManager.BoardState;
            Net outNet = null;
            foreach (var net in boardState.Nets)
            {
                if (net.NetName == "OUT")
                {
                    outNet = net;
                    break;
                }
            }

            Assert.IsNotNull(outNet, "OUT net must exist");
            Assert.Greater(outNet.ConnectedPins.Count, 0,
                "OUT net should have at least 1 connected pin (probe pin 0)");
        }

        [Test]
        public void LoadStage_WithTestCaseAndNoProbeFixedPlacement_PlacesAutoOutputTerminal()
        {
            var stage = CreateStageDefinition("s-auto-output", "AutoOutput", "world1", 1, 16);
            SetPrivateField(stage, "_fixedPlacements", Array.Empty<FixedPlacement>());
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 3.3f, 0.1f, probeNode: null)
            });

            _stageManager.LoadStage(stage);

            string autoProbeId = GetAutoOutputProbeComponentId();
            bool hasAutoProbe = false;
            foreach (var component in _gameManager.BoardState.Components)
            {
                if (component.ComponentDefinitionId == autoProbeId)
                {
                    hasAutoProbe = true;
                    break;
                }
            }

            Assert.IsTrue(hasAutoProbe, "Stage should auto-place an output terminal probe when no probe fixed placement exists.");
        }

        [Test]
        public void LoadStage_WithProbeFixedPlacement_DoesNotAddAutoOutputTerminal()
        {
            var stage = CreateStageDefinition("s-auto-output-skip", "AutoOutputSkip", "world1", 1, 16);
            var probeDef = CreateComponentDefinition("existing_probe", ComponentKind.Probe);
            SetPrivateField(stage, "_fixedPlacements", new[]
            {
                new FixedPlacement
                {
                    component = probeDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            });
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 3.3f, 0.1f, probeNode: "VOUT")
            });

            _stageManager.LoadStage(stage);

            int autoProbeCount = 0;
            string autoProbeId = GetAutoOutputProbeComponentId();
            foreach (var component in _gameManager.BoardState.Components)
            {
                if (component.ComponentDefinitionId == autoProbeId)
                {
                    autoProbeCount++;
                }
            }

            Assert.AreEqual(0, autoProbeCount, "Stage should not auto-place an output terminal when a fixed probe is already configured.");
        }

        // ------------------------------------------------------------------
        // RunSimulationAndEvaluate — guard checks
        // ------------------------------------------------------------------

        [Test]
        public void RunSimulationAndEvaluate_WithNoStageLoaded_LogsWarning()
        {
            // Should not throw; should log a warning. We verify it doesn't crash.
            Assert.DoesNotThrow(() => _stageManager.RunSimulationAndEvaluate(),
                "RunSimulationAndEvaluate should log a warning (not throw) when no stage is loaded");
        }

        [Test]
        public void CollectAdditionalObjectiveFailures_WithoutOutputTerminal_ReturnsFailure()
        {
            var stage = CreateStageDefinition("s-no-output", "NoOutput", "world1", 1, 16);
            SetPrivateField(stage, "_fixedPlacements", Array.Empty<FixedPlacement>());
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 3.0f, 0.1f, probeNode: null)
            });

            var failures = InvokeStageManagerStatic<List<string>>(
                "CollectAdditionalObjectiveFailures",
                stage,
                new List<TestCaseInput>(),
                null);

            Assert.IsNotNull(failures);
            Assert.Greater(failures.Count, 0, "Missing output terminal must produce a stage-clear failure.");
            Assert.That(failures[0], Does.Contain("Output terminal"));
        }

        [Test]
        public void CollectAdditionalObjectiveFailures_DividerOutputNotLessThanInput_ReturnsFailure()
        {
            var stage = CreateStageDefinition("s-divider", "Divider", "world1", 1, 16);

            var sourceDef = CreateComponentDefinition("vsource_5v", ComponentKind.VoltageSource);
            SetPrivateField(sourceDef, "_voltageVolts", 5f);
            SetPrivateField(stage, "_fixedPlacements", new[]
            {
                new FixedPlacement
                {
                    component = sourceDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            });
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 2.5f, 0.1f, probeNode: "VOUT")
            });

            var testInputs = new List<TestCaseInput>
            {
                new("VOUT", 2.5, 0.1)
            };
            var simResult = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);
            simResult.ProbeResults.Add(new ProbeResult("V_VOUT", ProbeType.Voltage, "VOUT", 5.0));

            var failures = InvokeStageManagerStatic<List<string>>(
                "CollectAdditionalObjectiveFailures",
                stage,
                testInputs,
                simResult);

            Assert.IsNotNull(failures);
            Assert.Greater(failures.Count, 0, "Vin <= Vout must fail divider objective.");
            Assert.That(string.Join(" | ", failures), Does.Contain("Vin"));
        }

        [Test]
        public void CollectAdditionalObjectiveFailures_DividerOutputLessThanInput_ReturnsNoFailure()
        {
            var stage = CreateStageDefinition("s-divider-pass", "DividerPass", "world1", 1, 16);

            var sourceDef = CreateComponentDefinition("vsource_5v_pass", ComponentKind.VoltageSource);
            SetPrivateField(sourceDef, "_voltageVolts", 5f);
            SetPrivateField(stage, "_fixedPlacements", new[]
            {
                new FixedPlacement
                {
                    component = sourceDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            });
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 2.5f, 0.1f, probeNode: "VOUT")
            });

            var testInputs = new List<TestCaseInput>
            {
                new("VOUT", 2.5, 0.1)
            };
            var simResult = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);
            simResult.ProbeResults.Add(new ProbeResult("V_VOUT", ProbeType.Voltage, "VOUT", 2.5));

            var failures = InvokeStageManagerStatic<List<string>>(
                "CollectAdditionalObjectiveFailures",
                stage,
                testInputs,
                simResult);

            Assert.IsNotNull(failures);
            Assert.AreEqual(0, failures.Count, "Vin > Vout should pass divider constraint.");
        }

        [Test]
        public void CollectAdditionalObjectiveFailures_DividerStage_TargetEqualsInputVoltage_ReturnsFailure()
        {
            var stage = CreateStageDefinition("s-divider-eq", "Voltage Divider", "world1", 1, 16);

            var sourceDef = CreateComponentDefinition("vsource_5v_eq", ComponentKind.VoltageSource);
            SetPrivateField(sourceDef, "_voltageVolts", 5f);
            SetPrivateField(stage, "_fixedPlacements", new[]
            {
                new FixedPlacement
                {
                    component = sourceDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            });
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 5.0f, 0.1f, probeNode: "VOUT")
            });

            var testInputs = new List<TestCaseInput>
            {
                new("VOUT", 5.0, 0.1)
            };
            var simResult = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);
            simResult.ProbeResults.Add(new ProbeResult("V_VOUT", ProbeType.Voltage, "VOUT", 5.0));

            var failures = InvokeStageManagerStatic<List<string>>(
                "CollectAdditionalObjectiveFailures",
                stage,
                testInputs,
                simResult);

            Assert.IsNotNull(failures);
            Assert.Greater(failures.Count, 0, "Divider stage must fail when Vin == Vout.");
            Assert.That(string.Join(" | ", failures), Does.Contain("Vin"));
        }

        [Test]
        public void CollectAdditionalObjectiveFailures_NonDividerStage_TargetEqualsInputVoltage_ReturnsNoFailure()
        {
            var stage = CreateStageDefinition("s-ohm-eq", "Ohms Law", "world1", 1, 16);

            var sourceDef = CreateComponentDefinition("vsource_5v_ohm", ComponentKind.VoltageSource);
            SetPrivateField(sourceDef, "_voltageVolts", 5f);
            SetPrivateField(stage, "_fixedPlacements", new[]
            {
                new FixedPlacement
                {
                    component = sourceDef,
                    position = new Vector2Int(0, 0),
                    rotation = 0,
                    overrideCustomValue = false,
                    customValue = 0f
                }
            });
            SetPrivateField(stage, "_testCases", new[]
            {
                CreateStageTestCase("Vout", 5.0f, 0.1f, probeNode: "VOUT")
            });

            var testInputs = new List<TestCaseInput>
            {
                new("VOUT", 5.0, 0.1)
            };
            var simResult = SimulationResult.Success(SimulationType.DCOperatingPoint, 0);
            simResult.ProbeResults.Add(new ProbeResult("V_VOUT", ProbeType.Voltage, "VOUT", 5.0));

            var failures = InvokeStageManagerStatic<List<string>>(
                "CollectAdditionalObjectiveFailures",
                stage,
                testInputs,
                simResult);

            Assert.IsNotNull(failures);
            Assert.AreEqual(0, failures.Count, "Non-divider stage should allow Vin == Vout.");
        }

        // ------------------------------------------------------------------
        // LoadStage — successive loads replace CurrentStage
        // ------------------------------------------------------------------

        [Test]
        public void LoadStage_CalledTwice_CurrentStageIsSecondStage()
        {
            var stage1 = CreateStageDefinition("s1", "Stage 1", "world1", 1, 4);
            var stage2 = CreateStageDefinition("s2", "Stage 2", "world1", 2, 9);

            _stageManager.LoadStage(stage1);
            _stageManager.LoadStage(stage2);

            Assert.AreSame(stage2, _stageManager.CurrentStage,
                "CurrentStage should reflect the most recently loaded stage");
        }

        [Test]
        public void LoadStage_CalledTwice_BoardIsResetEachTime()
        {
            var stage1 = CreateStageDefinition("s1", "Stage 1", "world1", 1, 4);
            var stage2 = CreateStageDefinition("s2", "Stage 2", "world1", 2, 16);

            _stageManager.LoadStage(stage1);
            // Place something manually after stage1 load
            _gameManager.BoardState.PlaceComponent(
                "manual",
                new GridPosition(0, 0),
                0,
                new List<PinInstance> { new PinInstance(0, "p0", new GridPosition(0, 0)) });

            _stageManager.LoadStage(stage2);

            // Board should be cleared on second load (manual component gone)
            Assert.AreEqual(0, _gameManager.BoardState.Components.Count,
                "Second LoadStage should reset board, clearing manually placed components");
        }

        // ------------------------------------------------------------------
        // Events — OnStageCompleted is NOT tested here (requires full async sim)
        // OnDRCCompleted is NOT tested here (requires async flow)
        // ------------------------------------------------------------------
    }
}
