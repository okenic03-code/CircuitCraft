using UnityEngine;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    public class PaletteResizer
    {
        private const float PaletteMinWidth = 280f;
        private const float PaletteMaxWidth = 420f;

        private readonly VisualElement _paletteElement;
        private readonly VisualElement _resizeHandle;
        private readonly VisualElement _root;

        private bool _isPaletteResizing;
        private int _activeResizePointerId = -1;
        private float _resizeStartX;
        private float _resizeStartWidth;

        public PaletteResizer(VisualElement paletteElement, VisualElement resizeHandle, VisualElement root)
        {
            _paletteElement = paletteElement;
            _resizeHandle = resizeHandle;
            _root = root;
        }

        public void RegisterCallbacks()
        {
            if (_resizeHandle == null || _paletteElement == null || _root == null)
                return;

            _resizeHandle.RegisterCallback<PointerDownEvent>(OnPaletteResizePointerDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPaletteResizePointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPaletteResizePointerUp);
            _resizeHandle.RegisterCallback<PointerCaptureOutEvent>(OnPaletteResizePointerCaptureOut);
        }

        public void UnregisterCallbacks()
        {
            if (_resizeHandle == null || _root == null)
            {
                EndPaletteResize();
                return;
            }

            _resizeHandle.UnregisterCallback<PointerDownEvent>(OnPaletteResizePointerDown);
            _root.UnregisterCallback<PointerMoveEvent>(OnPaletteResizePointerMove);
            _root.UnregisterCallback<PointerUpEvent>(OnPaletteResizePointerUp);
            _resizeHandle.UnregisterCallback<PointerCaptureOutEvent>(OnPaletteResizePointerCaptureOut);
            EndPaletteResize();
        }

        private void OnPaletteResizePointerDown(PointerDownEvent evt)
        {
            if (_paletteElement == null || _resizeHandle == null || evt.button != 0)
                return;

            float currentWidth = _paletteElement.resolvedStyle.width;
            if (currentWidth <= 0f)
                currentWidth = PaletteMinWidth;

            _isPaletteResizing = true;
            _activeResizePointerId = evt.pointerId;
            _resizeStartX = evt.position.x;
            _resizeStartWidth = currentWidth;

            _resizeHandle.CapturePointer(_activeResizePointerId);
            evt.StopPropagation();
        }

        private void OnPaletteResizePointerMove(PointerMoveEvent evt)
        {
            if (!_isPaletteResizing || evt.pointerId != _activeResizePointerId || _paletteElement == null)
                return;

            float delta = evt.position.x - _resizeStartX;
            float nextWidth = Mathf.Clamp(_resizeStartWidth + delta, PaletteMinWidth, PaletteMaxWidth);
            _paletteElement.style.width = nextWidth;
            _paletteElement.style.minWidth = nextWidth;
            evt.StopPropagation();
        }

        private void OnPaletteResizePointerUp(PointerUpEvent evt)
        {
            if (!_isPaletteResizing || evt.pointerId != _activeResizePointerId)
                return;

            EndPaletteResize();
            evt.StopPropagation();
        }

        private void OnPaletteResizePointerCaptureOut(PointerCaptureOutEvent _)
        {
            if (_isPaletteResizing)
                EndPaletteResize();
        }

        private void EndPaletteResize()
        {
            if (_resizeHandle != null && _activeResizePointerId >= 0 && _resizeHandle.HasPointerCapture(_activeResizePointerId))
                _resizeHandle.ReleasePointer(_activeResizePointerId);

            _isPaletteResizing = false;
            _activeResizePointerId = -1;
        }
    }
}
