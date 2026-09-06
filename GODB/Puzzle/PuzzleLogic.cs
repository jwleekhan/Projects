using System;
using System.Collections.Generic;

namespace Game.Puzzle
{
    public enum TargetType { None, Position, Item, Direction, Special }
    public enum ActionType { None, Move, Interchange, ChangeTo, Delete }

    public enum PosType { None, OutOfRange, Point, Row, Col }
    public enum DirectionType { None, Up, Down, Left, Right }
    public enum SpecialType { None, Fire, Ash }
    public enum StateType { None, Ashed }

    public enum SelectionType { None, Target, Action, Extra }

    [Serializable]
    public struct PlaceableObject
    {
        public int itemNum;
        public StateType state;
        public SpecialType specialType;
        public int runtimeValue;
    }

    public static class PuzzleLogic
    {
        public static bool IsSame(PlaceableObject[] a, PlaceableObject[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].specialType != b[i].specialType) return false;
                
                // 특수 오브젝트는 type까지만 검사
                if (a[i].specialType != SpecialType.None)
                    continue;

                // 일반 아이템에 한해 검사
                if (a[i].itemNum != b[i].itemNum) return false;
                if (a[i].state != b[i].state) return false;
            }

            return true;
        }

        public static bool IsSameSelection(SelectionType selectionType, OperationSelection a, OperationSelection b)
        {
            return selectionType switch
            {
                SelectionType.None => IsSameSelection(SelectionType.Target, a, b)
                                        && IsSameSelection(SelectionType.Action, a, b)
                                        && IsSameSelection(SelectionType.Extra, a, b),
                SelectionType.Target => a.targetType == b.targetType && a.targetNum == b.targetNum,
                SelectionType.Action => a.action == b.action,
                SelectionType.Extra => a.extraType == b.extraType && a.extraNum == b.extraNum,
                _ => false
            };
        }

        public static bool IsAbleTarget(TargetType targetType, List<TargetType> ableTargets)
        {
            if (targetType == TargetType.None)
                return true;

            if (ableTargets == null || ableTargets.Count <= 0)
                return true;

            return ableTargets.Contains(targetType);
        }

        public static bool IsAbleAction(ActionType action, List<ActionType> ableActions)
        {
            if (action == ActionType.None)
                return true;

            if (ableActions == null || ableActions.Count <= 0)
                return true;

            return ableActions.Contains(action);
        }
        public static bool IsAbleAction(int action, List<ActionType> ableActions) => IsAbleAction((ActionType)action, ableActions);

        public static bool IsAbleSpecial(SpecialType specialType, List<SpecialType> ableSpecials)
        {
            if (specialType == SpecialType.None)
                return false;

            // special은 null이면 false
            if (ableSpecials == null || ableSpecials.Count <= 0)
                return false;

            return ableSpecials.Contains(specialType);
        }
        public static bool IsAbleSpecial(int specialType, List<SpecialType> ableSpecials) => IsAbleSpecial((SpecialType)specialType, ableSpecials);

        public static bool CanOperate(OperationSelection selection, bool mustComplete = true)
        {
            SelectionState.State selectionState = SelectionState.GetState(selection);

            if (selectionState == SelectionState.State.Impossible) return false;
            if (mustComplete && selectionState != SelectionState.State.Complete) return false;

            // mustComplete 조건 검증 완료했으므로, None은 허용하되 절대 불가능한 조합일 때만 false
            return selection.action switch
            {
                ActionType.None => true,
                ActionType.Move => PotentialMove(selection),
                ActionType.Interchange => PotentialInterchange(selection),
                ActionType.ChangeTo => PotentialChangeTo(selection),
                ActionType.Delete => PotentialDelete(selection),
                _ => false
            };
        }

        private static bool PotentialMove(OperationSelection selection)
        {
            // Type Check
            TargetType targetType = selection.targetType;
            TargetType extraType = selection.extraType;

            if (targetType != TargetType.None)
            {
                if (targetType == TargetType.Direction) return false;

                if (extraType != TargetType.None)
                {
                    if (extraType != TargetType.Direction) return false;
                }
            }

            return true;
        }

        private static bool PotentialInterchange(OperationSelection selection)
        {
            // Type Check
            TargetType targetType = selection.targetType;
            TargetType extraType = selection.extraType;

            if (targetType != TargetType.None)
            {
                if (targetType == TargetType.Direction) return false;

                if (extraType != TargetType.None)
                {
                    if (targetType == TargetType.Position && extraType != TargetType.Position) return false;
                    if ((targetType == TargetType.Item || targetType == TargetType.Special)
                        && !(extraType == TargetType.Item || extraType == TargetType.Special)) return false;
                }
            }
            
            // Num Check
            int targetNum = selection.targetNum;
            int extraNum = selection.extraNum;

            if (targetType != TargetType.None)
            {
                if (extraType != TargetType.None)
                {
                    if (targetType == extraType && targetNum == extraNum) return false;
                    if (targetType == TargetType.Position && DecodePos(targetNum, out _, out _) != DecodePos(extraNum, out _, out _)) return false;
                }
            }

            return true;
        }

        private static bool PotentialChangeTo(OperationSelection selection)
        {
            // Type Check
            TargetType targetType = selection.targetType;
            TargetType extraType = selection.extraType;

            if (targetType != TargetType.None)
            {
                if (targetType == TargetType.Direction) return false;

                if (extraType != TargetType.None)
                {
                    if (extraType != TargetType.Item && extraType != TargetType.Special) return false;
                }
            }
            
            // Num Check
            int targetNum = selection.targetNum;
            int extraNum = selection.extraNum;

            if (targetType != TargetType.None)
            {
                if (extraType != TargetType.None)
                {
                    if (targetType == extraType && targetNum == extraNum) return false;
                }
            }

            return true;
        }

        private static bool PotentialDelete(OperationSelection selection)
        {
            // Type Check
            TargetType targetType = selection.targetType;
            TargetType extraType = selection.extraType;

            if (targetType != TargetType.None)
            {
                if (targetType == TargetType.Direction) return false;

                if (extraType != TargetType.None)
                {
                    return false;
                }
            }

            return true;
        }

        public static int EncodeRow(int row) => (row + 1) * 10;
        public static int EncodeCol(int col) => (col + 1);
        public static int EncodePoint(int row, int col) => EncodeRow(row) + EncodeCol(col);

        public static PosType DecodePos(int pos, out int row, out int col)
        {
            PosType posType;

            if (pos <= 0)
                posType = PosType.None;

            else if (pos >= 100)
                posType = PosType.OutOfRange;

            else if (pos % 10 == 0)
                posType = PosType.Row;

            else if (pos / 10 == 0)
                posType = PosType.Col;

            else
                posType = PosType.Point;

            row = pos / 10 - 1;
            col = pos % 10 - 1;

            return posType;
        }

        public static PosType DecodeAndValidatePos(int width, int height, int pos, out int row, out int col)
        {
            PosType posType = DecodePos(pos, out row, out col);

            if (row >= height || col >= width)
                return PosType.OutOfRange;

            return posType;
        }

        public static int[] PosToIdxs(int pos, int width, int height)
        {
            PosType posType = DecodePos(pos, out int row, out int col);
            switch (posType)
            {
                case PosType.Point:
                    return new int[1] { GetIndex(width, height, row, col) };
                case PosType.Row:
                    int[] rowIdxs = new int[width];
                    for (int i = 0; i < width; i++)
                    {
                        rowIdxs[i] = GetIndex(width, height, row, i);
                    }
                    return rowIdxs;
                case PosType.Col:
                    int[] colIdxs = new int[height];
                    for (int i = 0; i < height; i++)
                    {
                        colIdxs[i] = GetIndex(width, height, i, col);
                    }
                    return colIdxs;
                default:
                    return null;
            }
        }

        public static int IdxToPos(int idx, int width, int height)
        {
            int row = IndexToRow(width, height, idx);
            int col = IndexToCol(width, height, idx);
            return EncodePoint(row, col);
        }

        /// <summary>
        /// 2차원 row, col을 1차원 index로 변환.
        /// wrap이 true이면 [0, w), [0, h) 범위를 벗어난 값에 대해 순환 보정함.
        /// </summary>
        public static int GetIndex(int width, int height, int row, int col, bool wrap = false)
        {
            if (wrap)
            {
                row %= height;
                if (row < 0) row += height;

                col %= width;
                if (col < 0) col += width;
            }

            return row * width + col;
        }

        public static int IndexToRow(int width, int height, int index) => index / width;

        public static int IndexToCol(int width, int height, int index) => index % width;

        public static ISpecialBehavior GetSpecialBehavior(SpecialType type)
        {
            switch (type)
            {
                case SpecialType.Fire:
                    return new FireBehavior();
                case SpecialType.Ash:
                    return new AshBehavior();
                default:
                    return null;
            }
        }

        public static bool IsSelectableSpecial(SpecialType type)
        {
            ISpecialBehavior specialBehavior = GetSpecialBehavior(type);
            if (specialBehavior == null) return false;
            return specialBehavior.isSelectable;
        }
    }
}
