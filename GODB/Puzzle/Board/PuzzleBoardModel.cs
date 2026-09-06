using System;
using UnityEngine;

namespace Game.Puzzle
{
    public readonly struct StepContext
    {
        public readonly int prevStep;
        public readonly int curStep;
        public readonly int maxSteps;
        public readonly bool canUndo;
        public readonly bool canRedo;
        public readonly bool canSubmit;

        public StepContext(int _prevStep, int _curStep, int _maxSteps, bool _canUndo, bool _canRedo, bool _canSubmit)
        {
            prevStep = _prevStep;
            curStep = _curStep;
            maxSteps = _maxSteps;
            canUndo = _canUndo;
            canRedo = _canRedo;
            canSubmit = _canSubmit;
        }
    }

    public sealed class PuzzleBoardModel
    {
        public event Action<int, PlaceableObject[][], OperationSelection> ApplySuccessed;
        public event Action ApplyFailed;
        public event Action<StepContext> StepChanged;

        private PlaceableObject[][][] _boardData;
        public PlaceableObject[][][] BoardData => GetBoardClone();

        private OperationSelection[] _appliedSelections;
        public OperationSelection[] AppliedSelections => (OperationSelection[])_appliedSelections.Clone();

        private int _curStep = 0;
        public int CurStep
        {
            get => _curStep;
            set => ChangeStep(value);
        }
        private int _validSteps = 0; // Redo 가능한 최대 step
        private int _maxSteps = 0;

        private bool canUndo => _curStep > 0;
        private bool canRedo => _curStep < _validSteps;
        private bool canApply => _curStep < _maxSteps;
        private bool canSubmit => _curStep == _maxSteps;

        public PuzzleBoardModel(PuzzleDefinition puzzle)
        {
            _boardData = new PlaceableObject[puzzle.maxSteps + 2][][];
            _boardData[0] = new PlaceableObject[][] { puzzle.a0, puzzle.b0, puzzle.c0 };
            _boardData[^1] = new PlaceableObject[][] { puzzle.aPrime, puzzle.bPrime, puzzle.cPrime };
            _appliedSelections = new OperationSelection[puzzle.maxSteps];
            _curStep = 0;
            _validSteps = 0;
            _maxSteps = puzzle.maxSteps;
        }

        public void ChangeStep(int newStep)
        {
            if (_curStep == newStep)
                return;

            int prevStep = _curStep;
            _curStep = newStep;

            StepChanged?.Invoke(new StepContext(prevStep, _curStep, _maxSteps, canUndo, canRedo, canSubmit));
        }

        public void Undo()
        {
            if (!canUndo)
            {
                Debug.LogWarning("!canUndo but tried");
                return;
            }

            CurStep--;
        }

        public void Redo()
        {
            if (!canRedo)
            {
                Debug.LogWarning("!canRedo but tried");
                return;
            }

            CurStep++;
        }

        public void ApplyOperation(PuzzleDefinition puzzle, OperationSelection selection)
        {
            if (!canApply)
            {
                Debug.LogWarning("!canApply but tried");
                return;
            }

            // 기존 행에 변형 적용 검증
            PlaceableObject[][] oldStepData = BoardData[_curStep]; // Clone
            PlaceableObject[][] newStepData = new PlaceableObject[oldStepData.Length][];

            for (int i = 0; i < oldStepData.Length; i++)
            {
                PlaceableObject[] gridData = oldStepData[i];

                OperationError error = PuzzleOperation.ApplyOperation(puzzle, gridData, selection);
                if (error != 0)
                {
                    ApplyFailed?.Invoke();
                    return;
                }

                error = PuzzleOperation.PostOperation(gridData);
                if (error != 0)
                {
                    ApplyFailed?.Invoke();
                    return;
                }

                newStepData[i] = gridData;
            }

            // 다음 행 생성
            _appliedSelections[CurStep] = selection;
            _boardData[CurStep + 1] = newStepData;
            ApplySuccessed?.Invoke(CurStep, newStepData, selection);
            _validSteps = CurStep + 1;
            CurStep++;
        }

        private PlaceableObject[][][] GetBoardClone()
        {
            PlaceableObject[][][] clone = new PlaceableObject[_boardData.Length][][];
            for (int i = 0; i < _boardData.Length; i++)
            {
                if (_boardData[i] == null) continue;
                clone[i] = new PlaceableObject[_boardData[i].Length][];
                for (int j = 0; j < _boardData[i].Length; j++)
                {
                    if (_boardData[i][j] == null) continue;
                    clone[i][j] = (PlaceableObject[])_boardData[i][j].Clone();
                }
            }
            return clone;
        }
    }
}
