using System;
using System.Collections;
using UnityEngine;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Core;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Bridges StageManager (CircuitCraft.Managers assembly) with CameraController (Assembly-CSharp).
    /// Subscribes to OnStageLoaded and frames the camera on the active board area.
    /// </summary>
    public class StageCameraFramer : MonoBehaviour
    {
        [Tooltip("Stage manager that raises stage load events.")]
        [SerializeField] private StageManager _stageManager;

        [Tooltip("Camera controller to frame the board area.")]
        [SerializeField] private CameraController _cameraController;

        [Tooltip("Grid settings used to convert stage area to world-space framing.")]
        [SerializeField] private GridSettings _gridSettings;

        [Tooltip("Optional GameManager used to frame actual placed content after stage setup.")]
        [SerializeField] private GameManager _gameManager;

        private Coroutine _frameRoutine;

        private void OnEnable()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded += FrameCamera;
        }

        private void OnDisable()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= FrameCamera;

            if (_frameRoutine != null)
            {
                StopCoroutine(_frameRoutine);
                _frameRoutine = null;
            }
        }

        private void FrameCamera()
        {
            if (_frameRoutine != null)
            {
                StopCoroutine(_frameRoutine);
            }

            _frameRoutine = StartCoroutine(FrameCameraDeferred());
        }

        private IEnumerator FrameCameraDeferred()
        {
            if (_cameraController == null || _gridSettings == null || _stageManager.CurrentStage == null)
                yield break;

            // Wait one frame so fixed placements are already spawned before we compute framing bounds.
            yield return null;

            if (_gameManager == null)
            {
                _gameManager = FindObjectOfType<GameManager>();
            }

            if (_gameManager != null && _gameManager.BoardState != null)
            {
                BoardState boardState = _gameManager.BoardState;
                if (boardState.Components.Count > 0 || boardState.Traces.Count > 0)
                {
                    BoardBounds bounds = boardState.ComputeContentBounds();
                    float cellSize = _gridSettings.CellSize;
                    Vector3 origin = _gridSettings.GridOrigin;
                    Vector3 worldMin = origin + new Vector3(bounds.MinX * cellSize, 0f, bounds.MinY * cellSize);
                    Vector3 worldMax = origin + new Vector3(bounds.MaxX * cellSize, 0f, bounds.MaxY * cellSize);
                    _cameraController.FrameBounds(worldMin, worldMax, padding: 2.5f);
                    _frameRoutine = null;
                    yield break;
                }
            }

            int side = (int)Math.Ceiling(Math.Sqrt(_stageManager.CurrentStage.TargetArea));
            _cameraController.FrameSuggestedArea(side, side, _gridSettings.CellSize, _gridSettings.GridOrigin);
            _frameRoutine = null;
        }
    }
}
