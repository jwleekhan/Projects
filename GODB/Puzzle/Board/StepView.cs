using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class StepView : MonoBehaviour
    {
        [Header("GridView")]
        [SerializeField] private GridView[] gridViews;

        [Header("UI")]
        public RectTransform rect;
        public RectTransform backImage;
        public HorizontalLayoutGroup layout;

        public int Length => gridViews.Length;
        public GridView GetGridView(int index) => index < gridViews.Length ? gridViews[index] : null;

        private void Awake()
        {
            if (gridViews == null || gridViews.Length == 0)
            {
                Debug.LogError($"{name}: gridViews is not assigned");
            }

            if (rect == null) rect = GetComponent<RectTransform>();
            if (layout == null) layout = GetComponent<HorizontalLayoutGroup>();

            if (rect == null || layout == null)
            {
                Debug.LogError($"{name}: rect or layout is not assigned");
            }
        }

        public void Build(int width, int height)
        {
            for (int i = 0; i < gridViews.Length; i++)
            {
                gridViews[i].Build(width, height);
            }
        }

        public void Bind(Sprite[][] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"{name}.BuildAndBind: sprites is empty");
                return;
            }
            if (sprites.Length != gridViews.Length)
            {
                Debug.LogWarning($"{name}.BuildAndBind: sprites.Length != gridViews.Length");
                return;
            }

            for (int i = 0; i < gridViews.Length; i++)
            {
                gridViews[i].Bind(sprites[i]);
            }
        }

        public void BuildAndBind(int width, int height, Sprite[][] sprites)
        {
            Build(width, height);
            Bind(sprites);
        }
    }
}

