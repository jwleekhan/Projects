using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class PuzzleZoom : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private float zoomStep = 0.1f;
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 2f;

        [Header("StepView")]
        [SerializeField] private RectTransform stepViewPrime;
        [SerializeField] private float thresholdWorldY;

        private float currentZoom = 1f;

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null)
                return;

            bool isCtrlPressed =
                Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed;

            scrollRect.enabled = !isCtrlPressed;

            if (!isCtrlPressed)
                return;

            // Ctrl + 0 -> Reset Zoom
            if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                ResetZoom();
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;

            // Ctrl + 휠 -> Zoom
            if (scroll != 0f)
            {
                float direction = Mathf.Sign(scroll);
                Zoom(direction * zoomStep);
            }
        }

        private void LateUpdate()
        {
            ClampStepView();
        }

        public void Zoom(float delta)
        {
            currentZoom += delta;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            content.localScale = Vector3.one * currentZoom;
        }

        public void ResetZoom()
        {
            currentZoom = 1f;
            content.localScale = Vector3.one;
            scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
        }

        private void ClampStepView()
        {
            // 위치 초기화
            stepViewPrime.anchoredPosition = Vector2.zero;
            float worldY = stepViewPrime.position.y;
            if (worldY >= thresholdWorldY) return;

            // 임계값보다 낮으면 보정
            float delta = (thresholdWorldY - worldY) / stepViewPrime.lossyScale.y;
            stepViewPrime.anchoredPosition = new Vector2(0f, delta);
        }

        public void OnClickZoomIn()
        {
            Zoom(zoomStep);
        }

        public void OnClickZoomOut()
        {
            Zoom(-zoomStep);
        }
    }
}
