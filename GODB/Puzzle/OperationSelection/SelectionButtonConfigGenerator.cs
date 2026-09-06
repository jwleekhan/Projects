using System.Collections.Generic;
using System;

namespace Game.Puzzle
{
    public static class SelectionButtonConfigGenerator
    {
        public static List<SelectionButtonConfig> GenerateTargets(Action<SelectionType, OperationSelection> onClick, PuzzleDefinition puzzle)
            => GenerateTargetsOrExtras(onClick, puzzle, SelectionType.Target);

        public static List<SelectionButtonConfig> GenerateActions(Action<SelectionType, OperationSelection> onClick, PuzzleDefinition puzzle)
        {
            List<SelectionButtonConfig> list = new();

            for (int i = 1; i < Enum.GetValues(typeof(ActionType)).Length; i++)
            {
                if (!PuzzleLogic.IsAbleAction(i, puzzle.ableActions))
                    continue;

                OperationSelection selection = new();
                selection.action = (ActionType)i;
                list.Add(new SelectionButtonConfig(PuzzleText.ActionText((ActionType)i), onClick, SelectionType.Action, selection));
            }

            return list;
        }

        public static List<SelectionButtonConfig> GenerateExtras(Action<SelectionType, OperationSelection> onClick, PuzzleDefinition puzzle)
            => GenerateTargetsOrExtras(onClick, puzzle, SelectionType.Extra);

        private static List<SelectionButtonConfig> GenerateTargetsOrExtras(Action<SelectionType, OperationSelection> onClick, PuzzleDefinition puzzle, SelectionType selectionType)
        {
            List<SelectionButtonConfig> list = new();

            if (selectionType == SelectionType.Extra)
            {
                GenDirection(list, puzzle.symbolNames, onClick, selectionType);
            }

            if (PuzzleLogic.IsAbleTarget(TargetType.Position, puzzle.ableTargets))
            {
                GenPosition(list, puzzle.symbolNames, onClick, selectionType, puzzle.width, puzzle.height);
            }

            if (PuzzleLogic.IsAbleTarget(TargetType.Item, puzzle.ableTargets))
            {
                GenItem(list, puzzle.symbolNames, onClick, selectionType);
                GenSpecial(list, puzzle.symbolNames, onClick, selectionType, puzzle.ableSpecials);
            }

            return list;
        }

        private static SelectionButtonConfig GenerateOne(TargetType type, int num, string[] symbolNames,
                                                            Action<SelectionType, OperationSelection> onClick, SelectionType selectionType)
        {
            OperationSelection selection = new();
            if (selectionType == SelectionType.Target)
            {
                selection.targetType = type;
                selection.targetNum = num;
                return new SelectionButtonConfig(PuzzleText.TargetText(type, num, symbolNames), onClick, selectionType, selection);
            }
            else if (selectionType == SelectionType.Extra)
            {
                selection.extraType = type;
                selection.extraNum = num;
                return new SelectionButtonConfig(PuzzleText.ExtraText(type, num, symbolNames), onClick, selectionType, selection);
            }
            return new();
        }

        private static void GenPosition(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType, int width, int height)
        {
            GenRow(list, symbolNames, onClick, selectionType, height);
            GenCol(list, symbolNames, onClick, selectionType, width);
            GenPoint(list, symbolNames, onClick, selectionType, width, height);
        }


        private static void GenRow(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType, int height)
        {
            for (int i = 0; i < height; i++)
            {
                list.Add(GenerateOne(TargetType.Position, PuzzleLogic.EncodeRow(i), symbolNames, onClick, selectionType));
            }
        }

        private static void GenCol(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType, int width)
        {
            for (int i = 0; i < width; i++)
            {
                list.Add(GenerateOne(TargetType.Position, PuzzleLogic.EncodeCol(i), symbolNames, onClick, selectionType));
            }
        }

        private static void GenPoint(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType, int width, int height)
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    list.Add(GenerateOne(TargetType.Position, PuzzleLogic.EncodePoint(i, j), symbolNames, onClick, selectionType));
                }
            }
        }

        private static void GenItem(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType)
        {
            for (int i = 1; i < symbolNames.Length; i++)
            {
                list.Add(GenerateOne(TargetType.Item, i, symbolNames, onClick, selectionType));
            }
        }

        private static void GenDirection(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType)
        {
            if (selectionType != SelectionType.Extra)
                return;

            for (int i = 1; i < Enum.GetValues(typeof(DirectionType)).Length; i++)
            {
                list.Add(GenerateOne(TargetType.Direction, i, symbolNames, onClick, selectionType));
            }
        }

        private static void GenSpecial(List<SelectionButtonConfig> list, string[] symbolNames, Action<SelectionType, OperationSelection> onClick, SelectionType selectionType, List<SpecialType> ableSpecials)
        {
            for (int i = 1; i < Enum.GetValues(typeof(SpecialType)).Length; i++)
            {
                if (!PuzzleLogic.IsSelectableSpecial((SpecialType)i)
                    || !PuzzleLogic.IsAbleSpecial(i, ableSpecials))
                    continue;

                list.Add(GenerateOne(TargetType.Special, i, symbolNames, onClick, selectionType));
            }
        }
    }
}
