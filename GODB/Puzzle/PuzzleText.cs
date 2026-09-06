using Game.Util;
using System.Collections.Generic;

namespace Game.Puzzle
{
    public static class PuzzleText
    {
        private static readonly Dictionary<ActionType, string> _actionTexts = new()
        {
            { ActionType.None, "" },
            { ActionType.Move, "1칸 이동" },
            { ActionType.Interchange, "교환" },
            { ActionType.ChangeTo, "변경" },
            { ActionType.Delete, "삭제" }
        };

        private static readonly Dictionary<DirectionType, string> _directionTexts = new()
        {
            { DirectionType.None, "" },
            { DirectionType.Up, "위쪽" },
            { DirectionType.Down, "아래쪽" },
            { DirectionType.Left, "왼쪽" },
            { DirectionType.Right, "오른쪽" }
        };

        private static readonly Dictionary<SpecialType, string> _specialTexts = new()
        {
            { SpecialType.None, "" },
            { SpecialType.Fire, "불" },
            { SpecialType.Ash, "잿더미" }
        };

        public static string RowText(int row) => $"{row + 1}행";
        public static string ColText(int col) => $"{(char)('A' + col)}열";
        public static string PointText(int row, int col) => $"{RowText(row)} {ColText(col)}";
        public static string ActionText(ActionType type) => _actionTexts[type];
        public static string DirectionText(DirectionType type) => _directionTexts[type];
        public static string SpecialText(SpecialType type) => _specialTexts[type];

        public static string PosText(int pos)
        {
            PosType posType = PuzzleLogic.DecodePos(pos, out int row, out int col);
            return posType switch
            {
                PosType.Row => RowText(row),
                PosType.Col => ColText(col),
                PosType.Point => PointText(row, col),
                _ => ""
            };
        }

        public static string TargetText(TargetType targetType, int targetNum, string[] symbolNames)
        {
            return targetType switch
            {
                TargetType.Position => PosText(targetNum),
                TargetType.Item => (symbolNames != null) ? symbolNames[targetNum] : "",
                TargetType.Direction => DirectionText((DirectionType)targetNum),
                TargetType.Special => SpecialText((SpecialType)targetNum),
                _ => ""
            };
        }

        public static string ExtraText(TargetType extraType, int extraNum, string[] symbolNames)
            => TargetText(extraType, extraNum, symbolNames);

        private static string TargetJosa(string targetText)
            => KoreanTextUtil.ChooseJosa(targetText, "을 ", "를 ", false);
        
        private static string ExtraJosa(ActionType action, string extraText)
        {
            string extraJosa = "";
            switch (action)
            {
                case ActionType.Move:
                    extraJosa += KoreanTextUtil.ChooseJosa(extraText, "으로 ", "로 ", true);
                    break;

                case ActionType.Interchange:
                    extraJosa += KoreanTextUtil.ChooseJosa(extraText, "과 ", "와 ", false);
                    break;

                case ActionType.ChangeTo:
                    extraJosa += KoreanTextUtil.ChooseJosa(extraText, "으로 ", "로 ", true);
                    break;
            }
            return extraJosa;
        }

        public static string SelectionText(OperationSelection selection, string[] symbolNames, bool formatted = false)
        {
            string targetText = TargetText(selection.targetType, selection.targetNum, symbolNames);
            string extraText = ExtraText(selection.extraType, selection.extraNum, symbolNames);

            string targetJosa = TargetJosa(targetText);
            string extraJosa = ExtraJosa(selection.action, extraText);
            string actionText = ActionText(selection.action);

            if (formatted)
            {
                targetText = RichFormat(targetText);
                actionText = RichFormat(actionText);
                extraText = RichFormat(extraText);
            }

            string text = targetText + targetJosa;
            text += extraText + extraJosa;
            text += actionText + "한다.";

            return text;
        }

        private static string RichFormat(string text)
            => $"<font=\"Galmuri11-Bold SDF\"><size=28.3f>{text}</size></font>";
    }
}
