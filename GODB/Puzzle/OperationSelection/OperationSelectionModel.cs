using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Puzzle
{
    public struct OperationSelection
    {
        public TargetType targetType;
        public int targetNum;
        public ActionType action;
        public TargetType extraType;
        public int extraNum;
    }

    public static class SelectionState
    {
        public enum State { Init, TargetOnlySelected, ActionOnlySelected, TargetAndActionSelected, Complete, Impossible }

        public static State GetState(OperationSelection selection)
        {
            TargetType targetType = selection.targetType;
            ActionType action = selection.action;
            TargetType extraType = selection.extraType;

            if (targetType == TargetType.None && action == ActionType.None && extraType == TargetType.None)
                return State.Init;

            if (targetType != TargetType.None && action == ActionType.None && extraType == TargetType.None)
                return State.TargetOnlySelected;

            if (targetType == TargetType.None && action != ActionType.None && extraType == TargetType.None)
                return State.ActionOnlySelected;

            if (targetType != TargetType.None && action != ActionType.None && extraType == TargetType.None && IsExtraRequired(action))
                return State.TargetAndActionSelected;

            if (targetType != TargetType.None && action != ActionType.None && (extraType != TargetType.None || !IsExtraRequired(action)))
                return State.Complete;

            else
                return State.Impossible;
        }

        private static bool IsExtraRequired(ActionType action)
            => action != ActionType.Delete;
    }

    public sealed class OperationSelectionModel
    {
        public event Action<OperationSelection> SelectionChanged;

        private bool _isLocked = false;
        public bool IsLocked => _isLocked;

        private OperationSelection _curSelection = default;
        public OperationSelection CurSelection => _curSelection;

        // 가능한 선택 옵션
        private List<TargetType> ableTargets;
        private List<ActionType> ableActions;
        private List<SpecialType> ableSpecials;

        public OperationSelectionModel(PuzzleDefinition puzzle)
        {
            ableTargets = puzzle.ableTargets;
            ableActions = puzzle.ableActions;
            ableSpecials = puzzle.ableSpecials;
        }

        public void Lock(bool setLock)
        {
            _isLocked = setLock;
            SelectionChanged?.Invoke(_curSelection);
        }

        public bool IsAbleSelection(SelectionType type, OperationSelection selection)
        {
            switch (type)
            {
                case SelectionType.Target:
                    if (!PuzzleLogic.IsAbleTarget(selection.targetType, ableTargets)) return false;
                    if (selection.targetType == TargetType.Special)
                    {
                        if (!PuzzleLogic.IsAbleSpecial(selection.targetNum, ableSpecials)) return false;
                        if (!PuzzleLogic.IsSelectableSpecial((SpecialType)selection.targetNum)) return false;
                    }
                    return true;

                case SelectionType.Action:
                    return PuzzleLogic.IsAbleAction(selection.action, ableActions);

                case SelectionType.Extra:
                    if (!PuzzleLogic.IsAbleTarget(selection.extraType, ableTargets)) return false;
                    if (selection.extraType == TargetType.Special)
                    {
                        if (!PuzzleLogic.IsAbleSpecial(selection.extraNum, ableSpecials)) return false;
                        if (!PuzzleLogic.IsSelectableSpecial((SpecialType)selection.extraNum)) return false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        public void Select(SelectionType selectionType, OperationSelection newSelection)
        {
            if (_isLocked)
                return;

            bool isChanged;
            switch (selectionType)
            {
                case SelectionType.None:
                    isChanged = SelectDefault();
                    break;
                case SelectionType.Target:
                    isChanged = SelectTarget(newSelection);
                    break;
                case SelectionType.Action:
                    isChanged = SelectAction(newSelection);
                    break;
                case SelectionType.Extra:
                    isChanged = SelectExtra(newSelection);
                    break;
                default:
                    return;
            }
            if (isChanged) SelectionChanged?.Invoke(_curSelection);
        }

        private bool SelectDefault()
        {
            _curSelection = default;
            return true;
        }

        private bool SelectTarget(OperationSelection newSelection)
        {
            TargetType newType = newSelection.targetType;
            int newNum = newSelection.targetNum;

            if (newType == _curSelection.targetType && newNum == _curSelection.targetNum)
            {
                newType = TargetType.None;
                newNum = 0;
            }

            else if (!IsAbleSelection(SelectionType.Target, newSelection))
            {
                Debug.LogError("[OperationSelectionData] Disabled target is selected");
                return false;
            }

            _curSelection.targetType = newType;
            _curSelection.targetNum = newNum;

            _curSelection.extraType = TargetType.None;
            _curSelection.extraNum = 0;

            return true;
        }

        private bool SelectAction(OperationSelection newSelection)
        {
            ActionType newAction = newSelection.action;

            if (newAction == _curSelection.action)
            {
                newAction = ActionType.None;
            }

            else if (!IsAbleSelection(SelectionType.Action, newSelection))
            {
                Debug.LogError("[OperationSelectionData] Disabled action is selected");
                return false;
            }

            _curSelection.action = newAction;

            _curSelection.extraType = TargetType.None;
            _curSelection.extraNum = 0;

            return true;
        }

        private bool SelectExtra(OperationSelection newSelection)
        {
            TargetType newType = newSelection.extraType;
            int newNum = newSelection.extraNum;

            if (newType == _curSelection.extraType && newNum == _curSelection.extraNum)
            {
                newType = TargetType.None;
                newNum = 0;
            }

            else if (!IsAbleSelection(SelectionType.Extra, newSelection))
            {
                Debug.LogError("[OperationSelectionData] Disabled extra is selected");
                return false;
            }

            _curSelection.extraType = newType;
            _curSelection.extraNum = newNum;

            return true;
        }

        public bool CanSelect(OperationSelection curSelection, OperationSelection btnSelection)
        {
            if (_isLocked)
                return false;

            OperationSelection newSelection = curSelection;

            if (btnSelection.targetType != TargetType.None)
            {
                newSelection.targetType = btnSelection.targetType;
                newSelection.targetNum = btnSelection.targetNum;

                newSelection.extraType = TargetType.None;
                newSelection.extraNum = 0;
            }

            if (btnSelection.action != ActionType.None)
            {
                newSelection.action = btnSelection.action;

                newSelection.extraType = TargetType.None;
                newSelection.extraNum = 0;
            }

            if (btnSelection.extraType != TargetType.None)
            {
                newSelection.extraType = btnSelection.extraType;
                newSelection.extraNum = btnSelection.extraNum;
            }

            return PuzzleLogic.CanOperate(newSelection, mustComplete: false);
        }
    }
}
