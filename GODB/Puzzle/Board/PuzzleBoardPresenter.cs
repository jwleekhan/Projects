using Game.Region;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Puzzle
{
    public sealed class PuzzleBoardPresenter : MonoBehaviour
    {
        public event Action<StepContext> StepChanged;
        public event Action ApplyFailed;
        public event Action<int> PuzzleFailed;
        public event Action PuzzleSuccessed;
        public event Action<OperationSelection, OperationSelection> CellClicked;

        private PuzzleBoardModel boardModel;
        private PuzzleBoardView boardView;

        private RegionPuzzleDatabase _puzzleDB;

        // 퍼즐 정보
        private int _gridWidth, _gridHeight;
        private string[] _symbolNames;

        public void Init(PuzzleDefinition puzzle, RegionPuzzleDatabase puzzleDB)
        {
            _puzzleDB = puzzleDB;
            (_gridWidth, _gridHeight) = (puzzle.width, puzzle.height);
            _symbolNames = puzzle.symbolNames;

            boardModel = new(puzzle);
            SetEventListeners(true);

            if (boardView == null)
                boardView = GetComponent<PuzzleBoardView>();
            InitView(puzzle);
        }

        private void OnDestroy()
        {
            SetEventListeners(false);
        }

        private void SetEventListeners(bool willListen)
        {
            if (willListen)
            {
                SetEventListeners(false);
                boardModel.ApplySuccessed += OnSuccessApply;
                boardModel.ApplyFailed += OnFailApply;
                boardModel.StepChanged += OnChangeStep;
            }
            else
            {
                if (boardModel != null)
                {
                    boardModel.ApplySuccessed -= OnSuccessApply;
                    boardModel.ApplyFailed -= OnFailApply;
                    boardModel.StepChanged -= OnChangeStep;
                }
            }
        }

        // PuzzleBoardModel 이벤트

        private void OnSuccessApply(int prevStep, PlaceableObject[][] newStepData, OperationSelection appliedSelection)
        {
            Sprite[][] stepSprites = GetStepSprites(newStepData);
            string appliedText = PuzzleText.SelectionText(appliedSelection, _symbolNames);
            boardView.UpdateBoard(prevStep, stepSprites, appliedText);
        }

        private void OnFailApply()
        {
            ApplyFailed?.Invoke();
        }

        private void OnChangeStep(StepContext stepContext)
        {
            boardView.HighlightCells(stepContext.prevStep, null);
            boardView.SetCellInteractables(stepContext.prevStep, false);
            boardView.SetCellInteractables(stepContext.curStep, stepContext.curStep < stepContext.maxSteps);
            boardView.UpdateStep(stepContext.curStep);

            StepChanged?.Invoke(stepContext);
        }

        // PuzzleController에서 호출

        public void OnChangeSelection(OperationSelection selection)
        {
            int curStep = boardModel.CurStep;
            PlaceableObject[][] stepData = boardModel.BoardData[curStep];
            SelectionType[][] selectionTypes = new SelectionType[stepData.Length][];

            for (int gridIdx = 0; gridIdx < stepData.Length; gridIdx++)
            {
                selectionTypes[gridIdx] = new SelectionType[stepData[gridIdx].Length];
                for (int cellIdx = 0; cellIdx < stepData[gridIdx].Length; cellIdx++)
                {
                    PlaceableObject cellData = stepData[gridIdx][cellIdx];
                    selectionTypes[gridIdx][cellIdx] = GetCellSelectionType(cellIdx, cellData, selection);
                }
            }

            boardView.HighlightCells(curStep, selectionTypes);
        }

        public void OnClickUndo()
        {
            boardModel.Undo();
        }

        public void OnClickRedo()
        {
            boardModel.Redo();
        }

        public void OnClickApply(PuzzleDefinition puzzle, OperationSelection selection)
        {
            boardModel.ApplyOperation(puzzle, selection);
        }

        // 버튼 OnClick 이벤트

        public void OnClickCell(int stepIdx, int gridIdx, int cellIdx)
        {
            // 위치 선택 생성
            OperationSelection positionSelection = default;
            positionSelection.targetType = TargetType.Position;
            positionSelection.targetNum = PuzzleLogic.IdxToPos(cellIdx, _gridWidth, _gridHeight);

            // 아이템 선택 생성
            OperationSelection itemSelection = default;
            PlaceableObject cellData = boardModel.BoardData[stepIdx][gridIdx][cellIdx];
            if (cellData.specialType == SpecialType.None && cellData.itemNum != 0)
            {
                itemSelection.targetType = TargetType.Item;
                itemSelection.targetNum = cellData.itemNum;
            }
            else if (cellData.specialType != SpecialType.None)
            {
                itemSelection.targetType = TargetType.Special;
                itemSelection.targetNum = (int)cellData.specialType;
            }

            CellClicked?.Invoke(positionSelection, itemSelection);
        }
        
        public void OnDragCell(int stepIdx, int gridIdx, int cellIdx, Vector2 deltaPos)
        {
            // 위치 선택 생성
            OperationSelection positionSelection = default;
            positionSelection.targetType = TargetType.Position;
            if (Mathf.Abs(deltaPos.x) >= Mathf.Abs(deltaPos.y))
            {
                int row = PuzzleLogic.IndexToRow(_gridWidth, _gridHeight, cellIdx);
                positionSelection.targetNum = PuzzleLogic.EncodeRow(row);
            }
            else
            {
                int col = PuzzleLogic.IndexToCol(_gridWidth, _gridHeight, cellIdx);
                positionSelection.targetNum = PuzzleLogic.EncodeCol(col);
            }

            CellClicked?.Invoke(positionSelection, default);
        }

        public void OnClickSubmit()
        {
            var tryStepData = boardModel.BoardData[boardModel.CurStep];
            var answerStepData = boardModel.BoardData[^1];
            if (tryStepData.Length != answerStepData.Length)
            {
                Debug.LogError($"{name}: tryStepData.Length != answerStepData.Length");
                return;
            }

            for (int i = 0; i < answerStepData.Length; i++)
            {
                PlaceableObject[] tryData = tryStepData[i];
                PlaceableObject[] answerData = answerStepData[i];
                if (!PuzzleLogic.IsSame(tryData, answerData))
                {
                    PuzzleFailed?.Invoke(boardModel.CurStep);
                    return;
                }
            }

            PuzzleSuccessed?.Invoke();
        }

        public List<string> GetAppliedTexts()
        {
            List<string> appliedTexts = new();
            for (int i = 0; i < boardModel.AppliedSelections.Length; i++)
            {
                appliedTexts.Add(PuzzleText.SelectionText(boardModel.AppliedSelections[i], _symbolNames));
            }
            return appliedTexts;
        }

        private void InitView(PuzzleDefinition puzzle)
        {
            Sprite[][] stepSprites0 = GetStepSprites(boardModel.BoardData[0]);
            Sprite[][] stepSpritesPrime = GetStepSprites(boardModel.BoardData[^1]);
            boardView.Init(puzzle.width, puzzle.height, puzzle.maxSteps, stepSprites0, stepSpritesPrime);
            boardView.SetCellListeners(OnClickCell, OnDragCell);
            boardView.SetCellInteractables(boardModel.CurStep, true);
        }

        private Sprite[][] GetStepSprites(PlaceableObject[][] stepData)
        {
            Sprite[][] stepSprites = new Sprite[stepData.Length][];
            for (int i = 0; i < stepData.Length; i++)
            {
                stepSprites[i] = new Sprite[stepData[i].Length];
                for (int j = 0; j < stepData[i].Length; j++)
                {
                    PlaceableObject obj = stepData[i][j];

                    string name = "";
                    if (obj.specialType == SpecialType.None)
                    {
                        if (obj.itemNum >= 0 && _symbolNames != null && obj.itemNum < _symbolNames.Length)
                        {
                            name = _symbolNames[obj.itemNum];
                        }
                    }
                    else
                    {
                        name = PuzzleText.SpecialText(obj.specialType);
                    }

                    _puzzleDB.TryGetSprite(name, obj.state, out stepSprites[i][j]);
                }
            }
            return stepSprites;
        }

        private SelectionType GetCellSelectionType(int cellIdx, PlaceableObject cellData, OperationSelection selection)
        {
            if (IsCellMatchedSelection(cellIdx, cellData, selection.targetType, selection.targetNum))
                return SelectionType.Target;
            if (selection.action == ActionType.Interchange && IsCellMatchedSelection(cellIdx, cellData, selection.extraType, selection.extraNum))
                return SelectionType.Extra;
            return SelectionType.None;
        }

        private bool IsCellMatchedSelection(int cellIdx, PlaceableObject cellData, TargetType type, int num)
        {
            if (type == TargetType.Position)
            {
                if (PuzzleLogic.PosToIdxs(num, _gridWidth, _gridHeight).Contains(cellIdx))
                    return true;
            }
            else if (type == TargetType.Item)
            {
                if (num == cellData.itemNum && cellData.specialType == SpecialType.None)
                    return true;
            }
            else if (type == TargetType.Special)
            {
                if (num == (int)cellData.specialType)
                    return true;
            }
            return false;
        }
    }
}
