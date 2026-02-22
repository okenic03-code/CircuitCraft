using System;
using UnityEngine;
using CircuitCraft.Data;
using CircuitCraft.Managers;

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

        private void OnEnable()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded += FrameCamera;
        }

        private void OnDisable()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded -= FrameCamera;
        }

        private void FrameCamera()
        {
            if (_cameraController == null || _gridSettings == null || _stageManager.CurrentStage == null)
                return;

            int side = (int)Math.Ceiling(Math.Sqrt(_stageManager.CurrentStage.TargetArea));
            _cameraController.FrameSuggestedArea(side, side, _gridSettings.CellSize, _gridSettings.GridOrigin);
        }
    }
}
