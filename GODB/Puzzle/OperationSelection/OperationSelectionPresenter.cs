using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Puzzle
{
    public sealed class OperationSelectionPresenter : MonoBehaviour
    {
        public event Action<OperationSelection> SelectionChanged;
        public event Action UndoClicked;
        public event Action RedoClicked;
        public event Action<OperationSelection> ApplyClicked;

        private OperationSelectionModel selectionModel;
        private OperationSelectionView selectionView;

        private string[] _symbolNames;

        public void Init(PuzzleDefinition puzzle)
        {
            _symbolNames = puzzle.symbolNames;

            selectionModel = new(puzzle);
            SetEventListeners(true);

            if (selectionView == null)
                selectionView = GetComponent<OperationSelectionView>();
            InitView(puzzle);

            selectionModel.Select(SelectionType.None, default);
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
                selectionModel.SelectionChanged += OnChangeSelection;
            }
            else
            {
                if (selectionModel != null)
                {
                    selectionModel.SelectionChanged -= OnChangeSelection;
                }
            }
        }

        // OperationSelectionModel 이벤트

        private void OnChangeSelection(OperationSelection selection)
        {
            UpdateView(selection);

            SelectionChanged?.Invoke(selection);
        }

        // PuzzleController에서 호출

        public void OnChangeStep(StepContext stepContext)
        {
            selectionModel.Select(SelectionType.None, default);

            selectionView.ResetScroll();
            selectionView.SetUndoInteractable(stepContext.canUndo);
            selectionView.SetRedoInteractable(stepContext.canRedo);
            selectionView.SetSubmitInteractable(stepContext.canSubmit);

            selectionModel.Lock(stepContext.curStep >= stepContext.maxSteps);
        }

        public void OnFailApply()
        {
            selectionView.ShowImageApplyFail();
        }

        public void OnClickCell(OperationSelection positionSelection, OperationSelection itemSelection)
        {
            bool isAblePosition = selectionModel.IsAbleSelection(SelectionType.Target, positionSelection);
            bool isAbleItem = selectionModel.IsAbleSelection(SelectionType.Target, itemSelection);

            OperationSelection curSelection = selectionModel.CurSelection;
            bool isCurNone = curSelection.targetType == TargetType.None;
            bool isSamePosition = PuzzleLogic.IsSameSelection(SelectionType.Target, curSelection, positionSelection);
            bool isSameItem = PuzzleLogic.IsSameSelection(SelectionType.Target, curSelection, itemSelection);

            // 기존 선택이 없거나 기존 선택과 다르면 위치를 선택
            if (isAblePosition && (isCurNone || (!isSamePosition && !isSameItem)))
                selectionModel.Select(SelectionType.Target, positionSelection);
            // 기존 아이템과 다르면 아이템을 선택
            else if (isAbleItem && !isSameItem)
                selectionModel.Select(SelectionType.Target, itemSelection);
            // 그 외에는 선택 해제
            else
                selectionModel.Select(SelectionType.Target, default);
        }

        // 버튼 OnClick 이벤트

        public void OnClickUndo()
        {
            UndoClicked?.Invoke();
        }

        public void OnClickRedo()
        {
            RedoClicked?.Invoke();
        }

        public void OnClickApply()
        {
            ApplyClicked?.Invoke(selectionModel.CurSelection);
        }

        private void InitView(PuzzleDefinition puzzle)
        {
            List<SelectionButtonConfig> targetButtonConfigs = SelectionButtonConfigGenerator.GenerateTargets(selectionModel.Select, puzzle);
            List<SelectionButtonConfig> actionButtonConfigs = SelectionButtonConfigGenerator.GenerateActions(selectionModel.Select, puzzle);
            List<SelectionButtonConfig> extraButtonConfigs = SelectionButtonConfigGenerator.GenerateExtras(selectionModel.Select, puzzle);

            selectionView.InitSelectionButtons(targetButtonConfigs, actionButtonConfigs, extraButtonConfigs);
            selectionView.SetUndoInteractable(false);
            selectionView.SetRedoInteractable(false);
            selectionView.SetSubmitInteractable(false);
        }

        private void UpdateView(OperationSelection selection)
        {
            selectionView.UpdateSelectionButtons(selection, (SelectionButton btn) => selectionModel.CanSelect(selection, btn.Selection));

            SelectionState.State selectionState = SelectionState.GetState(selection);
            if (selectionState == SelectionState.State.Complete)
            {
                selectionView.UpdateApply(true, PuzzleText.SelectionText(selection, _symbolNames, formatted: true));
            }
            else
            {
                selectionView.UpdateApply(false, "");
            }
        }
    }
}
