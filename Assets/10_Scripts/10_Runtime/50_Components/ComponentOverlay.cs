using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace CircuitCraft.Components
{
    /// <summary>
    /// Manages creation and visibility of per-component simulation text overlays.
    /// </summary>
    internal sealed class ComponentOverlay
    {
        private readonly Transform _parent;
        private readonly SpriteRenderer _parentSprite;
        private readonly Vector3 _simulationOverlayOffset;
        private readonly float _simulationOverlayScale;
        private readonly Color _simulationOverlayColor;

        private GameObject _simulationOverlayObject;

#if UNITY_TEXTMESHPRO
        private TextMeshPro _simulationOverlayText;
#else
        private TextMesh _simulationOverlayText;
#endif

        /// <summary>
        /// Creates a simulation overlay helper.
        /// </summary>
        /// <param name="parent">Transform used as overlay parent.</param>
        /// <param name="parentSprite">Parent sprite renderer for sorting context.</param>
        /// <param name="simulationOverlayOffset">Local overlay position offset.</param>
        /// <param name="simulationOverlayScale">Local overlay scale multiplier.</param>
        /// <param name="simulationOverlayColor">Text color for the overlay.</param>
        public ComponentOverlay(
            Transform parent,
            SpriteRenderer parentSprite,
            Vector3 simulationOverlayOffset,
            float simulationOverlayScale,
            Color simulationOverlayColor)
        {
            _parent = parent;
            _parentSprite = parentSprite;
            _simulationOverlayOffset = simulationOverlayOffset;
            _simulationOverlayScale = simulationOverlayScale;
            _simulationOverlayColor = simulationOverlayColor;
        }

        /// <summary>
        /// Shows simulation overlay text.
        /// </summary>
        /// <param name="text">Overlay text to display.</param>
        public void ShowSimulationOverlay(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideSimulationOverlay();
                return;
            }

            EnsureSimulationOverlayText();
            if (_simulationOverlayText == null)
            {
                return;
            }

            _simulationOverlayText.text = text;
            _simulationOverlayText.color = _simulationOverlayColor;
            _simulationOverlayObject.SetActive(true);
        }

        /// <summary>
        /// Hides the simulation overlay text object.
        /// </summary>
        public void HideSimulationOverlay()
        {
            if (_simulationOverlayObject != null)
            {
                _simulationOverlayObject.SetActive(false);
            }
        }

        /// <summary>
        /// Clears overlay resources.
        /// </summary>
        public void Cleanup()
        {
            ClearSimulationOverlay();
        }

        private void EnsureSimulationOverlayText()
        {
            if (_simulationOverlayObject != null)
            {
                return;
            }

            _simulationOverlayObject = new GameObject("SimulationOverlay");
            _simulationOverlayObject.transform.SetParent(_parent, false);
            _simulationOverlayObject.transform.localPosition = _simulationOverlayOffset;
            _simulationOverlayObject.transform.localScale = Vector3.one * _simulationOverlayScale;

#if UNITY_TEXTMESHPRO
            var overlayText = _simulationOverlayObject.AddComponent<TextMeshPro>();
            overlayText.alignment = TMPro.TextAlignmentOptions.Center;
            overlayText.fontSize = 4f;
            overlayText.sortingLayerID = _parentSprite != null ? _parentSprite.sortingLayerID : 0;
            overlayText.sortingOrder = _parentSprite != null ? _parentSprite.sortingOrder + 2 : 0;
            overlayText.autoSizeTextContainer = false;
            _simulationOverlayText = overlayText;
#else
            var overlayText = _simulationOverlayObject.AddComponent<TextMesh>();
            overlayText.alignment = TextAlignment.Center;
            overlayText.anchor = TextAnchor.MiddleCenter;
            overlayText.characterSize = 0.08f;
            overlayText.fontSize = 28;
            overlayText.GetComponent<MeshRenderer>().sortingLayerID =
                _parentSprite != null ? _parentSprite.sortingLayerID : 0;
            overlayText.GetComponent<MeshRenderer>().sortingOrder = _parentSprite != null ? _parentSprite.sortingOrder + 2 : 0;
            _simulationOverlayText = overlayText;
#endif

            _simulationOverlayObject.SetActive(false);
        }

        private void ClearSimulationOverlay()
        {
            if (_simulationOverlayObject != null)
            {
                Object.Destroy(_simulationOverlayObject);
                _simulationOverlayObject = null;
                _simulationOverlayText = null;
            }
        }
    }
}
