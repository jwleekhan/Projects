using System;

namespace Game.Puzzle
{
    public enum OperationError { None, Error } // TODO: 추후 확장

    public static class PuzzleOperation
    {
        private struct Grid
        {
            public readonly int width;
            public readonly int height;
            public readonly PlaceableObject[] data;

            public Grid(int _width, int _height, PlaceableObject[] _data)
            {
                width = _width;
                height = _height;
                data = _data;
            }
        }

        /// <summary>
        /// puzzle 정의에 따라 data에 연산을 적용.
        /// 반환값이 0이 아니면 data는 폐기해야 함.
        /// </summary>
        public static OperationError ApplyOperation(PuzzleDefinition puzzle, PlaceableObject[] data, OperationSelection selection)
        {
            // 검증 단계
            if (puzzle == null || data == null)
            {
                return OperationError.Error;
            }

            int w = puzzle.width;
            int h = puzzle.height;
            if (w * h != data.Length)
            {
                // geometry가 맞지 않음
                return OperationError.Error;
            }
            int maxItem = puzzle.symbolNames.Length - 1;

            OperationError error = ValidateSelection(selection, maxItem);
            if (error != 0)
                return error;

            Grid grid = new Grid(w, h, data);

            // 분기
            if (selection.targetType == TargetType.Position)
            {
                PosType posType = PuzzleLogic.DecodeAndValidatePos(w, h, selection.targetNum, out int row, out int col);

                switch (posType)
                {
                    case PosType.None:
                        return OperationError.Error;

                    case PosType.OutOfRange:
                        return OperationError.Error;

                    case PosType.Point:
                        return PointOperation(grid, row, col, selection.action, selection.extraType, selection.extraNum);

                    case PosType.Row:
                        return RowOperation(grid, row, selection.action, selection.extraType, selection.extraNum);

                    case PosType.Col:
                        return ColOperation(grid, col, selection.action, selection.extraType, selection.extraNum);

                    default:
                        return OperationError.Error;
                }
            }

            else if (selection.targetType == TargetType.Item)
            {
                return ItemOperation(grid, selection.targetNum, selection.action, selection.extraType, selection.extraNum);
            }

            else if (selection.targetType == TargetType.Special)
            {
                return SpecialOperation(grid, (SpecialType)selection.targetNum, selection.action, selection.extraType, selection.extraNum);
            }

            return OperationError.Error;
        }

        public static OperationError PostOperation(PlaceableObject[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                ISpecialBehavior specialBehavior = PuzzleLogic.GetSpecialBehavior(data[i].specialType);
                if (specialBehavior != null)
                {
                    bool success = specialBehavior.OnFinishOperation(data[i], out data[i]);
                    if (!success)
                    {
                        return OperationError.Error;
                    }
                }
            }
            return 0;
        }

        private static OperationError ValidateSelection(OperationSelection selection, int maxItem)
        {
            if (selection.targetType <= 0 || selection.action <= 0 || selection.extraType < 0)
            {
                return OperationError.Error;
            }
            if (selection.targetNum < 0 || selection.extraNum < 0)
            {
                return OperationError.Error;
            }
            if (selection.targetType == TargetType.Item)
            {
                OperationError error = ValidateItem(selection.targetNum, maxItem);
                if (error != 0)
                    return error;
            }
            if (selection.extraType == TargetType.Item)
            {
                OperationError error = ValidateItem(selection.extraNum, maxItem);
                if (error != 0)
                    return error;
            }
            if (selection.targetType == TargetType.Special)
            {
                OperationError error = ValidateSpecial(selection.targetNum);
                if (error != 0)
                    return error;
            }
            if (selection.extraType == TargetType.Special)
            {
                OperationError error = ValidateSpecial(selection.extraNum);
                if (error != 0)
                    return error;
            }
            if (!PuzzleLogic.CanOperate(selection))
            {
                return OperationError.Error;
            }
            return 0;
        }

        private static OperationError ValidateItem(int item, int maxItem)
        {
            if (item > maxItem || item <= 0)
                return OperationError.Error;

            return 0;
        }

        private static OperationError ValidateSpecial(int special)
        {
            if (!Enum.IsDefined(typeof(SpecialType), special) || special <= 0)
                return OperationError.Error;
            return 0;
        }

        private static OperationError ResolveCollision(PlaceableObject a, PlaceableObject b, out PlaceableObject result)
        {
            // 둘 중 하나가 빈칸인 경우
            if (a.itemNum == 0 && a.specialType == 0)
            {
                result = b;
                return 0;
            }
            if (b.itemNum == 0 && b.specialType == 0)
            {
                result = a;
                return 0;
            }

            SpecialType priorityType = SpecialOperationPriority.CollisionFuncPriority(a.specialType, b.specialType);
            // 특수 오브젝트와 충돌
            if (priorityType != 0)
            {
                if (PuzzleLogic.GetSpecialBehavior(priorityType).OnCollide(a, b, out result))
                    return 0;
                else
                    return OperationError.Error;
            }

            // 일반 아이템 간 충돌 시 실패 리턴
            result = b;
            return OperationError.Error;
        }

        private static OperationError TryMoveCell(PlaceableObject[] data, int src, int dest)
        {
            // 충돌 처리
            OperationError error = ResolveCollision(data[src], data[dest], out var result);
            if (error != 0)
                return error;

            data[dest] = result;
            data[src] = new PlaceableObject();
            return 0;
        }

        private static void Swap(PlaceableObject[] data, int src, int dest)
        {
            PlaceableObject item = data[src];
            data[src] = data[dest];
            data[dest] = item;
        }

        private static int GetIndex(int w, int h, int row, int col, bool wrap = false) => PuzzleLogic.GetIndex(w, h, row, col, wrap);

        private static OperationError PointOperation(Grid grid, int row, int col, ActionType action, TargetType extraType, int extraNum)
        {
            switch (action)
            {
                case ActionType.Move:
                    return PointMove(grid, row, col, extraNum);

                case ActionType.Interchange:
                    PosType posType = PuzzleLogic.DecodeAndValidatePos(grid.width, grid.height, extraNum, out int destRow, out int destCol);
                    if (posType != PosType.Point)
                        return OperationError.Error;
                    return PointInterchange(grid, row, col, destRow, destCol);

                case ActionType.ChangeTo:
                    if (extraType == TargetType.Item)
                        return PointChangeToItem(grid, row, col, extraNum);
                    else if (extraType == TargetType.Special)
                        return PointChangeToSpecial(grid, row, col, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.Delete:
                    return PointDelete(grid, row, col);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError RowOperation(Grid grid, int row, ActionType action, TargetType extraType, int extraNum)
        {
            switch (action)
            {
                case ActionType.Move:
                    return RowMove(grid, row, extraNum);

                case ActionType.Interchange:
                    PosType posType = PuzzleLogic.DecodeAndValidatePos(grid.width, grid.height, extraNum, out int destRow, out _);
                    if (posType != PosType.Row)
                        return OperationError.Error;
                    return RowInterchange(grid, row, destRow);

                case ActionType.ChangeTo:
                    if (extraType == TargetType.Item)
                        return RowChangeToItem(grid, row, extraNum);
                    else if (extraType == TargetType.Special)
                        return RowChangeToSpecial(grid, row, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.Delete:
                    return RowDelete(grid, row);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError ColOperation(Grid grid, int col, ActionType action, TargetType extraType, int extraNum)
        {
            switch (action)
            {
                case ActionType.Move:
                    return ColMove(grid, col, extraNum);

                case ActionType.Interchange:
                    PosType posType = PuzzleLogic.DecodeAndValidatePos(grid.width, grid.height, extraNum, out _, out int destCol);
                    if (posType != PosType.Col)
                        return OperationError.Error;
                    return ColInterchange(grid, col, destCol);

                case ActionType.ChangeTo:
                    if (extraType == TargetType.Item)
                        return ColChangeToItem(grid, col, extraNum);
                    else if (extraType == TargetType.Special)
                        return ColChangeToSpecial(grid, col, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.Delete:
                    return ColDelete(grid, col);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError ItemOperation(Grid grid, int item, ActionType action, TargetType extraType, int extraNum)
        {
            switch (action)
            {
                case ActionType.Move:
                    return ItemMove(grid, item, extraNum);

                case ActionType.Interchange:
                    if (extraType == TargetType.Item)
                        return ItemInterchangeItem(grid, item, extraNum);
                    else if (extraType == TargetType.Special)
                        return ItemInterchangeSpecial(grid, item, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.ChangeTo:
                    if (extraType == TargetType.Item)
                        return ItemChangeToItem(grid, item, extraNum);
                    else if (extraType == TargetType.Special)
                        return ItemChangeToSpecial(grid, item, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.Delete:
                    return ItemDelete(grid, item);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError SpecialOperation(Grid grid, SpecialType specialType, ActionType action, TargetType extraType, int extraNum)
        {
            switch (action)
            {
                case ActionType.Move:
                    return SpecialMove(grid, specialType, extraNum);

                case ActionType.Interchange:
                    if (extraType == TargetType.Item)
                        return SpecialInterchangeItem(grid, specialType, extraNum);
                    else if (extraType == TargetType.Special)
                        return SpecialInterchangeSpecial(grid, specialType, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.ChangeTo:
                    if (extraType == TargetType.Item)
                        return SpecialChangeToItem(grid, specialType, extraNum);
                    else if (extraType == TargetType.Special)
                        return SpecialChangeToSpecial(grid, specialType, (SpecialType)extraNum);
                    return OperationError.Error;

                case ActionType.Delete:
                    return SpecialDelete(grid, specialType);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError PointMove(Grid grid, int row, int col, int dir)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int index = GetIndex(w, h, row, col);
            int dest = 0;
            switch ((DirectionType)dir)
            {
                case DirectionType.Up:
                    dest = GetIndex(w, h, row - 1, col, wrap: true);
                    break;

                case DirectionType.Down:
                    dest = GetIndex(w, h, row + 1, col, wrap: true);
                    break;

                case DirectionType.Left:
                    dest = GetIndex(w, h, row, col - 1, wrap: true);
                    break;

                case DirectionType.Right:
                    dest = GetIndex(w, h, row, col + 1, wrap: true);
                    break;

                default:
                    return OperationError.Error;
            }

            OperationError error = TryMoveCell(data, index, dest);
            if (error != 0)
                return error;

            return 0;
        }

        private static OperationError RowMove(Grid grid, int row, int dir)
        {
            switch ((DirectionType)dir)
            {
                case DirectionType.Up:
                    return RowMoveUp(grid, row);

                case DirectionType.Down:
                    return RowMoveDown(grid, row);

                case DirectionType.Left:
                    return RowMoveLeft(grid, row);

                case DirectionType.Right:
                    return RowMoveRight(grid, row);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError RowMoveUp(Grid grid, int row)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);
            int destStart = GetIndex(w, h, row - 1, 0, wrap: true);

            for (int i = 0; i < w; i++)
            {
                int index = rowStart + i;
                int dest = destStart + i;

                OperationError error = TryMoveCell(data, index, dest);
                if (error != 0)
                    return error;
            }

            return 0;
        }

        private static OperationError RowMoveDown(Grid grid, int row)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);
            int destStart = GetIndex(w, h, row + 1, 0, wrap: true);

            for (int i = 0; i < w; i++)
            {
                int index = rowStart + i;
                int dest = destStart + i;

                OperationError error = TryMoveCell(data, index, dest);
                if (error != 0)
                    return error;
            }

            return 0;
        }

        private static OperationError RowMoveLeft(Grid grid, int row)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);

            PlaceableObject first = data[rowStart];
            for (int i = 0; i < w - 1; i++)
            {
                data[rowStart + i] = data[rowStart + i + 1];
            }
            data[rowStart + w - 1] = first;

            return 0;
        }

        private static OperationError RowMoveRight(Grid grid, int row)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);

            PlaceableObject last = data[rowStart + w - 1];
            for (int i = w - 1; i > 0; i--)
            {
                data[rowStart + i] = data[rowStart + i - 1];
            }
            data[rowStart] = last;

            return 0;
        }

        private static OperationError ColMove(Grid grid, int col, int dir)
        {
            switch ((DirectionType)dir)
            {
                case DirectionType.Up:
                    return ColMoveUp(grid, col);

                case DirectionType.Down:
                    return ColMoveDown(grid, col);

                case DirectionType.Left:
                    return ColMoveLeft(grid, col);

                case DirectionType.Right:
                    return ColMoveRight(grid, col);

                default:
                    return OperationError.Error;
            }
        }

        private static OperationError ColMoveUp(Grid grid, int col)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            PlaceableObject first = data[col];
            for (int i = 0; i < h - 1; i++)
            {
                data[GetIndex(w, h, i, col)] = data[GetIndex(w, h, i + 1, col)];
            }
            data[GetIndex(w, h, h - 1, col)] = first;

            return 0;
        }

        private static OperationError ColMoveDown(Grid grid, int col)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            PlaceableObject last = data[GetIndex(w, h, h - 1, col)];
            for (int i = h - 1; i > 0; i--)
            {
                data[GetIndex(w, h, i, col)] = data[GetIndex(w, h, i - 1, col)];
            }
            data[col] = last;

            return 0;
        }

        private static OperationError ColMoveLeft(Grid grid, int col)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < h; i++)
            {
                int index = GetIndex(w, h, i, col);
                int dest = GetIndex(w, h, i, col - 1, wrap: true);

                OperationError error = TryMoveCell(data, index, dest);
                if (error != 0)
                    return error;
            }

            return 0;
        }

        private static OperationError ColMoveRight(Grid grid, int col)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < h; i++)
            {
                int index = GetIndex(w, h, i, col);
                int dest = GetIndex(w, h, i, col + 1, wrap: true);

                OperationError error = TryMoveCell(data, index, dest);
                if (error != 0)
                    return error;
            }

            return 0;
        }

        private static OperationError ItemMove(Grid grid, int item, int dir)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            PlaceableObject[] origin = (PlaceableObject[])data.Clone();

            for (int i = 0; i < origin.Length; i++)
            {
                if (origin[i].itemNum == item)
                    data[i] = new PlaceableObject();
            }

            for (int i = 0; i < origin.Length; i++)
            {
                if (origin[i].itemNum == item)
                {
                    int row = PuzzleLogic.IndexToRow(w, h, i);
                    int col = PuzzleLogic.IndexToCol(w, h, i);
                    int dest = 0;
                    switch ((DirectionType)dir)
                    {
                        case DirectionType.Up:
                            dest = GetIndex(w, h, row - 1, col, wrap: true);
                            break;

                        case DirectionType.Down:
                            dest = GetIndex(w, h, row + 1, col, wrap: true);
                            break;

                        case DirectionType.Left:
                            dest = GetIndex(w, h, row, col - 1, wrap: true);
                            break;

                        case DirectionType.Right:
                            dest = GetIndex(w, h, row, col + 1, wrap: true);
                            break;

                        default:
                            return OperationError.Error;
                    }

                    if (origin[dest].itemNum == item && origin[dest].specialType == 0)
                    {
                        data[dest] = origin[i];
                    }
                    else
                    {
                        OperationError error = ResolveCollision(origin[i], origin[dest], out var result);
                        if (error != 0)
                            return error;
                        data[dest] = result;
                    }
                }
            }

            return 0;
        }

        private static OperationError SpecialMove(Grid grid, SpecialType specialType, int dir)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            PlaceableObject[] origin = (PlaceableObject[])data.Clone();

            for (int i = 0; i < origin.Length; i++)
            {
                if (origin[i].specialType == specialType)
                    data[i] = new PlaceableObject();
            }

            for (int i = 0; i < origin.Length; i++)
            {
                if (origin[i].specialType == specialType)
                {
                    int row = PuzzleLogic.IndexToRow(w, h, i);
                    int col = PuzzleLogic.IndexToCol(w, h, i);
                    int dest = 0;
                    switch ((DirectionType)dir)
                    {
                        case DirectionType.Up:
                            dest = GetIndex(w, h, row - 1, col, wrap: true);
                            break;

                        case DirectionType.Down:
                            dest = GetIndex(w, h, row + 1, col, wrap: true);
                            break;

                        case DirectionType.Left:
                            dest = GetIndex(w, h, row, col - 1, wrap: true);
                            break;

                        case DirectionType.Right:
                            dest = GetIndex(w, h, row, col + 1, wrap: true);
                            break;

                        default:
                            return OperationError.Error;
                    }

                    if (origin[dest].specialType == specialType)
                    {
                        data[dest] = origin[i];
                    }
                    else
                    {
                        OperationError error = ResolveCollision(origin[i], origin[dest], out var result);
                        if (error != 0)
                            return error;
                        data[dest] = result;
                    }
                }
            }

            return 0;
        }

        private static OperationError PointInterchange(Grid grid, int row, int col, int destRow, int destCol)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int index = GetIndex(w, h, row, col);
            int dest = GetIndex(w, h, destRow, destCol);

            Swap(data, index, dest);

            return 0;
        }

        private static OperationError RowInterchange(Grid grid, int row, int destRow)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);
            int destStart = GetIndex(w, h, destRow, 0);

            for (int i = 0; i < w; i++)
            {
                int index = rowStart + i;
                int dest = destStart + i;

                Swap(data, index, dest);
            }

            return 0;
        }

        private static OperationError ColInterchange(Grid grid, int col, int destCol)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < h; i++)
            {
                int index = GetIndex(w, h, i, col);
                int dest = GetIndex(w, h, i, destCol);

                Swap(data, index, dest);
            }

            return 0;
        }

        private static OperationError ItemInterchangeItem(Grid grid, int item, int destItem)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].itemNum == item && data[i].specialType == 0)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = destItem;
                }
                else if (data[i].itemNum == destItem && data[i].specialType == 0)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = item;
                }
            }

            return 0;
        }

        private static OperationError ItemInterchangeSpecial(Grid grid, int item, SpecialType specialType)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].itemNum == item && data[i].specialType == 0)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = specialType;
                }
                else if (data[i].specialType == specialType)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = item;
                }
            }

            return 0;
        }

        private static OperationError SpecialInterchangeItem(Grid grid, SpecialType specialType, int item)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].specialType == specialType)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = item;
                }
                else if (data[i].itemNum == item && data[i].specialType == 0)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = specialType;
                }
            }

            return 0;
        }

        private static OperationError SpecialInterchangeSpecial(Grid grid, SpecialType specialType, SpecialType destSpecial)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].specialType == specialType)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = destSpecial;
                }
                else if (data[i].specialType == destSpecial)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = specialType;
                }
            }

            return 0;
        }

        private static OperationError PointChangeToItem(Grid grid, int row, int col, int item)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int index = GetIndex(w, h, row, col);

            data[index] = new PlaceableObject();
            data[index].itemNum = item;

            return 0;
        }

        private static OperationError PointChangeToSpecial(Grid grid, int row, int col, SpecialType specialType)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int index = GetIndex(w, h, row, col);

            data[index] = new PlaceableObject();
            data[index].specialType = specialType;

            return 0;
        }

        private static OperationError RowChangeToItem(Grid grid, int row, int item)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);

            for (int i = 0; i < w; i++)
            {
                data[rowStart + i] = new PlaceableObject();
                data[rowStart + i].itemNum = item;
            }

            return 0;
        }

        private static OperationError RowChangeToSpecial(Grid grid, int row, SpecialType specialType)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            int rowStart = GetIndex(w, h, row, 0);

            for (int i = 0; i < w; i++)
            {
                data[rowStart + i] = new PlaceableObject();
                data[rowStart + i].specialType = specialType;
            }

            return 0;
        }

        private static OperationError ColChangeToItem(Grid grid, int col, int item)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < h; i++)
            {
                data[GetIndex(w, h, i, col)] = new PlaceableObject();
                data[GetIndex(w, h, i, col)].itemNum = item;
            }

            return 0;
        }

        private static OperationError ColChangeToSpecial(Grid grid, int col, SpecialType specialType)
        {
            int w = grid.width;
            int h = grid.height;
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < h; i++)
            {
                data[GetIndex(w, h, i, col)] = new PlaceableObject();
                data[GetIndex(w, h, i, col)].specialType = specialType;
            }

            return 0;
        }

        private static OperationError ItemChangeToItem(Grid grid, int item, int destItem)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].itemNum == item)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = destItem;
                }
            }

            return 0;
        }

        private static OperationError ItemChangeToSpecial(Grid grid, int item, SpecialType specialType)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].itemNum == item)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = specialType;
                }
            }

            return 0;
        }

        private static OperationError SpecialChangeToItem(Grid grid, SpecialType specialType, int item)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].specialType == specialType)
                {
                    data[i] = new PlaceableObject();
                    data[i].itemNum = item;
                }
            }

            return 0;
        }

        private static OperationError SpecialChangeToSpecial(Grid grid, SpecialType specialType, SpecialType destSpecial)
        {
            PlaceableObject[] data = grid.data;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].specialType == specialType)
                {
                    data[i] = new PlaceableObject();
                    data[i].specialType = destSpecial;
                }
            }

            return 0;
        }

        private static OperationError PointDelete(Grid grid, int row, int col)
            => PointChangeToItem(grid, row, col, 0);

        private static OperationError RowDelete(Grid grid, int row)
            => RowChangeToItem(grid, row, 0);

        private static OperationError ColDelete(Grid grid, int col)
            => ColChangeToItem(grid, col, 0);

        private static OperationError ItemDelete(Grid grid, int item)
            => ItemChangeToItem(grid, item, 0);

        private static OperationError SpecialDelete(Grid grid, SpecialType specialType)
            => SpecialChangeToItem(grid, specialType, 0);
    }
}
