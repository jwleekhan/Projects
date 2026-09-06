using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class PuzzleBoardView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PuzzleZoom zoom;

        [Header("Views")]
        [SerializeField] private RectTransform panelBoard;
        [SerializeField] private RectTransform stepViewGroup;
        [SerializeField] private StepView stepView0;
        [SerializeField] private StepView stepViewPrime;
        [SerializeField] private StepView stepViewPrefab;
        private StepView[] stepViews;

        [Header("Helper")]
        [SerializeField] private RectTransform stepSeperateLineParent;
        [SerializeField] private RectTransform backImageCurStep;
        [SerializeField] private RectTransform backImageStepPrime;
        [SerializeField] private RectTransform rowIndexer;
        [SerializeField] private RectTransform colIndexer;
        [SerializeField] private RectTransform stepSeperateLinePrefab;
        [SerializeField] private TMP_Text textIndexerPrefab;
        private RectTransform[] stepSeperateLines;
        private Vector2 backImageCurStep_basePos;
        private Vector2 rowIndexer_basePos;
        private Vector2 colIndexer_basePos;
        private Vector2 deltaPos;

        [Header("Applied")]
        [SerializeField] private RectTransform panelAppliedGroup;
        [SerializeField] private GameObject panelAppliedPrefab;
        private GameObject[] panelApplieds;
        private TMP_Text[] textApplieds;

        // View Panel이 차지할 수 있는 최대 크기 (default 720x900)
        // Awake 시 씬에 설정된 PanelBoard의 크기로 재할당됨
        private float _maxPanelWidth = 720;
        private float _maxPanelHeight = 900;

        private void Awake()
        {
            _maxPanelWidth = panelBoard.rect.width;
            _maxPanelHeight = panelBoard.rect.height;
        }

        public void Init(int gridWidth, int gridHeight, int maxSteps, Sprite[][] stepSprites0, Sprite[][] stepSpritesPrime)
        {
            InitInstantiate(gridWidth, gridHeight, maxSteps);

            stepView0.BuildAndBind(gridWidth, gridHeight, stepSprites0);
            stepViewPrime.BuildAndBind(gridWidth, gridHeight, stepSpritesPrime);

            ResizeBoard(gridWidth, gridHeight, maxSteps);

            zoom.ResetZoom();
        }

        public void SetCellListeners(Action<int, int, int> onClick, Action<int, int, int, Vector2> onDrag)
        {
            for (int i = 0; i < stepViews.Length; i++)
            {
                int stepIdx = i;
                StepView stepView = stepViews[stepIdx];
                for (int j = 0; j < stepView.Length; j++)
                {
                    int gridIdx = j;
                    GridView gridView = stepView.GetGridView(gridIdx);
                    gridView.SetCellListeners((cellIdx) => onClick(stepIdx, gridIdx, cellIdx),
                                                (cellIdx, vec) => onDrag(stepIdx, gridIdx, cellIdx, vec));
                    gridView.SetCellInteractables(false);
                }
            }

            for (int gridIdx = 0; gridIdx < stepViewPrime.Length; gridIdx++)
            {
                stepViewPrime.GetGridView(gridIdx).SetCellInteractables(false);
            }
        }

        public void UpdateBoard(int prevStep, Sprite[][] newSprites, string appliedText)
        {
            if (prevStep + 1 < stepViews.Length)
                stepViews[prevStep + 1].Bind(newSprites);
            if (prevStep < textApplieds.Length)
                textApplieds[prevStep].text = appliedText;
        }

        public void UpdateStep(int curStep)
        {
            for (int i = 0; i < stepViews.Length; i++)
            {
                stepViews[i].gameObject.SetActive(i <= curStep);
                if (i < panelApplieds.Length)
                    panelApplieds[i].SetActive(i < curStep);
            }

            SetCurStepUIPos(curStep);
        }

        public void HighlightCells(int curStep, SelectionType[][] selectionTypes)
        {
            StepView curStepView = stepViews[curStep];
            for (int i = 0; i < curStepView.Length; i++)
            {
                if (selectionTypes == null)
                    curStepView.GetGridView(i).HighlightCells(null);
                else
                    curStepView.GetGridView(i).HighlightCells(selectionTypes[i]);
            }
        }

        public void SetCellInteractables(int curStep, bool interactable)
        {
            StepView curStepView = stepViews[curStep];
            for (int i = 0; i < curStepView.Length; i++)
            {
                curStepView.GetGridView(i).SetCellInteractables(interactable);
            }
        }

        private void InitInstantiate(int gridWidth, int gridHeight, int maxSteps)
        {
            // 중간 과정 StepView 파괴
            if (stepViews != null)
            {
                for (int i = 1; i < stepViews.Length; i++)
                {
                    Destroy(stepViews[i].gameObject);
                }
            }

            // stepViews 초기화
            int count = maxSteps + 1;
            stepViews = new StepView[count];
            stepViews[0] = stepView0;
            for (int i = 1; i <= maxSteps; i++)
            {
                stepViews[i] = InstantiateStepView(gridWidth, gridHeight);
            }

            // 모든 생성된 PanelApplied 파괴
            if (panelApplieds != null)
            {
                for (int i = 0; i < panelApplieds.Length; i++)
                {
                    Destroy(panelApplieds[i].gameObject);
                }
            }

            // PanelApplieds 초기화 (index 체계 다름 유의)
            panelApplieds = new GameObject[maxSteps];
            textApplieds = new TMP_Text[maxSteps];
            for (int i = 0; i < maxSteps; i++)
            {
                (panelApplieds[i], textApplieds[i]) = InstantiatePanelApplied();
            }

            // 모든 생성된 StepSeperateLine 파괴
            if (stepSeperateLines != null)
            {
                for (int i = 0; i < stepSeperateLines.Length; i++)
                {
                    Destroy(stepSeperateLines[i].gameObject);
                }
            }

            // StepSeperateLines 초기화
            stepSeperateLines = new RectTransform[count];
            for (int i = 0; i <= maxSteps; i++)
            {
                stepSeperateLines[i] = InstantiateStepSeperateLine();
            }
        }

        private StepView InstantiateStepView(int gridWidth, int gridHeight)
        {
            StepView newStepView = Instantiate(stepViewPrefab, stepViewGroup);

            newStepView.Build(gridWidth, gridHeight);
            newStepView.gameObject.SetActive(false);

            return newStepView;
        }

        private (GameObject, TMP_Text) InstantiatePanelApplied()
        {
            GameObject newPanel = Instantiate(panelAppliedPrefab, panelAppliedGroup);
            TMP_Text newText = newPanel.GetComponentInChildren<TMP_Text>();

            newPanel.SetActive(false);

            return (newPanel, newText);
        }

        private RectTransform InstantiateStepSeperateLine()
        {
            RectTransform newLine = Instantiate(stepSeperateLinePrefab, stepSeperateLineParent);

            newLine.gameObject.SetActive(false);

            return newLine;
        }

        private void ResizeBoard(int gridWidth, int gridHeight, int maxSteps)
        {
            // ----- Calculate -----
            // GridView LayoutGroup에 할당된 값들을 기반으로 계산함
            GridView grid = stepViewPrefab.GetGridView(0);
            Vector2 cellSize = grid.cellSize;
            Vector2 cellSpacing = grid.spacing;
            RectOffset cellPadding = grid.padding;

            // GridView 하나의 크기
            float gridViewWidth = (cellSize.x * gridWidth) + (cellSpacing.x * (gridWidth - 1)) + cellPadding.horizontal;
            float gridViewHeight = (cellSize.y * gridHeight) + (cellSpacing.y * (gridHeight - 1)) + cellPadding.vertical;

            // GridView 간의 간격 - 임의 지정
            Vector2Int gridSpacing = Vector2Int.RoundToInt(new Vector2(cellSize.x / 4.0f, cellSize.y));

            // StepView 하나의 크기
            float stepViewWidth = (gridViewWidth * stepViewPrefab.Length) + (gridSpacing.x * (stepViewPrefab.Length - 1));
            float stepViewHeight = gridViewHeight;

            // StepView Group의 크기
            Vector2Int stepGroupPadding = Vector2Int.RoundToInt(new Vector2(cellSize.x / 2.0f, gridSpacing.y / 2.0f));
            float stepGroupWidth = stepViewWidth + (stepGroupPadding.x * 2);
            float stepGroupHeight = (stepViewHeight * maxSteps) + (gridSpacing.y * (maxSteps - 1)) + (stepGroupPadding.y * 2);
            float stepGroupPosY = -(stepViewHeight + (gridSpacing.y / 2.0f));

            // 전체 Panel 크기
            float panelWidth = stepGroupWidth;
            float panelHeight = stepGroupHeight + (stepViewHeight * 2) + gridSpacing.y;

            // 지정된 영역에 들어맞도록 Scale 조정
            float scaleX = panelWidth / _maxPanelWidth;
            float scaleY = panelHeight / _maxPanelHeight;
            float scale = Mathf.Max(scaleX, scaleY);

            // ----- Apply -----
            // StepView의 크기를 조정하면 GridView의 크기도 자동으로 조정됨 (Control Child Size)
            stepView0.rect.sizeDelta = new Vector2(stepViewWidth, stepViewHeight);
            stepView0.layout.spacing = gridSpacing.x;
            stepViewPrime.rect.sizeDelta = new Vector2(stepViewWidth, stepViewHeight);
            stepViewPrime.layout.spacing = gridSpacing.x;
            stepViewPrefab.rect.sizeDelta = new Vector2(stepViewWidth, stepViewHeight);
            stepViewPrefab.layout.spacing = gridSpacing.x;
            for (int i = 1; i < stepViews.Length; i++)
            {
                stepViews[i].rect.sizeDelta = new Vector2(stepViewWidth, stepViewHeight);
                stepViews[i].layout.spacing = gridSpacing.x;
            }

            // StepView Group 크기와 위치 조정
            var stepViewGroupRect = stepViewGroup.GetComponent<RectTransform>();
            stepViewGroupRect.sizeDelta = new Vector2(stepGroupWidth, stepGroupHeight);
            stepViewGroupRect.anchoredPosition = new Vector2(0, stepGroupPosY);

            // StepView Group Layout 세팅
            var stepViewGroupLayout = stepViewGroup.GetComponent<VerticalLayoutGroup>();
            stepViewGroupLayout.spacing = gridSpacing.y;
            stepViewGroupLayout.padding.top = stepGroupPadding.y;
            stepViewGroupLayout.padding.bottom = stepGroupPadding.y;
            stepViewGroupLayout.padding.left = stepGroupPadding.x;
            stepViewGroupLayout.padding.right = stepGroupPadding.x;

            // Step Seperate Line 위치 조정
            for (int i = 0; i < stepSeperateLines.Length; i++)
            {
                RectTransform line = stepSeperateLines[i];
                float positionY = (panelHeight / 2.0f) + (stepGroupPosY - stepGroupPadding.y + (gridSpacing.y / 2.0f));
                positionY -= i * (stepViewHeight + gridSpacing.y);
                line.anchoredPosition = new Vector2(0, positionY);
                line.localScale = Vector3.one * scale;
                line.gameObject.SetActive(true);
            }

            // Indexer 생성 (123ABC 표시)
            InitIndexers(gridWidth, gridHeight, cellSize, stepGroupPadding.x);

            // BackImageCurStep
            Vector2 backImageCurStep_blankArea = new Vector2(74, 74); // 스프라이트 여백
            Vector2 backImageCurStep_skew = new Vector2(7.5f, -7.5f); // 스프라이트 치우침
            // Sliced Sprite를 사용하는 경우 올바르게 보이기 위해 크기를 2배로 해야 함
            backImageCurStep.sizeDelta = new Vector2(stepGroupWidth, stepViewHeight + gridSpacing.y * 0.75f) * 2;
            backImageCurStep_basePos = new Vector2(0, (panelHeight / 2.0f) - (stepViewHeight / 2.0f)) - backImageCurStep_skew;
            deltaPos = new Vector2(0, -(stepViewHeight + stepViewGroupLayout.spacing));
            SetCurStepUIPos(0);
            // BackImageStepPrime
            backImageStepPrime.sizeDelta = new Vector2(stepGroupWidth, stepViewHeight + gridSpacing.y * 0.75f) * 2 - backImageCurStep_blankArea;
            //backImageStepPrime.anchoredPosition = backImageCurStep_basePos + deltaPos * (_maxSteps + 1) + backImageCurStep_skew;

            // 전체 Panel 크기와 Scale 적용
            panelBoard.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelBoard.localScale = Vector3.one / scale;
        }

        private void InitIndexers(int gridWidth, int gridHeight, Vector2 cellSize, int stepGroupPaddingX)
        {
            Vector2 indexerSize = cellSize / 4.0f;
            rowIndexer.sizeDelta = new Vector2(indexerSize.x, cellSize.y * gridHeight);
            rowIndexer_basePos = new Vector2(stepGroupPaddingX - indexerSize.x, 0);
            rowIndexer.anchoredPosition = rowIndexer_basePos;
            colIndexer.sizeDelta = new Vector2(cellSize.x * gridWidth, indexerSize.y);
            colIndexer_basePos = new Vector2(stepGroupPaddingX, indexerSize.y);
            colIndexer.anchoredPosition = colIndexer_basePos;

            foreach (Transform child in rowIndexer)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in colIndexer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < gridHeight; i++)
            {
                TMP_Text text = Instantiate(textIndexerPrefab, rowIndexer);
                text.text = $"{i + 1}";
            }

            for (int j = 0; j < gridWidth; j++)
            {
                TMP_Text text = Instantiate(textIndexerPrefab, colIndexer);
                text.text = $"{(char)('A' + j)}";
            }
        }

        private void SetCurStepUIPos(int curStep)
        {
            backImageCurStep.anchoredPosition = backImageCurStep_basePos + deltaPos * curStep;
            rowIndexer.anchoredPosition = rowIndexer_basePos + deltaPos * curStep;
            colIndexer.anchoredPosition = colIndexer_basePos + deltaPos * curStep;
        }
    }
}
