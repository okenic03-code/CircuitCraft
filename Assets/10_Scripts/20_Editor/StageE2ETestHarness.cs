using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEditor;
using UnityEngine;

namespace CircuitCraft.Editor
{
    /// <summary>
    /// End-to-end test harness for CircuitCraft stages 1-1 through 1-10.
    /// Builds exact circuit blueprints and runs simulation + scoring for each stage.
    /// </summary>
    public static class StageE2ETestHarness
    {
        // ── GUID Constants (verified from asset database) ──────────────────
        private const string GUID_R_1K = "f6bb8507e14dd9c4d98804edfc204890";
        private const string GUID_R_2K2 = "ebc16846293594d4db0feea5af68ab84";
        private const string GUID_R_10K = "e8da7b3c2a7ca494eaa819665e3eea83";
        private const string GUID_R_4K7 = "7ca4f65367656f449afdd499f69190d9";
        private const string GUID_R_470 = "750562e6434ddb941b62ba6e951fa594";
        private const string GUID_R_220 = "3a80a319f7b98a74abaaf2def5c9363e";
        private const string GUID_V_5V = "66e631d4439b5d34d9b358060a2fd734";
        private const string GUID_V_9V = "cdc6bbd9197b5c04eae917d4e6c7de79";
        private const string GUID_V_12V = "b03b4a56d58e03b4087fc1f1ea898d17";
        private const string GUID_GND = "a59f82134262bd6478d58c4667b5e989";
        private const string GUID_LED_RED = "adbd9050ec83469bbe3f4926739eb08e";

        private const string StageRootPath = "Assets/70_Data/10_ScriptableObjects/60_Stages";
        private const double BatchStageTimeoutSeconds = 90d;
        private static BatchState _batchState;

        // ── Batch State ────────────────────────────────────────────────────
        private sealed class BatchState
        {
            public StageManager StageManager;
            public GameManager GameManager;
            public List<string> StageIds;
            public List<string> Logs = new();
            public int Index;
            public bool WaitingResult;
            public double StartedAt;
            public string CurrentStageId;
        }

        // ── Menu Items: Individual Stages ──────────────────────────────────

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-1")]
        private static void RunStage1_1() => RunSingleStage("1-1");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-1", true)]
        private static bool Validate_RunStage1_1() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-2")]
        private static void RunStage1_2() => RunSingleStage("1-2");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-2", true)]
        private static bool Validate_RunStage1_2() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-3")]
        private static void RunStage1_3() => RunSingleStage("1-3");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-3", true)]
        private static bool Validate_RunStage1_3() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-4")]
        private static void RunStage1_4() => RunSingleStage("1-4");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-4", true)]
        private static bool Validate_RunStage1_4() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-5")]
        private static void RunStage1_5() => RunSingleStage("1-5");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-5", true)]
        private static bool Validate_RunStage1_5() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-6")]
        private static void RunStage1_6() => RunSingleStage("1-6");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-6", true)]
        private static bool Validate_RunStage1_6() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-7")]
        private static void RunStage1_7() => RunSingleStage("1-7");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-7", true)]
        private static bool Validate_RunStage1_7() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-8")]
        private static void RunStage1_8() => RunSingleStage("1-8");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-8", true)]
        private static bool Validate_RunStage1_8() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-9")]
        private static void RunStage1_9() => RunSingleStage("1-9");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-9", true)]
        private static bool Validate_RunStage1_9() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-10")]
        private static void RunStage1_10() => RunSingleStage("1-10");

        [MenuItem("Tools/CircuitCraft/E2E/Run Stage 1-10", true)]
        private static bool Validate_RunStage1_10() => EditorApplication.isPlaying;

        [MenuItem("Tools/CircuitCraft/E2E/Test Junction Dots")]
        private static void TestJunctionDots()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("E2E: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("E2E: StageManager/GameManager not found.");
                return;
            }

            StageDefinition stage = FindStageDefinition("1-1");
            if (stage == null)
            {
                Debug.LogError("E2E: StageDefinition not found for stageId=1-1");
                return;
            }

            stageManager.LoadStage(stage);

            BoardState boardState = gameManager.BoardState;
            var resistor = PlaceComponentByGuid(boardState, GUID_R_1K, new GridPosition(3, 5));
            if (resistor == null)
            {
                Debug.LogError("E2E_JUNCTION_TEST: FAIL, unable to place resistor at (3,5)");
                return;
            }

            Net net = boardState.CreateNet("JUNCTION_TEST_NET");
            var sourcePlus = new GridPosition(1, 9);
            var resistorPinA = new GridPosition(3, 5);
            var expectedJunction = new GridPosition(5, 9);

            boardState.ConnectPinToNet(net.NetId, new PinReference(1, 1, sourcePlus));
            boardState.ConnectPinToNet(net.NetId, new PinReference(resistor.InstanceId, 0, resistorPinA));

            boardState.AddTrace(net.NetId, new GridPosition(1, 9), new GridPosition(5, 9));
            boardState.AddTrace(net.NetId, new GridPosition(5, 9), new GridPosition(5, 5));
            boardState.AddTrace(net.NetId, new GridPosition(5, 9), new GridPosition(8, 9));

            Debug.Log($"E2E_JUNCTION_TEST: expectedPosition=({expectedJunction.X},{expectedJunction.Y})");

            EditorApplication.delayCall += () => VerifyJunctionDots(expectedJunction);
        }

        [MenuItem("Tools/CircuitCraft/E2E/Test Junction Dots", true)]
        private static bool Validate_TestJunctionDots() => EditorApplication.isPlaying;

        // ── Menu Item: Batch Runner ────────────────────────────────────────

        [MenuItem("Tools/CircuitCraft/E2E/Run All World 1 (1-1..1-10)")]
        private static void RunAllWorld1() =>
            RunBatchStages("1-1", "1-2", "1-3", "1-4", "1-5", "1-6", "1-7", "1-8", "1-9", "1-10");

        [MenuItem("Tools/CircuitCraft/E2E/Run All World 1 (1-1..1-10)", true)]
        private static bool Validate_RunAllWorld1() => EditorApplication.isPlaying;

        // ── Core Helpers ───────────────────────────────────────────────────

        private static PlacedComponent PlaceComponentByGuid(BoardState board, string guid, GridPosition pos, int rotation = 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(assetPath);
            if (def == null)
            {
                Debug.LogError($"E2E: Failed to load ComponentDefinition for GUID={guid} at path={assetPath}");
                return null;
            }

            var pins = PinInstanceFactory.CreatePinInstances(def);
            return board.PlaceComponent(def.Id, pos, rotation, pins);
        }

        private static Net ConnectToNet(BoardState board, string netName, PlacedComponent comp, int pinIndex)
        {
            Net net = board.GetNetByName(netName) ?? board.CreateNet(netName);
            GridPosition pinWorld = comp.GetPinWorldPosition(pinIndex);
            board.ConnectPinToNet(net.NetId, new PinReference(comp.InstanceId, pinIndex, pinWorld));
            return net;
        }

        private static PlacedComponent FindPlacedByKind(BoardState board, string definitionId)
        {
            foreach (var component in board.Components)
            {
                if (component.ComponentDefinitionId == definitionId)
                    return component;
            }

            return null;
        }

        private static PlacedComponent FindFixedByDefinitionId(BoardState board, string definitionId)
        {
            foreach (var component in board.Components)
            {
                if (component.IsFixed && component.ComponentDefinitionId == definitionId)
                    return component;
            }

            return null;
        }

        private static string LoadDefinitionId(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(path);
            return def != null ? def.Id : null;
        }

        private static void VerifyJunctionDots(GridPosition expectedJunction)
        {
            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            int junctionCount = 0;
            bool expectedFound = false;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform transform = allTransforms[i];
                if (transform == null || !transform.name.StartsWith("Junction_", StringComparison.Ordinal))
                    continue;

                junctionCount++;
                if (transform.name == $"Junction_{expectedJunction.X}_{expectedJunction.Y}")
                    expectedFound = true;
            }

            bool pass = junctionCount == 1 && expectedFound;
            string result = pass ? "PASS" : "FAIL";
            Debug.Log(
                $"E2E_JUNCTION_TEST: {result}, junctionCount={junctionCount}, expectedPosition=({expectedJunction.X},{expectedJunction.Y})");

            ScreenCapture.CaptureScreenshot("junction_dot_verification.png");
        }

        // ── Single Stage Orchestrator ──────────────────────────────────────

        private static void RunSingleStage(string stageId)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("E2E: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("E2E: StageManager/GameManager not found.");
                return;
            }

            StageDefinition stage = FindStageDefinition(stageId);
            if (stage == null)
            {
                Debug.LogError($"E2E: StageDefinition not found for stageId={stageId}");
                return;
            }

            stageManager.LoadStage(stage);
            Debug.Log($"E2E_STAGE_START: stage={stageId}, name={stage.DisplayName}");

            BoardState board = gameManager.BoardState;

            try
            {
                BuildCircuitForStage(stageId, board);
            }
            catch (Exception ex)
            {
                Debug.LogError($"E2E: BuildCircuit failed for stage={stageId}: {ex.Message}");
                return;
            }

            Action<ScoreBreakdown> onCompleted = null;
            onCompleted = breakdown =>
            {
                stageManager.OnStageCompleted -= onCompleted;
                Debug.Log(
                    $"E2E_STAGE_RESULT: stage={stageId}, passed={breakdown.Passed}, stars={breakdown.Stars}, " +
                    $"score={breakdown.TotalScore}, summary={breakdown.Summary}");
            };

            stageManager.OnStageCompleted += onCompleted;
            stageManager.RunSimulationAndEvaluate();
        }

        private static StageDefinition FindStageDefinition(string stageId)
        {
            string[] guids = AssetDatabase.FindAssets("t:StageDefinition", new[] { StageRootPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
                if (stage != null && stage.StageId == stageId)
                    return stage;
            }

            return null;
        }

        private static void BuildCircuitForStage(string stageId, BoardState board)
        {
            switch (stageId)
            {
                case "1-1": BuildCircuit_1_1(board); break;
                case "1-2": BuildCircuit_1_2(board); break;
                case "1-3": BuildCircuit_1_3(board); break;
                case "1-4": BuildCircuit_1_4(board); break;
                case "1-5": BuildCircuit_1_5(board); break;
                case "1-6": BuildCircuit_1_6(board); break;
                case "1-7": BuildCircuit_1_7(board); break;
                case "1-8": BuildCircuit_1_8(board); break;
                case "1-9": BuildCircuit_1_9(board); break;
                case "1-10": BuildCircuit_1_10(board); break;
                default:
                    Debug.LogError($"E2E: No circuit blueprint for stage {stageId}");
                    break;
            }

            Debug.Log($"E2E_STAGE_PLACED: stage={stageId}, components={board.Components.Count}, traces={board.Traces.Count}");
        }

        // ── Stage Blueprints ───────────────────────────────────────────────

        // Stage 1-1 (Ohm's Law): Fixed V5V@(1,8) + GND@(1,0)
        // Place: R1(1kΩ)@(0,5), R2(1kΩ)@(0,3)
        // Probe: NET1 at R1.B→R2.A junction (2.5V)
        private static void BuildCircuit_1_1(BoardState board)
        {
            string v5vDefId = LoadDefinitionId(GUID_V_5V);
            string gndDefId = LoadDefinitionId(GUID_GND);
            var v5v = FindFixedByDefinitionId(board, v5vDefId);
            var gnd = FindFixedByDefinitionId(board, gndDefId);

            var r1 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 5));
            var r2 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 3));

            // VIN: V5V.pin1(+) → R1.pin0(A)
            Net vinNet = ConnectToNet(board, "VIN", v5v, 1);
            ConnectToNet(board, "VIN", r1, 0);

            // NET1: R1.pin1(B) → R2.pin0(A) — PROBE TARGET
            Net net1 = ConnectToNet(board, "NET1", r1, 1);
            ConnectToNet(board, "NET1", r2, 0);

            // GND (net 0): R2.pin1(B) → GND.pin0, V5V.pin0(−) → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r2, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v5v, 0);
        }

        // Stage 1-2 (Series Resistors): Fixed V9V@(1,8) + GND@(1,0)
        // Place: R1(1kΩ)@(0,5), R2(2.2kΩ)@(0,3)
        // Probe: NET2 at R1.B→R2.A junction (6.19V)
        private static void BuildCircuit_1_2(BoardState board)
        {
            string v9vDefId = LoadDefinitionId(GUID_V_9V);
            string gndDefId = LoadDefinitionId(GUID_GND);
            var v9v = FindFixedByDefinitionId(board, v9vDefId);
            var gnd = FindFixedByDefinitionId(board, gndDefId);

            var r1 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 5));
            var r2 = PlaceComponentByGuid(board, GUID_R_2K2, new GridPosition(0, 3));

            // VIN: V9V.pin1 → R1.pin0
            Net vinNet = ConnectToNet(board, "VIN", v9v, 1);
            ConnectToNet(board, "VIN", r1, 0);

            // NET2: R1.pin1 → R2.pin0 — PROBE
            Net net2 = ConnectToNet(board, "NET2", r1, 1);
            ConnectToNet(board, "NET2", r2, 0);

            // GND (net 0): R2.pin1 → GND.pin0, V9V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r2, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v9v, 0);
        }

        // Stage 1-3 (Voltage Divider): Fixed V12V@(1,8) + GND@(1,0)
        // Place: R1(10kΩ)@(0,5), R2(4.7kΩ)@(0,3)
        // Probe: NET2 (3.84V)
        private static void BuildCircuit_1_3(BoardState board)
        {
            string v12vDefId = LoadDefinitionId(GUID_V_12V);
            string gndDefId = LoadDefinitionId(GUID_GND);
            var v12v = FindFixedByDefinitionId(board, v12vDefId);
            var gnd = FindFixedByDefinitionId(board, gndDefId);

            var r1 = PlaceComponentByGuid(board, GUID_R_10K, new GridPosition(0, 5));
            var r2 = PlaceComponentByGuid(board, GUID_R_4K7, new GridPosition(0, 3));

            // VIN: V12V.pin1 → R1.pin0
            Net vinNet = ConnectToNet(board, "VIN", v12v, 1);
            ConnectToNet(board, "VIN", r1, 0);

            // NET2: R1.pin1 → R2.pin0 — PROBE
            Net net2 = ConnectToNet(board, "NET2", r1, 1);
            ConnectToNet(board, "NET2", r2, 0);

            // GND (net 0): R2.pin1 → GND.pin0, V12V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r2, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v12v, 0);
        }

        // Stage 1-4 (Parallel Resistors): Fixed V5V@(1,8) + GND@(1,0)
        // Place: R_ser(470Ω)@(0,6), R_p1(1kΩ)@(0,4), R_p2(2.2kΩ)@(2,4)
        // Probe: NET2 at R_ser.B→R_p1.A/R_p2.A junction (2.97V)
        private static void BuildCircuit_1_4(BoardState board)
        {
            string v5vDefId = LoadDefinitionId(GUID_V_5V);
            string gndDefId = LoadDefinitionId(GUID_GND);
            var v5v = FindFixedByDefinitionId(board, v5vDefId);
            var gnd = FindFixedByDefinitionId(board, gndDefId);

            var rSer = PlaceComponentByGuid(board, GUID_R_470, new GridPosition(0, 6));
            var rP1 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 4));
            var rP2 = PlaceComponentByGuid(board, GUID_R_2K2, new GridPosition(2, 4));

            // VIN: V5V.pin1 → R_ser.pin0
            Net vinNet = ConnectToNet(board, "VIN", v5v, 1);
            ConnectToNet(board, "VIN", rSer, 0);

            // NET2: R_ser.pin1 → R_p1.pin0 AND R_p2.pin0 — PROBE
            Net net2 = ConnectToNet(board, "NET2", rSer, 1);
            ConnectToNet(board, "NET2", rP1, 0);
            ConnectToNet(board, "NET2", rP2, 0);

            // GND (net 0): R_p1.pin1 → GND.pin0, R_p2.pin1 → GND.pin0, V5V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", rP1, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", rP2, 1);
            ConnectToNet(board, "0", v5v, 0);
        }

        // Stage 1-5 (Series-Parallel): Fixed V9V@(1,8) + GND@(1,0)
        // Place: R_ser(1kΩ)@(0,6), R_p1(2.2kΩ)@(0,4), R_p2(4.7kΩ)@(2,4)
        // Probe: NET2 (5.40V)
        private static void BuildCircuit_1_5(BoardState board)
        {
            string v9vDefId = LoadDefinitionId(GUID_V_9V);
            string gndDefId = LoadDefinitionId(GUID_GND);
            var v9v = FindFixedByDefinitionId(board, v9vDefId);
            var gnd = FindFixedByDefinitionId(board, gndDefId);

            var rSer = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 6));
            var rP1 = PlaceComponentByGuid(board, GUID_R_2K2, new GridPosition(0, 4));
            var rP2 = PlaceComponentByGuid(board, GUID_R_4K7, new GridPosition(2, 4));

            // VIN: V9V.pin1 → R_ser.pin0
            Net vinNet = ConnectToNet(board, "VIN", v9v, 1);
            ConnectToNet(board, "VIN", rSer, 0);

            // NET2: R_ser.pin1 → R_p1.pin0 AND R_p2.pin0 — PROBE
            Net net2 = ConnectToNet(board, "NET2", rSer, 1);
            ConnectToNet(board, "NET2", rP1, 0);
            ConnectToNet(board, "NET2", rP2, 0);

            // GND (net 0): R_p1.pin1 → GND.pin0, R_p2.pin1 → GND.pin0, V9V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", rP1, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", rP2, 1);
            ConnectToNet(board, "0", v9v, 0);
        }

        // Stage 1-6 (LED Current Limit): NO FIXED — place ALL
        // Place: V5V@(1,8), GND@(1,0), LED_Red@(0,6), R_470@(0,4)
        // Probe: V_R at LED.Cathode→R_470.A (≈3.0V)
        private static void BuildCircuit_1_6(BoardState board)
        {
            var v5v = PlaceComponentByGuid(board, GUID_V_5V, new GridPosition(1, 8));
            var gnd = PlaceComponentByGuid(board, GUID_GND, new GridPosition(1, 0));
            var led = PlaceComponentByGuid(board, GUID_LED_RED, new GridPosition(0, 6));
            var r470 = PlaceComponentByGuid(board, GUID_R_470, new GridPosition(0, 4));

            // VIN: V5V.pin1(+) → LED.pin0(Anode)
            Net vinNet = ConnectToNet(board, "VIN", v5v, 1);
            ConnectToNet(board, "VIN", led, 0);

            // V_R: LED.pin1(Cathode) → R_470.pin0 — PROBE
            Net vrNet = ConnectToNet(board, "V_R", led, 1);
            ConnectToNet(board, "V_R", r470, 0);

            // GND (net 0): R_470.pin1 → GND.pin0, V5V.pin0(−) → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r470, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v5v, 0);
        }

        // Stage 1-7 (Series LED): NO FIXED
        // Place: V9V@(1,8), GND@(1,0), LED1@(0,6), LED2@(0,5), R_220@(0,3)
        // Probe: V_R at LED2.Cathode→R_220.A (≈5.0V)
        private static void BuildCircuit_1_7(BoardState board)
        {
            var v9v = PlaceComponentByGuid(board, GUID_V_9V, new GridPosition(1, 8));
            var gnd = PlaceComponentByGuid(board, GUID_GND, new GridPosition(1, 0));
            var led1 = PlaceComponentByGuid(board, GUID_LED_RED, new GridPosition(0, 6));
            var led2 = PlaceComponentByGuid(board, GUID_LED_RED, new GridPosition(0, 5));
            var r220 = PlaceComponentByGuid(board, GUID_R_220, new GridPosition(0, 3));

            // VIN: V9V.pin1(+) → LED1.pin0(Anode)
            Net vinNet = ConnectToNet(board, "VIN", v9v, 1);
            ConnectToNet(board, "VIN", led1, 0);

            // VMID: LED1.pin1(Cathode) → LED2.pin0(Anode)
            Net vmidNet = ConnectToNet(board, "VMID", led1, 1);
            ConnectToNet(board, "VMID", led2, 0);

            // V_R: LED2.pin1(Cathode) → R_220.pin0 — PROBE
            Net vrNet = ConnectToNet(board, "V_R", led2, 1);
            ConnectToNet(board, "V_R", r220, 0);

            // GND (net 0): R_220.pin1 → GND.pin0, V9V.pin0(−) → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r220, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v9v, 0);
        }

        // Stage 1-8 (Two Source Series): NO FIXED
        // Place: GND@(1,0), V5V@(1,3), V9V@(1,6), R_1k@(0,9)
        // Probe: V_total at V9V.pin1(+)→R_1k.pin0 (14.0V)
        private static void BuildCircuit_1_8(BoardState board)
        {
            var gnd = PlaceComponentByGuid(board, GUID_GND, new GridPosition(1, 0));
            var v5v = PlaceComponentByGuid(board, GUID_V_5V, new GridPosition(1, 3));
            var v9v = PlaceComponentByGuid(board, GUID_V_9V, new GridPosition(1, 6));
            var r1k = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 9));

            // GND (net 0): GND.pin0 → V5V.pin0(−)
            Net vgndNet = ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v5v, 0);

            // VMID: V5V.pin1(+) → V9V.pin0(−)
            Net vmidNet = ConnectToNet(board, "VMID", v5v, 1);
            ConnectToNet(board, "VMID", v9v, 0);

            // V_total: V9V.pin1(+) → R_1k.pin0 — PROBE
            Net vtotalNet = ConnectToNet(board, "V_total", v9v, 1);
            ConnectToNet(board, "V_total", r1k, 0);

            // GND (net 0): R_1k.pin1 → GND.pin0 (same net as net 0)
            ConnectToNet(board, "0", r1k, 1);
        }

        // Stage 1-9 (Ground Reference): NO FIXED
        // Place: V12V@(1,8), GND@(1,0), R1(1kΩ)@(0,5), R2(1kΩ)@(0,3)
        // Probe: V_mid at R1.B→R2.A (6.0V)
        private static void BuildCircuit_1_9(BoardState board)
        {
            var v12v = PlaceComponentByGuid(board, GUID_V_12V, new GridPosition(1, 8));
            var gnd = PlaceComponentByGuid(board, GUID_GND, new GridPosition(1, 0));
            var r1 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 5));
            var r2 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 3));

            // VIN: V12V.pin1 → R1.pin0
            Net vinNet = ConnectToNet(board, "VIN", v12v, 1);
            ConnectToNet(board, "VIN", r1, 0);

            // V_mid: R1.pin1 → R2.pin0 — PROBE
            Net vmidNet = ConnectToNet(board, "V_mid", r1, 1);
            ConnectToNet(board, "V_mid", r2, 0);

            // GND (net 0): R2.pin1 → GND.pin0, V12V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r2, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v12v, 0);
        }

        // Stage 1-10 (Triple Divider): NO FIXED
        // Place: V9V@(1,8), GND@(1,0), R1(1kΩ)@(0,6), R2(1kΩ)@(0,4), R3(1kΩ)@(0,2)
        // Probe: V2 at R2.B→R3.A (3.0V)
        private static void BuildCircuit_1_10(BoardState board)
        {
            var v9v = PlaceComponentByGuid(board, GUID_V_9V, new GridPosition(1, 8));
            var gnd = PlaceComponentByGuid(board, GUID_GND, new GridPosition(1, 0));
            var r1 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 6));
            var r2 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 4));
            var r3 = PlaceComponentByGuid(board, GUID_R_1K, new GridPosition(0, 2));

            // VIN: V9V.pin1 → R1.pin0
            Net vinNet = ConnectToNet(board, "VIN", v9v, 1);
            ConnectToNet(board, "VIN", r1, 0);

            // VMID1: R1.pin1 → R2.pin0
            Net vmid1Net = ConnectToNet(board, "VMID1", r1, 1);
            ConnectToNet(board, "VMID1", r2, 0);

            // V2: R2.pin1 → R3.pin0 — PROBE
            Net v2Net = ConnectToNet(board, "V2", r2, 1);
            ConnectToNet(board, "V2", r3, 0);

            // GND (net 0): R3.pin1 → GND.pin0, V9V.pin0 → GND.pin0
            Net vgndNet = ConnectToNet(board, "0", r3, 1);
            ConnectToNet(board, "0", gnd, 0);
            ConnectToNet(board, "0", v9v, 0);
        }

        // ── Batch Runner ───────────────────────────────────────────────────

        private static void RunBatchStages(params string[] stageIds)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("E2E: Enter Play Mode first.");
                return;
            }

            if (_batchState != null)
            {
                Debug.LogWarning("E2E: Batch run is already in progress.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("E2E: StageManager/GameManager not found.");
                return;
            }

            _batchState = new BatchState
            {
                StageManager = stageManager,
                GameManager = gameManager,
                StageIds = new List<string>(stageIds),
                Index = 0,
                WaitingResult = false
            };

            stageManager.OnStageCompleted += HandleBatchCompleted;
            EditorApplication.update += UpdateBatch;

            Debug.Log($"E2E_BATCH_START: Running {stageIds.Length} stage(s) (1-1..1-10).");
        }

        private static void UpdateBatch()
        {
            var state = _batchState;
            if (state == null)
                return;

            if (state.WaitingResult)
            {
                double elapsed = EditorApplication.timeSinceStartup - state.StartedAt;
                if (elapsed > BatchStageTimeoutSeconds)
                {
                    Debug.LogError($"E2E: Timeout waiting for stage result ({state.CurrentStageId}).");
                    CompleteBatch(aborted: true);
                }

                return;
            }

            if (state.Index >= state.StageIds.Count)
            {
                CompleteBatch(aborted: false);
                return;
            }

            string stageId = state.StageIds[state.Index];
            state.CurrentStageId = stageId;

            StageDefinition stage = FindStageDefinition(stageId);
            if (stage == null)
            {
                string errLine =
                    $"E2E_STAGE_RESULT: stage={stageId}, passed=False, stars=0, score=0, summary=StageDefinition not found";
                state.Logs.Add(errLine);
                Debug.LogError(errLine);
                state.Index++;
                return;
            }

            state.StageManager.LoadStage(stage);
            Debug.Log($"E2E_STAGE_START: stage={stageId}, name={stage.DisplayName}");

            BoardState board = state.GameManager.BoardState;

            try
            {
                BuildCircuitForStage(stageId, board);
            }
            catch (Exception ex)
            {
                string failLine =
                    $"E2E_STAGE_RESULT: stage={stageId}, passed=False, stars=0, score=0, summary=Build failed: {ex.Message}";
                state.Logs.Add(failLine);
                Debug.LogError(failLine);
                state.WaitingResult = false;
                state.Index++;
                return;
            }

            state.WaitingResult = true;
            state.StartedAt = EditorApplication.timeSinceStartup;

            state.StageManager.RunSimulationAndEvaluate();
        }

        private static void HandleBatchCompleted(ScoreBreakdown breakdown)
        {
            var state = _batchState;
            if (state == null || !state.WaitingResult || state.Index >= state.StageIds.Count)
                return;

            string stageId = state.StageIds[state.Index];
            string line =
                $"E2E_STAGE_RESULT: stage={stageId}, passed={breakdown.Passed}, stars={breakdown.Stars}, " +
                $"score={breakdown.TotalScore}, summary={breakdown.Summary}";
            state.Logs.Add(line);
            Debug.Log(line);

            state.WaitingResult = false;
            state.Index++;
        }

        private static void CompleteBatch(bool aborted)
        {
            var state = _batchState;
            if (state == null)
                return;

            state.StageManager.OnStageCompleted -= HandleBatchCompleted;
            EditorApplication.update -= UpdateBatch;

            int passCount = 0;
            int failCount = 0;
            foreach (string line in state.Logs)
            {
                if (line.Contains("passed=True"))
                    passCount++;
                else
                    failCount++;
            }

            // Account for stages not yet reached (timeout/abort)
            int notRun = state.StageIds.Count - state.Logs.Count;
            failCount += notRun;

            Debug.Log($"E2E_BATCH_SUMMARY: total={state.StageIds.Count}, passed={passCount}, failed={failCount}");
            foreach (string line in state.Logs)
            {
                Debug.Log(line);
            }

            _batchState = null;
        }
    }
}
