using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class Cell : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image item;
        [SerializeField] private CellButton cellButton;

        public void SetBackground(Sprite sprite, float scale)
        {
            background.sprite = sprite;
            background.rectTransform.localScale = new Vector2(scale, scale);
        }

        public void SetItem(Sprite sprite)
        {
            item.sprite = sprite;
            Color color = item.color;
            color.a = (sprite != null) ? 1 : 0;
            item.color = color;
        }

        public void SetButtonListener(Action onClick, Action<Vector2> onDrag)
        {
            cellButton.SetButtonListener(onClick, onDrag);
        }

        public void SetButtonInteractable(bool interactable)
        {
            cellButton.SetButtonInteractable(interactable);
        }
    }
}
