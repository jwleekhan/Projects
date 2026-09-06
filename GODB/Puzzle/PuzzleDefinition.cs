using System.Collections.Generic;
using UnityEngine;

namespace Game.Puzzle
{
    [CreateAssetMenu(menuName = "Puzzle/Puzzle Definition", fileName = "PuzzleDefinition")]
    public sealed class PuzzleDefinition : ScriptableObject
    {
        [Header("Grid Size")]
        public int width = 3;
        public int height = 3;

        [Header("Max Steps")]
        public int maxSteps = 1;

        [Header("Story / Guide")]
        [Tooltip("퍼즐 시작 전 실행할 스토리 노드 ID (DialogueNode 등)")]
        public string introStoryId;

        [Tooltip("스토리 재생 후 연이어 보여줄 가이드 노드 ID (또는 스토리가 없으면 바로 표시)")]
        public string introGuideId;

        [Header("Symbols (index = symbolId)")]
        public string[] symbolNames;

        [Header("Able Operations (null이면 전체 허용)")]
        public List<TargetType> ableTargets;
        public List<ActionType> ableActions;

        [Header("Able Specials (null이면 일반만 허용)")]
        public List<SpecialType> ableSpecials;

        [Header("Given Grids")]
        public PlaceableObject[] a0;
        public PlaceableObject[] b0;
        public PlaceableObject[] c0;
        public PlaceableObject[] aPrime;
        public PlaceableObject[] bPrime;
        public PlaceableObject[] cPrime;

        public int CellCount => width * height;

        public bool IsValid()
        {
            if (width <= 0 || height <= 0) return false;
            if (maxSteps <= 0) return false;
            if (symbolNames == null || symbolNames.Length == 0) return false;

            int n = CellCount;
            return IsGridSized(a0, n)
                && IsGridSized(b0, n)
                && IsGridSized(c0, n)
                && IsGridSized(aPrime, n)
                && IsGridSized(bPrime, n)
                && IsGridSized(cPrime, n);
        }

        private static bool IsGridSized(PlaceableObject[] arr, int n) => arr != null && arr.Length == n;
    }
}
