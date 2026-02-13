using System;
using System.Threading;
using CircuitCraft.Core;
using CircuitCraft.Simulation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CircuitCraft.Managers
{
    /// <summary>
    /// Main game controller for CircuitCraft gameplay.
    /// Manages BoardState lifecycle and delegates simulation execution to SimulationManager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Board Configuration")]
        [SerializeField] private int _boardWidth = 20;
        [SerializeField] private int _boardHeight = 20;

        [Header("Dependencies")]
        [SerializeField] private BoardState _boardState;
        [SerializeField] private SimulationManager _simulationManager;

        private void Awake() => Init();

        private void Init()
        {
            InitializeServiceRegistry();
            ValidateSimulationManager();
            InitializeBoardState();
        }

        private void InitializeServiceRegistry()
        {
            ServiceRegistry.Register(this);
        }

        private void ValidateSimulationManager()
        {
            if (_simulationManager == null)
            {
                Debug.LogError("GameManager: SimulationManager reference is missing. Assign via Inspector.");
            }
        }

        private void InitializeBoardState()
        {
            if (_boardState == null)
            {
                _boardState = new BoardState(_boardWidth, _boardHeight);
                Debug.Log($"GameManager: BoardState initialized ({_boardWidth}x{_boardHeight})");
            }
        }

        /// <summary>
        /// Gets the current BoardState instance.
        /// </summary>
        public BoardState BoardState => _boardState;

        /// <summary>
        /// Resets the board to a new empty state with the specified dimensions.
        /// Used by StageManager when loading a new stage.
        /// </summary>
        /// <param name="width">Board width in grid cells.</param>
        /// <param name="height">Board height in grid cells.</param>
        public void ResetBoard(int width, int height)
        {
            _boardWidth = width;
            _boardHeight = height;
            _boardState = new BoardState(width, height);
            Debug.Log($"GameManager: Board reset ({width}x{height})");
        }

        /// <summary>
        /// Whether a simulation is currently in progress.
        /// </summary>
        public bool IsSimulating => _simulationManager != null && _simulationManager.IsSimulating;

        /// <summary>
        /// The result of the most recent simulation, or null if no simulation has been run.
        /// </summary>
        public SimulationResult LastSimulationResult =>
            _simulationManager != null ? _simulationManager.LastSimulationResult : null;

        /// <summary>
        /// Event raised when a simulation completes (success or failure).
        /// </summary>
        public event Action<SimulationResult> OnSimulationCompleted
        {
            add
            {
                if (_simulationManager != null)
                {
                    _simulationManager.OnSimulationCompleted += value;
                }
            }
            remove
            {
                if (_simulationManager != null)
                {
                    _simulationManager.OnSimulationCompleted -= value;
                }
            }
        }

        /// <summary>
        /// Runs a DC operating point simulation on the current BoardState.
        /// Called by UI "Simulate" button.
        /// </summary>
        public void RunSimulation()
        {
            if (_simulationManager == null)
            {
                Debug.LogWarning("GameManager: Cannot run simulation - SimulationManager reference is missing.");
                return;
            }

            _simulationManager.RunSimulation(_boardState);
        }

        /// <summary>
        /// Runs a DC operating point simulation on the current BoardState asynchronously.
        /// </summary>
        public async UniTask RunSimulationAsync(CancellationToken cancellationToken = default)
        {
            if (_simulationManager == null)
            {
                Debug.LogWarning("GameManager: Cannot run simulation - SimulationManager reference is missing.");
                return;
            }

            await _simulationManager.RunSimulationAsync(_boardState, cancellationToken);
        }
    }
}
