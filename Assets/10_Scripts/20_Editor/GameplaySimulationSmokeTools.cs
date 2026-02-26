using System;
using System.Collections.Generic;
using CircuitCraft.Commands;
using CircuitCraft.Controllers;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEditor;
using UnityEngine;

namespace CircuitCraft.Editor
{
    /// <summary>
    /// Editor smoke helpers for quickly validating gameplay wiring and stage simulation.
    /// </summary>
    public static class GameplaySimulationSmokeTools
    {
        private const string SmokeMenuPath = "Tools/CircuitCraft/Smoke/Auto Wire Current Stage + Simulate";
        private const string World1BatchMenuPath = "Tools/CircuitCraft/Smoke/Validate Stage 1-1..1-10";
        private const string World1LoadOnlyMenuPath = "Tools/CircuitCraft/Smoke/Validate Stage 1-1..1-10 (Load Only)";
        private const string World1GameplayBasicMenuPath = "Tools/CircuitCraft/Smoke/Validate Stage 1-1..1-10 (Gameplay Basic)";
        private const string InputInteractionMenuPath = "Tools/CircuitCraft/Smoke/Validate Input Interactions (Rotate+Joint)";
        private const double BatchStageTimeoutSeconds = 90d;
        private static BatchValidationState _batchValidationState;

        private sealed class BatchValidationState
        {
            public StageManager StageManager;
            public GameManager GameManager;
            public List<StageDefinition> Stages;
            public List<string> Logs = new();
            public int Index;
            public bool WaitingResult;
            public double StartedAt;
            public string CurrentStageId;
            public bool CurrentAutoWired;
        }

        [MenuItem(SmokeMenuPath)]
        private static void AutoWireCurrentStageAndSimulate()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("SMOKE_STAGE_ERROR: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("SMOKE_STAGE_ERROR: StageManager/GameManager not found.");
                return;
            }

            StageDefinition stage = stageManager.CurrentStage;
            if (stage == null)
            {
                Debug.LogError("SMOKE_STAGE_ERROR: No current stage loaded.");
                return;
            }

            // Reset to a known clean stage state before auto-wiring.
            stageManager.LoadStage(stage);
            if (!TryAutoWireSimpleSourceGroundResistor(gameManager, stage, out string wireMessage))
            {
                Debug.LogError($"SMOKE_STAGE_ERROR: {wireMessage}");
                return;
            }

            Action<ScoreBreakdown> onCompleted = null;
            onCompleted = breakdown =>
            {
                stageManager.OnStageCompleted -= onCompleted;
                Debug.Log(
                    $"SMOKE_STAGE_RESULT: passed={breakdown.Passed}, stars={breakdown.Stars}, score={breakdown.TotalScore}, summary={breakdown.Summary}");
            };

            stageManager.OnStageCompleted += onCompleted;
            stageManager.RunSimulationAndEvaluate();
            Debug.Log("SMOKE_STAGE_RUN: Auto wiring submitted. Awaiting stage completion.");
        }

        [MenuItem(SmokeMenuPath, true)]
        private static bool ValidateAutoWireCurrentStageAndSimulate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(World1BatchMenuPath)]
        private static void ValidateWorld1Stage1To10()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("SMOKE_WORLD1_ERROR: Enter Play Mode first.");
                return;
            }

            if (_batchValidationState != null)
            {
                Debug.LogWarning("SMOKE_WORLD1_WARN: Batch validation is already running.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("SMOKE_WORLD1_ERROR: StageManager/GameManager not found.");
                return;
            }

            List<StageDefinition> stages = LoadWorld1StageRange(1, 10);
            if (stages.Count == 0)
            {
                Debug.LogError("SMOKE_WORLD1_ERROR: No StageDefinition assets found for world1 stage 1..10.");
                return;
            }

            _batchValidationState = new BatchValidationState
            {
                StageManager = stageManager,
                GameManager = gameManager,
                Stages = stages,
                Index = 0,
                WaitingResult = false
            };

            stageManager.OnStageCompleted += HandleBatchStageCompleted;
            EditorApplication.update += UpdateBatchValidation;

            Debug.Log($"SMOKE_WORLD1_START: validating {stages.Count} stage(s) (1-1..1-10).");
        }

        [MenuItem(World1BatchMenuPath, true)]
        private static bool ValidateWorld1Stage1To10_Validate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(World1LoadOnlyMenuPath)]
        private static void ValidateWorld1Stage1To10LoadOnly()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("SMOKE_WORLD1_LOAD_ERROR: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("SMOKE_WORLD1_LOAD_ERROR: StageManager/GameManager not found.");
                return;
            }

            List<StageDefinition> stages = LoadWorld1StageRange(1, 10);
            if (stages.Count == 0)
            {
                Debug.LogError("SMOKE_WORLD1_LOAD_ERROR: No StageDefinition assets found for world1 stage 1..10.");
                return;
            }

            int loadedCount = 0;
            int failedCount = 0;

            foreach (StageDefinition stage in stages)
            {
                try
                {
                    stageManager.LoadStage(stage);
                    BoardState board = gameManager.BoardState;
                    int componentCount = board?.Components?.Count ?? -1;
                    bool hasSource = FindFixedDefinition(stage.FixedPlacements, ComponentKind.VoltageSource) != null;
                    bool hasGround = FindFixedDefinition(stage.FixedPlacements, ComponentKind.Ground) != null;
                    bool hasAllowedResistor = FindAllowedDefinition(stage.AllowedComponents, ComponentKind.Resistor) != null;

                    Debug.Log(
                        $"SMOKE_WORLD1_LOAD_RESULT: stage={stage.StageId}, name={stage.DisplayName}, loaded=True, components={componentCount}, fixedSource={hasSource}, fixedGround={hasGround}, allowedResistor={hasAllowedResistor}");
                    loadedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SMOKE_WORLD1_LOAD_RESULT: stage={stage.StageId}, loaded=False, error={ex.Message}");
                    failedCount++;
                }
            }

            Debug.Log($"SMOKE_WORLD1_LOAD_SUMMARY: total={stages.Count}, loaded={loadedCount}, failed={failedCount}");
        }

        [MenuItem(World1LoadOnlyMenuPath, true)]
        private static bool ValidateWorld1Stage1To10LoadOnly_Validate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(World1GameplayBasicMenuPath)]
        private static void ValidateWorld1Stage1To10GameplayBasic()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("SMOKE_WORLD1_PLAY_ERROR: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("SMOKE_WORLD1_PLAY_ERROR: StageManager/GameManager not found.");
                return;
            }

            List<StageDefinition> stages = LoadWorld1StageRange(1, 10);
            if (stages.Count == 0)
            {
                Debug.LogError("SMOKE_WORLD1_PLAY_ERROR: No StageDefinition assets found for world1 stage 1..10.");
                return;
            }

            int okCount = 0;
            int failCount = 0;

            foreach (StageDefinition stage in stages)
            {
                bool loaded = false;
                bool placed = false;
                bool removed = false;
                string info = "ok";

                try
                {
                    stageManager.LoadStage(stage);
                    BoardState board = gameManager.BoardState;
                    loaded = board != null;

                    if (!loaded)
                    {
                        info = "BoardState null after LoadStage.";
                    }
                    else
                    {
                        ComponentDefinition placeDef = FindPreferredPlaceableDefinition(stage.AllowedComponents);
                        if (placeDef == null)
                        {
                            info = "No allowed component available for placement.";
                        }
                        else
                        {
                            GridPosition placement = FindFirstFreeCell(board);
                            var pins = PinInstanceFactory.CreatePinInstances(placeDef);
                            if (pins.Count == 0)
                            {
                                info = $"Pins missing for '{placeDef.Id}'.";
                            }
                            else
                            {
                                PlacedComponent placedComponent = board.PlaceComponent(placeDef.Id, placement, RotationConstants.None, pins);
                                placed = placedComponent != null;
                                if (!placed)
                                {
                                    info = $"PlaceComponent returned null for '{placeDef.Id}'.";
                                }
                                else
                                {
                                    removed = board.RemoveComponent(placedComponent.InstanceId);
                                    if (!removed)
                                    {
                                        info = $"RemoveComponent failed for instance {placedComponent.InstanceId}.";
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    info = ex.Message;
                }

                bool stageOk = loaded && placed && removed;
                if (stageOk)
                {
                    okCount++;
                }
                else
                {
                    failCount++;
                }

                Debug.Log(
                    $"SMOKE_WORLD1_PLAY_RESULT: stage={stage.StageId}, name={stage.DisplayName}, loaded={loaded}, placed={placed}, removed={removed}, ok={stageOk}, info={info}");
            }

            Debug.Log($"SMOKE_WORLD1_PLAY_SUMMARY: total={stages.Count}, ok={okCount}, failed={failCount}");
        }

        [MenuItem(World1GameplayBasicMenuPath, true)]
        private static bool ValidateWorld1Stage1To10GameplayBasic_Validate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(InputInteractionMenuPath)]
        private static void ValidateInputInteractionsRotateAndJoint()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("SMOKE_INPUT_ERROR: Enter Play Mode first.");
                return;
            }

            StageManager stageManager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (stageManager == null || gameManager == null)
            {
                Debug.LogError("SMOKE_INPUT_ERROR: StageManager/GameManager not found.");
                return;
            }

            StageDefinition stage = stageManager.CurrentStage;
            if (stage == null)
            {
                Debug.LogError("SMOKE_INPUT_ERROR: No current stage loaded.");
                return;
            }

            stageManager.LoadStage(stage);
            if (!TryAutoWireSimpleSourceGroundResistor(gameManager, stage, out string wireMessage))
            {
                Debug.LogError($"SMOKE_INPUT_ERROR: {wireMessage}");
                return;
            }

            BoardState board = gameManager.BoardState;
            CommandHistory history = gameManager.CommandHistory;
            if (board == null || history == null)
            {
                Debug.LogError("SMOKE_INPUT_ERROR: BoardState/CommandHistory is null.");
                return;
            }

            bool jointOk = ValidateTraceJointByCommandPath(board, history, stage, out string jointInfo);
            bool rotateOk = ValidateRotateByCommandPath(board, history, out string rotateInfo);
            bool fixedMoveOk = ValidateFixedComponentMoveByCommandPath(board, history, out string fixedMoveInfo);

            Debug.Log($"SMOKE_INPUT_RESULT: fixedMoveOk={fixedMoveOk}, rotateOk={rotateOk}, jointOk={jointOk}, fixedMoveInfo={fixedMoveInfo}, rotateInfo={rotateInfo}, jointInfo={jointInfo}");
        }

        [MenuItem(InputInteractionMenuPath, true)]
        private static bool ValidateInputInteractionsRotateAndJoint_Validate()
        {
            return EditorApplication.isPlaying;
        }

        private static void UpdateBatchValidation()
        {
            var state = _batchValidationState;
            if (state == null)
            {
                return;
            }

            if (state.WaitingResult)
            {
                double elapsed = EditorApplication.timeSinceStartup - state.StartedAt;
                if (elapsed > BatchStageTimeoutSeconds)
                {
                    Debug.LogError($"SMOKE_WORLD1_ERROR: Timeout waiting stage result ({state.CurrentStageId}).");
                    CompleteBatchValidation(aborted: true);
                }

                return;
            }

            if (state.Index >= state.Stages.Count)
            {
                CompleteBatchValidation(aborted: false);
                return;
            }

            StageDefinition stage = state.Stages[state.Index];
            state.CurrentStageId = stage.StageId;
            state.StageManager.LoadStage(stage);

            bool autoWired = TryAutoWireSimpleSourceGroundResistor(
                state.GameManager,
                stage,
                out string wireMessage);
            state.CurrentAutoWired = autoWired;
            state.WaitingResult = true;
            state.StartedAt = EditorApplication.timeSinceStartup;

            Debug.Log(
                $"SMOKE_WORLD1_RUN: stage={stage.StageId}, name={stage.DisplayName}, autoWired={autoWired}, info={wireMessage}");

            state.StageManager.RunSimulationAndEvaluate();
        }

        private static void HandleBatchStageCompleted(ScoreBreakdown breakdown)
        {
            var state = _batchValidationState;
            if (state == null || !state.WaitingResult || state.Index >= state.Stages.Count)
            {
                return;
            }

            StageDefinition stage = state.Stages[state.Index];
            string line =
                $"SMOKE_WORLD1_RESULT: stage={stage.StageId}, passed={breakdown.Passed}, stars={breakdown.Stars}, score={breakdown.TotalScore}, autoWired={state.CurrentAutoWired}, summary={breakdown.Summary}";
            state.Logs.Add(line);
            Debug.Log(line);

            state.WaitingResult = false;
            state.Index++;
        }

        private static void CompleteBatchValidation(bool aborted)
        {
            var state = _batchValidationState;
            if (state == null)
            {
                return;
            }

            state.StageManager.OnStageCompleted -= HandleBatchStageCompleted;
            EditorApplication.update -= UpdateBatchValidation;

            int passCount = 0;
            foreach (string line in state.Logs)
            {
                if (line.Contains("passed=True"))
                {
                    passCount++;
                }
            }

            Debug.Log(
                $"SMOKE_WORLD1_SUMMARY: completed={state.Logs.Count}/{state.Stages.Count}, passed={passCount}, aborted={aborted}");
            foreach (string line in state.Logs)
            {
                Debug.Log(line);
            }

            _batchValidationState = null;
        }

        private static List<StageDefinition> LoadWorld1StageRange(int minStageNumber, int maxStageNumber)
        {
            string[] guids = AssetDatabase.FindAssets("t:StageDefinition", new[] { "Assets/70_Data/10_ScriptableObjects/60_Stages" });
            var stages = new List<StageDefinition>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageDefinition stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
                if (stage == null)
                {
                    continue;
                }

                if (!string.Equals(stage.WorldId, "world1", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (stage.StageNumber < minStageNumber || stage.StageNumber > maxStageNumber)
                {
                    continue;
                }

                stages.Add(stage);
            }

            stages.Sort((a, b) => a.StageNumber.CompareTo(b.StageNumber));
            return stages;
        }

        private static ComponentDefinition FindPreferredPlaceableDefinition(ComponentDefinition[] definitions)
        {
            ComponentDefinition resistor = FindAllowedDefinition(definitions, ComponentKind.Resistor);
            if (resistor != null)
            {
                return resistor;
            }

            if (definitions == null)
            {
                return null;
            }

            foreach (var def in definitions)
            {
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        private static GridPosition FindFirstFreeCell(BoardState board)
        {
            BoardBounds bounds = board.SuggestedBounds;

            for (int y = bounds.MinY; y < bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x < bounds.MaxX; x++)
                {
                    var pos = new GridPosition(x, y);
                    if (!board.IsPositionOccupied(pos))
                    {
                        return pos;
                    }
                }
            }

            return new GridPosition(bounds.MinX, bounds.MinY);
        }

        private static bool TryAutoWireSimpleSourceGroundResistor(
            GameManager gameManager,
            StageDefinition stage,
            out string message)
        {
            message = "ok";

            BoardState board = gameManager.BoardState;
            if (board == null)
            {
                message = "BoardState is null.";
                return false;
            }

            ComponentDefinition sourceDef = FindFixedDefinition(stage.FixedPlacements, ComponentKind.VoltageSource);
            ComponentDefinition groundDef = FindFixedDefinition(stage.FixedPlacements, ComponentKind.Ground);
            ComponentDefinition resistorDef = FindAllowedDefinition(stage.AllowedComponents, ComponentKind.Resistor);

            if (sourceDef == null || groundDef == null || resistorDef == null)
            {
                message = "Required source/ground/resistor definitions not found.";
                return false;
            }

            PlacedComponent source = FindPlacedComponentByDefinitionId(board, sourceDef.Id);
            PlacedComponent ground = FindPlacedComponentByDefinitionId(board, groundDef.Id);
            if (source == null || ground == null)
            {
                message = "Required fixed components not found on board.";
                return false;
            }

            GridPosition resistorPos = FindSafePlacement(board, source.Position, ground.Position);
            List<PinInstance> resistorPins = PinInstanceFactory.CreatePinInstances(resistorDef);
            PlacedComponent resistor = board.PlaceComponent(resistorDef.Id, resistorPos, RotationConstants.None, resistorPins);

            int sourcePlusPin = FindPinIndex(source, "+", "positive");
            if (sourcePlusPin < 0)
            {
                sourcePlusPin = FindPinByExtremeWorldY(source, pickMax: true);
            }

            int sourceMinusPin = FindPinIndex(source, "-", "negative");
            if (sourceMinusPin < 0 || sourceMinusPin == sourcePlusPin)
            {
                sourceMinusPin = FindPinByExtremeWorldY(source, pickMax: false);
            }

            int groundPin = 0;
            int resistorPinA = 0;
            int resistorPinB = resistor.Pins.Count > 1 ? 1 : 0;

            // Route order matters: first route creates NET1 (stage probe target).
            RoutePins(gameManager.CommandHistory, board, source, sourcePlusPin, resistor, resistorPinA);
            RoutePins(gameManager.CommandHistory, board, resistor, resistorPinB, ground, groundPin);
            RoutePins(gameManager.CommandHistory, board, source, sourceMinusPin, ground, groundPin);

            message = "auto-wired source-resistor-ground path.";
            return true;
        }

        private static void RoutePins(
            CommandHistory history,
            BoardState board,
            PlacedComponent startComponent,
            int startPinIndex,
            PlacedComponent endComponent,
            int endPinIndex)
        {
            GridPosition start = startComponent.GetPinWorldPosition(startPinIndex);
            GridPosition end = endComponent.GetPinWorldPosition(endPinIndex);

            var command = new RouteTraceCommand(
                board,
                new PinReference(startComponent.InstanceId, startPinIndex, start),
                new PinReference(endComponent.InstanceId, endPinIndex, end),
                WirePathCalculator.BuildManhattanSegments(start, end));

            history.ExecuteCommand(command);
        }

        private static ComponentDefinition FindFixedDefinition(FixedPlacement[] placements, ComponentKind kind)
        {
            if (placements == null)
            {
                return null;
            }

            foreach (var placement in placements)
            {
                if (placement.component != null && placement.component.Kind == kind)
                {
                    return placement.component;
                }
            }

            return null;
        }

        private static ComponentDefinition FindAllowedDefinition(ComponentDefinition[] definitions, ComponentKind kind)
        {
            if (definitions == null)
            {
                return null;
            }

            foreach (var definition in definitions)
            {
                if (definition != null && definition.Kind == kind)
                {
                    return definition;
                }
            }

            return null;
        }

        private static PlacedComponent FindPlacedComponentByDefinitionId(BoardState board, string definitionId)
        {
            foreach (var component in board.Components)
            {
                if (component.ComponentDefinitionId == definitionId)
                {
                    return component;
                }
            }

            return null;
        }

        private static GridPosition FindSafePlacement(BoardState board, GridPosition sourcePos, GridPosition groundPos)
        {
            // Prefer placing resistor to the left of source/ground column to avoid
            // Manhattan auto-path self-crossing with source-to-ground net.
            int safeX = Math.Max(0, sourcePos.X - 1);
            GridPosition seed = new GridPosition(safeX, (sourcePos.Y + groundPos.Y) / 2);
            if (!board.IsPositionOccupied(seed))
            {
                return seed;
            }

            for (int radius = 1; radius <= 8; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        GridPosition candidate = new GridPosition(seed.X + dx, seed.Y + dy);
                        if (!board.IsPositionOccupied(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return new GridPosition(seed.X + 1, seed.Y + 1);
        }

        private static int FindPinIndex(PlacedComponent component, params string[] names)
        {
            for (int i = 0; i < component.Pins.Count; i++)
            {
                string pinName = component.Pins[i].PinName ?? string.Empty;
                foreach (var name in names)
                {
                    if (pinName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int FindPinByExtremeWorldY(PlacedComponent component, bool pickMax)
        {
            int selected = 0;
            int selectedY = component.GetPinWorldPosition(0).Y;

            for (int i = 1; i < component.Pins.Count; i++)
            {
                int y = component.GetPinWorldPosition(i).Y;
                if ((pickMax && y > selectedY) || (!pickMax && y < selectedY))
                {
                    selected = i;
                    selectedY = y;
                }
            }

            return selected;
        }

        private static bool ValidateRotateByCommandPath(BoardState board, CommandHistory history, out string info)
        {
            info = "ok";

            PlacedComponent candidate = null;
            foreach (var component in board.Components)
            {
                if (!component.IsFixed)
                {
                    candidate = component;
                    break;
                }
            }

            if (candidate == null)
            {
                info = "No non-fixed component found for rotation.";
                return false;
            }

            int expectedRotation = (candidate.Rotation + RotationConstants.Quarter) % RotationConstants.Full;
            GridPosition position = candidate.Position;
            string definitionId = candidate.ComponentDefinitionId;
            float? customValue = candidate.CustomValue;

            history.ExecuteCommand(new RemoveComponentCommand(board, candidate.InstanceId));
            history.ExecuteCommand(new PlaceComponentCommand(
                board,
                definitionId,
                position,
                expectedRotation,
                ClonePinsWithoutNet(candidate.Pins),
                customValue));

            PlacedComponent rotated = board.GetComponentAt(position);
            if (rotated == null)
            {
                info = "Rotated component not found at original position.";
                return false;
            }

            bool ok = rotated.ComponentDefinitionId == definitionId && rotated.Rotation == expectedRotation;
            if (!ok)
            {
                info = $"Rotation mismatch: expected={expectedRotation}, actual={rotated.Rotation}.";
                return false;
            }

            return true;
        }

        private static bool ValidateFixedComponentMoveByCommandPath(BoardState board, CommandHistory history, out string info)
        {
            info = "ok";

            PlacedComponent fixedComponent = null;
            foreach (var component in board.Components)
            {
                if (component.IsFixed)
                {
                    fixedComponent = component;
                    break;
                }
            }

            if (fixedComponent == null)
            {
                info = "No fixed component found to validate move.";
                return false;
            }

            GridPosition origin = fixedComponent.Position;
            GridPosition target = FindNearbyFreeCell(board, origin);
            if (target == origin)
            {
                info = "No nearby free cell for fixed component move.";
                return false;
            }

            history.ExecuteCommand(new RemoveComponentCommand(board, fixedComponent.InstanceId, allowFixed: true));
            history.ExecuteCommand(new PlaceComponentCommand(
                board,
                fixedComponent.ComponentDefinitionId,
                target,
                fixedComponent.Rotation,
                ClonePinsWithoutNet(fixedComponent.Pins),
                fixedComponent.CustomValue,
                fixedComponent.IsFixed));

            PlacedComponent moved = board.GetComponentAt(target);
            if (moved == null)
            {
                info = "Fixed component did not appear at target position.";
                return false;
            }

            bool ok = moved.ComponentDefinitionId == fixedComponent.ComponentDefinitionId && moved.IsFixed;
            if (!ok)
            {
                info = $"Fixed move mismatch: def={moved.ComponentDefinitionId}, isFixed={moved.IsFixed}.";
                return false;
            }

            return true;
        }

        private static bool ValidateTraceJointByCommandPath(BoardState board, CommandHistory history, StageDefinition stage, out string info)
        {
            info = "ok";

            if (board.Traces.Count == 0)
            {
                info = "No trace exists for joint target.";
                return false;
            }

            ComponentDefinition resistorDef = FindAllowedDefinition(stage.AllowedComponents, ComponentKind.Resistor);
            if (resistorDef == null)
            {
                info = "No allowed resistor definition for joint source.";
                return false;
            }

            GridPosition placement = FindFirstFreeCell(board);
            List<PinInstance> pins = PinInstanceFactory.CreatePinInstances(resistorDef);
            if (pins.Count == 0)
            {
                info = "Failed to create pins for joint-source resistor.";
                return false;
            }

            PlacedComponent source = board.PlaceComponent(resistorDef.Id, placement, RotationConstants.None, pins);
            if (source == null)
            {
                info = "Failed to place joint-source resistor.";
                return false;
            }

            TraceSegment targetTrace = board.Traces[0];
            GridPosition targetPoint = SelectTraceInteriorOrEndpoint(targetTrace);
            GridPosition startPoint = source.GetPinWorldPosition(0);
            int traceBefore = board.Traces.Count;

            var command = new RouteTraceToNetPointCommand(
                board,
                new PinReference(source.InstanceId, 0, startPoint),
                targetTrace.NetId,
                targetPoint,
                WirePathCalculator.BuildManhattanSegments(startPoint, targetPoint));
            history.ExecuteCommand(command);

            if (source.Pins.Count == 0 || !source.Pins[0].ConnectedNetId.HasValue)
            {
                info = "Joint route did not connect source pin to any net.";
                return false;
            }

            int connectedNetId = source.Pins[0].ConnectedNetId.Value;
            bool netExists = board.GetNet(connectedNetId) != null;
            bool tracesExpanded = board.Traces.Count > traceBefore;
            if (!netExists || !tracesExpanded)
            {
                info = $"Joint validation failed: netExists={netExists}, tracesExpanded={tracesExpanded}.";
                return false;
            }

            return true;
        }

        private static GridPosition SelectTraceInteriorOrEndpoint(TraceSegment trace)
        {
            if (trace.Start.X == trace.End.X)
            {
                int midY = (trace.Start.Y + trace.End.Y) / 2;
                return new GridPosition(trace.Start.X, midY);
            }

            if (trace.Start.Y == trace.End.Y)
            {
                int midX = (trace.Start.X + trace.End.X) / 2;
                return new GridPosition(midX, trace.Start.Y);
            }

            return trace.Start;
        }

        private static GridPosition FindNearbyFreeCell(BoardState board, GridPosition origin)
        {
            GridPosition[] candidates =
            {
                new(origin.X + 1, origin.Y),
                new(origin.X - 1, origin.Y),
                new(origin.X, origin.Y + 1),
                new(origin.X, origin.Y - 1),
                new(origin.X + 1, origin.Y + 1),
                new(origin.X - 1, origin.Y - 1),
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!board.IsPositionOccupied(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return origin;
        }

        private static List<PinInstance> ClonePinsWithoutNet(IReadOnlyList<PinInstance> pins)
        {
            var clonedPins = new List<PinInstance>(pins?.Count ?? 0);
            if (pins == null)
            {
                return clonedPins;
            }

            for (int i = 0; i < pins.Count; i++)
            {
                PinInstance source = pins[i];
                if (source == null)
                {
                    continue;
                }

                clonedPins.Add(new PinInstance(source.PinIndex, source.PinName, source.LocalPosition));
            }

            return clonedPins;
        }
    }
}
