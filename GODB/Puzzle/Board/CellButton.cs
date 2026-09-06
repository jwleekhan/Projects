using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class CellButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private Action _onClick;
        private Action<Vector2> _onDrag;

        [SerializeField] private Button button;

        private Vector2 _pointerDownPos;

        public void SetButtonListener(Action onClick, Action<Vector2> onDrag)
        {
            button.onClick.RemoveAllListeners();
            _onClick = onClick;
            _onDrag = onDrag;
        }

        public void SetButtonInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable)
                return;

            _pointerDownPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 아무 일도 하지 않지만, 드래그 이벤트가 빼앗기는 것을 방지
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!button.interactable)
                return;

            // 버튼 영역 안에 마우스가 있는지 검사
            RectTransform rectTransform = transform as RectTransform;
            bool isInside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera);
            if (isInside)
            {
                // 영역 내부면 클릭 이벤트
                _onClick?.Invoke();
            }
            else
            {
                // 영역 외부면 드래그 이벤트
                Vector2 deltaPos = eventData.position - _pointerDownPos;
                _onDrag?.Invoke(deltaPos);
            }
        }
    }
}
