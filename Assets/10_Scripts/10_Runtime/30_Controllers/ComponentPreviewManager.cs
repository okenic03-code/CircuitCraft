using CircuitCraft.Components;
using CircuitCraft.Data;
using CircuitCraft.Utils;
using UnityEngine;

namespace CircuitCraft.Controllers
{
    /// <summary>
    /// Manages the visual preview instance during component placement.
    /// Handles creation, positioning, rotation, and destruction of the preview GameObject.
    /// </summary>
    public class ComponentPreviewManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Prefab to instantiate when placing components.")]
        private GameObject _componentViewPrefab;

        private GameObject _previewInstance;
        private ComponentView _cachedPreviewView;
        private SpriteRenderer _cachedPreviewSprite;

        /// <summary>Whether a preview instance currently exists.</summary>
        public bool HasPreview => _previewInstance != null;

        /// <summary>
        /// Creates the placement preview for the given component definition.
        /// </summary>
        /// <param name="definition">Component definition used to initialize the preview.</param>
        public void CreatePreview(ComponentDefinition definition)
        {
            if (_componentViewPrefab == null || definition == null)
                return;

            _previewInstance = Instantiate(_componentViewPrefab, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));

            _cachedPreviewView = _previewInstance.GetComponent<ComponentView>();
            if (_cachedPreviewView != null)
            {
                _cachedPreviewView.Initialize(definition);
            }

            _cachedPreviewSprite = _previewInstance.GetComponent<SpriteRenderer>();
            if (_cachedPreviewSprite != null)
            {
                Color c = _cachedPreviewSprite.color;
                c.a = 0.5f;
                _cachedPreviewSprite.color = c;
            }
        }

        /// <summary>
        /// Destroys the current placement preview instance and clears cached references.
        /// </summary>
        public void DestroyPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;

                _cachedPreviewView = null;
                _cachedPreviewSprite = null;
            }
        }

        /// <summary>
        /// Moves the preview to a grid position and updates validity visuals.
        /// </summary>
        /// <param name="gridPos">Target grid position for the preview.</param>
        /// <param name="isValidPlacement">Whether the current placement is valid.</param>
        /// <param name="cellSize">Grid cell size in world units.</param>
        /// <param name="gridOrigin">Grid origin in world space.</param>
        public void UpdatePosition(Vector2Int gridPos, bool isValidPlacement, float cellSize, Vector3 gridOrigin)
        {
            if (_previewInstance == null)
                return;

            Vector3 worldPos = GridUtility.GridToWorldPosition(gridPos, cellSize, gridOrigin);
            _previewInstance.transform.position = worldPos;

            if (_cachedPreviewView != null)
                _cachedPreviewView.SetHovered(!isValidPlacement);
        }

        /// <summary>
        /// Applies Y-axis rotation to the active preview instance.
        /// </summary>
        /// <param name="rotation">Rotation in degrees.</param>
        public void ApplyRotation(int rotation)
        {
            if (_previewInstance != null)
                _previewInstance.transform.rotation = Quaternion.Euler(90f, rotation, 0f);
        }

        private void OnDestroy()
        {
            DestroyPreview();
        }
    }
}
