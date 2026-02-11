using System.Collections.Generic;
using UnityEngine;
using CircuitCraft.Core;
using CircuitCraft.Components;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Utils;

namespace CircuitCraft.Views
{
    /// <summary>
    /// Visualizes BoardState in Unity scene by listening to BoardState events.
    /// Automatically instantiates/destroys <see cref="ComponentView"/> instances
    /// when components are placed or removed via <see cref="BoardState"/>.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;

        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("Prefab with ComponentView component to instantiate for placed components.")]
        private GameObject _componentViewPrefab;

        [Header("Grid Configuration")]
        [SerializeField] private GridSettings _gridSettings;

        private BoardState _boardState;

        /// <summary>
        /// Maps component InstanceId to its visual <see cref="ComponentView"/> representation.
        /// </summary>
        private readonly Dictionary<int, ComponentView> _componentViews = new Dictionary<int, ComponentView>();

        /// <summary>
        /// Gets the read-only mapping of component instance IDs to their views.
        /// Useful for external systems that need to query visual state.
        /// </summary>
        public IReadOnlyDictionary<int, ComponentView> ComponentViews => _componentViews;

        /// <summary>
        /// Uses Start() instead of Awake() to ensure GameManager.Awake() has already
        /// initialized BoardState before we attempt to subscribe to its events.
        /// </summary>
        private void Start()
        {
            if (_gameManager != null)
            {
                _boardState = _gameManager.BoardState;

                if (_boardState != null)
                {
                    SubscribeToBoardEvents();
                    Debug.Log("BoardView: Subscribed to BoardState events");
                }
                else
                {
                    Debug.LogWarning("BoardView: GameManager has no BoardState instance");
                }
            }
            else
            {
                Debug.LogError("BoardView: No GameManager assigned!");
            }
            
            if (_gridSettings == null)
            {
                Debug.LogError("BoardView: GridSettings reference is missing!");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromBoardEvents();
        }

        /// <summary>
        /// Subscribes to <see cref="BoardState"/> events for automatic visual synchronization.
        /// Called in <see cref="Awake"/> after obtaining BoardState reference.
        /// </summary>
        private void SubscribeToBoardEvents()
        {
            if (_boardState == null) return;

            _boardState.OnComponentPlaced += HandleComponentPlaced;
            _boardState.OnComponentRemoved += HandleComponentRemoved;
        }

        /// <summary>
        /// Unsubscribes from <see cref="BoardState"/> events to prevent memory leaks.
        /// Called in <see cref="OnDestroy"/> during MonoBehaviour teardown.
        /// </summary>
        private void UnsubscribeFromBoardEvents()
        {
            if (_boardState == null) return;

            _boardState.OnComponentPlaced -= HandleComponentPlaced;
            _boardState.OnComponentRemoved -= HandleComponentRemoved;
        }

        /// <summary>
        /// Handles a component being placed on the board.
        /// Instantiates a <see cref="ComponentView"/> prefab at the correct grid position.
        /// </summary>
        /// <param name="component">The placed component data from BoardState.</param>
        private void HandleComponentPlaced(PlacedComponent component)
        {
            if (component == null)
            {
                Debug.LogWarning("BoardView: Received null PlacedComponent in HandleComponentPlaced");
                return;
            }

            if (_componentViews.ContainsKey(component.InstanceId))
            {
                Debug.LogWarning($"BoardView: ComponentView already exists for InstanceId {component.InstanceId}");
                return;
            }

            SpawnComponentView(component);
        }

        /// <summary>
        /// Handles a component being removed from the board.
        /// Finds and destroys the corresponding <see cref="ComponentView"/>.
        /// </summary>
        /// <param name="instanceId">The instance ID of the removed component.</param>
        private void HandleComponentRemoved(int instanceId)
        {
            DestroyComponentView(instanceId);
        }

        /// <summary>
        /// Instantiates a <see cref="ComponentView"/> prefab at the grid position of the placed component.
        /// The view is parented to this BoardView's transform for clean hierarchy management.
        /// </summary>
        /// <param name="component">The placed component to visualize.</param>
        private void SpawnComponentView(PlacedComponent component)
        {
            if (_componentViewPrefab == null)
            {
                Debug.LogWarning("BoardView: No ComponentView prefab assigned. Cannot spawn visual.");
                return;
            }
            
            if (_gridSettings == null)
            {
                Debug.LogWarning("BoardView: GridSettings is missing. Cannot spawn visual.");
                return;
            }

            // Convert grid position to world position using GridUtility
            Vector2Int gridPos = new Vector2Int(component.Position.X, component.Position.Y);
            Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, _gridSettings.CellSize, _gridSettings.GridOrigin);

            // Instantiate prefab at world position with rotation, parented to BoardView
            Quaternion rotation = Quaternion.Euler(0f, component.Rotation, 0f);
            GameObject instance = Instantiate(_componentViewPrefab, worldPos, rotation, transform);
            instance.name = $"Component_{component.InstanceId}_{component.ComponentDefinitionId}";

            // Configure ComponentView
            ComponentView view = instance.GetComponent<ComponentView>();
            if (view != null)
            {
                view.GridPosition = gridPos;
                // Note: Full initialization with ComponentDefinition requires a definition
                // lookup service (IComponentDefinitionProvider). PlacementController handles
                // Initialize() when placing via UI interaction. BoardView provides automatic
                // sync from BoardState events for programmatic placement.
                Debug.Log($"BoardView: Spawned ComponentView for {component.ComponentDefinitionId} " +
                          $"(InstanceId: {component.InstanceId}) at grid ({gridPos.x}, {gridPos.y})");
            }
            else
            {
                Debug.LogError("BoardView: ComponentView prefab is missing ComponentView component!");
            }

            _componentViews[component.InstanceId] = view;
        }

        /// <summary>
        /// Destroys the <see cref="ComponentView"/> associated with the given instance ID
        /// and removes it from the tracking dictionary.
        /// </summary>
        /// <param name="instanceId">The instance ID of the component to remove visually.</param>
        private void DestroyComponentView(int instanceId)
        {
            if (_componentViews.TryGetValue(instanceId, out ComponentView view))
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                    Debug.Log($"BoardView: Destroyed ComponentView for InstanceId {instanceId}");
                }

                _componentViews.Remove(instanceId);
            }
            else
            {
                Debug.LogWarning($"BoardView: No ComponentView found for InstanceId {instanceId}");
            }
        }

        /// <summary>
        /// Converts grid coordinates to a world position using the configured grid settings.
        /// Delegates to <see cref="GridUtility.GridToWorldPosition"/>.
        /// </summary>
        /// <param name="x">Grid X coordinate.</param>
        /// <param name="y">Grid Y coordinate.</param>
        /// <returns>World position at the center of the grid cell.</returns>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            if (_gridSettings == null)
                return Vector3.zero;
            
            return GridUtility.GridToWorldPosition(new Vector2Int(x, y), _gridSettings.CellSize, _gridSettings.GridOrigin);
        }
    }
}
