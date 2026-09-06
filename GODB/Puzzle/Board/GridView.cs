using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class GridView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GridLayoutGroup layout;
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Sprite[] _cellBackgrounds; // 0: default, 1: highlighted(target), 2: highlighted(extra)
        [SerializeField] private float[] _bgScales;

        public Vector2 cellSize => layout.cellSize;
        public Vector2 spacing => layout.spacing;
        public RectOffset padding => layout.padding;

        private Cell[] _cells;

        private int _w, _h;

        private void Awake()
        {
            if (layout == null) layout = GetComponent<GridLayoutGroup>();

            if (layout == null || cellPrefab == null)
            {
                Debug.LogError($"{name}: layout or cellPrefab is not assigned");
            }
        }

        public void Build(int width, int height)
        {
            _w = width;
            _h = height;

            // GridLayoutGroup 설정: 열 수 고정
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = _w;

            // 기존 삭제
            foreach (Transform child in layout.transform)
                Destroy(child.gameObject);

            // cell 생성
            int n = _w * _h;

            _cells = new Cell[n];
            for (int i = 0; i < n; i++)
            {
                Cell cell = Instantiate(cellPrefab, layout.transform);
                _cells[i] = cell;
            }
        }

        public void Bind(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"{name}.Bind: sprites is empty");
                return;
            }
            if (_cells == null || _cells.Length == 0)
            {
                Debug.LogWarning($"{name}.Bind: cells is empty");
                return;
            }
            if (sprites.Length != _cells.Length)
            {
                Debug.LogWarning($"{name}.Bind: sprites.Length != cells.Length");
                return;
            }

            ApplySprites(sprites);
        }

        public void BuildAndBind(int width, int height, Sprite[] sprites)
        {
            Build(width, height);
            Bind(sprites);
        }

        private void ApplySprites(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != _cells.Length) return;

            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                    _cells[i].SetItem(sprites[i]);
            }
        }

        public void HighlightCells(SelectionType[] selectionTypes)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                int type = 0;
                if (selectionTypes != null)
                {
                    if (selectionTypes[i] == SelectionType.Target) type = 1;
                    if (selectionTypes[i] == SelectionType.Extra) type = 2;
                }
                _cells[i].SetBackground(_cellBackgrounds[type], _bgScales[type]);
            }
        }

        public void SetCellListeners(Action<int> onClick, Action<int, Vector2> onDrag)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                int cellIdx = i;
                if (_cells[cellIdx] != null)
                {
                    _cells[cellIdx].SetButtonListener(() => onClick(cellIdx),
                                                        (vec) => onDrag(cellIdx, vec));
                }
            }
        }

        public void SetCellInteractables(bool interactable)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].SetButtonInteractable(interactable);
                }
            }
        }
    }
}
