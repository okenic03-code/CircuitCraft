using System;
using System.IO;
using System.Threading;
using CircuitCraft.Commands;
using CircuitCraft.Core;
using CircuitCraft.Simulation;
using CircuitCraft.Systems;
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
        [Header("Suggested Area Configuration")]
        [SerializeField, Tooltip("Suggested width (not a hard limit)")] private int _suggestedWidth = 20;
        [SerializeField, Tooltip("Suggested height (not a hard limit)")] private int _suggestedHeight = 20;

        [Header("Dependencies")]
        [SerializeField, Tooltip("Runtime board state container for placed components, nets, and traces.")]
        private BoardState _boardState;

        [SerializeField, Tooltip("Simulation manager used to run circuit analysis requests.")]
        private SimulationManager _simulationManager;

        private SaveLoadService _saveLoadService;
        private CommandHistory _commandHistory = new();

        private void Awake() => Init();

        private void Init()
        {
            ValidateSimulationManager();
            InitializeBoardState();
            InitializeSaveLoadService();
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
            if (_boardState is null)
            {
                _boardState = new BoardState(_suggestedWidth, _suggestedHeight);
                Debug.Log($"GameManager: BoardState initialized with suggested area ({_suggestedWidth}x{_suggestedHeight})");
            }
        }

        private void InitializeSaveLoadService()
        {
            _saveLoadService = new();
            Debug.Log("GameManager: SaveLoadService initialized.");
        }

        /// <summary>
        /// Gets the current BoardState instance.
        /// </summary>
        public BoardState BoardState => _boardState;

        /// <summary>
        /// Gets the command history used for undo/redo board mutations.
        /// </summary>
        public CommandHistory CommandHistory => _commandHistory;

        /// <summary>
        /// Event raised when the current board has been saved successfully.
        /// </summary>
        public event Action<string> OnBoardSaved;

        /// <summary>
        /// Event raised when a board has been loaded successfully.
        /// </summary>
        public event Action<string> OnBoardLoaded;

        /// <summary>
        /// Resets the board to a new empty state with the specified suggested area.
        /// Used by StageManager when loading a new stage.
        /// </summary>
        /// <param name="width">Suggested width in grid cells (not a hard limit).</param>
        /// <param name="height">Suggested height in grid cells (not a hard limit).</param>
        public void ResetBoard(int width, int height)
        {
            _suggestedWidth = width;
            _suggestedHeight = height;
            _boardState = new BoardState(width, height);
            _commandHistory.Clear();
            OnBoardLoaded?.Invoke(string.Empty);
            Debug.Log($"GameManager: Board reset with suggested area ({width}x{height})");
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
        /// <param name="cancellationToken">Cancellation token controlling the simulation request lifetime.</param>
        /// <returns>A task that completes when simulation execution finishes.</returns>
        public async UniTask RunSimulationAsync(CancellationToken cancellationToken = default)
        {
            if (_simulationManager == null)
            {
                Debug.LogWarning("GameManager: Cannot run simulation - SimulationManager reference is missing.");
                return;
            }

            await _simulationManager.RunSimulationAsync(_boardState, cancellationToken);
        }

        /// <summary>
        /// Saves the current board state to a JSON file for the given stage.
        /// File is written to <c>Application.persistentDataPath/saves/{stageId}.json</c>.
        /// </summary>
        /// <param name="stageId">Stage identifier used as the file name.</param>
        public void SaveCurrentBoard(string stageId)
        {
            try
            {
                var filePath = GetSaveFilePath(stageId);
                _saveLoadService.SaveToFile(filePath, _boardState, stageId);
                Debug.Log($"GameManager: Board saved for stage '{stageId}' at {filePath}");
                OnBoardSaved?.Invoke(stageId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager: Failed to save board for stage '{stageId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a previously saved board state from disk and replaces the current board.
        /// File is read from <c>Application.persistentDataPath/saves/{stageId}.json</c>.
        /// </summary>
        /// <param name="stageId">Stage identifier used as the file name.</param>
        public void LoadBoard(string stageId)
        {
            try
            {
                var filePath = GetSaveFilePath(stageId);
                var data = _saveLoadService.LoadFromFile(filePath);
                var newBoardState = new BoardState(data.boardWidth, data.boardHeight);
                _saveLoadService.RestoreToBoard(newBoardState, data);
                _boardState = newBoardState;
                _commandHistory.Clear();
                _suggestedWidth = data.boardWidth;
                _suggestedHeight = data.boardHeight;
                Debug.Log($"GameManager: Board loaded for stage '{stageId}' with suggested area ({data.boardWidth}x{data.boardHeight})");
                OnBoardLoaded?.Invoke(stageId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager: Failed to load board for stage '{stageId}': {ex.Message}");
            }
        }

        private static string GetSaveFilePath(string stageId) =>
            Path.Combine(Application.persistentDataPath, "saves", $"{stageId}.json");
    }
}
