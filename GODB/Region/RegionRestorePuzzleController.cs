using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Puzzle;
using Game.UI;
using Game.Save;
using Game.Flow;

namespace Game.Region
{
    public sealed class RegionRestorePuzzleController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RegionRestoreSceneController restoreController;
        [SerializeField] private PuzzleBoardPresenter boardPresenter;
        [SerializeField] private OperationSelectionPresenter selectionPresenter;
        [SerializeField] private RegionPuzzleDatabase puzzleDB;
        [SerializeField] private string hintPopupId = "HintPopup"; // 힌트 팝업 ID
        private SaveManager _save;


        [Header("Success/Fail")]
        [SerializeField] private Animator successAnim;
        [SerializeField] private Animator failAnim;

        [Header("Debug")]
        [Tooltip("디버그용: 퍼즐을 즉시 클리어하는 버튼 (개발 중에만 사용)")]
        [SerializeField] private Button debugClearPuzzleButton;

        // 현재 지역 및 퍼즐 정보
        private string _regionId;
        private PuzzleDefinition[] _regionPuzzles;
        private int _curPuzzleIndex = 0; // 현재 퍼즐 인덱스 (1-1 지역의 경우 0, 1, 2)
        private PuzzleDefinition _puzzle;

        private void Awake()
        {
            // --- 테스트용 강제 할당 제거됨 ---

            if (GameRoot.Instance == null || GameRoot.Instance.Save == null)
            {
                Debug.LogError($"{name}: GameRoot/Save is null");
            }
            _save = GameRoot.Instance.Save;

            if (restoreController == null || boardPresenter == null || selectionPresenter == null || puzzleDB == null)
            {
                Debug.LogError($"{name}: some references are null");
            }

            // 디버그 버튼 이벤트 연결
            if (debugClearPuzzleButton != null)
            {
                debugClearPuzzleButton.onClick.RemoveAllListeners();
                debugClearPuzzleButton.onClick.AddListener(OnDebugClearPuzzle);

                // 디버그 버튼은 항상 비활성화 (필요시 Inspector에서 활성화 가능)
                debugClearPuzzleButton.gameObject.SetActive(false);
            }

            successAnim.gameObject.SetActive(false);
            failAnim.gameObject.SetActive(false);
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
                boardPresenter.StepChanged += selectionPresenter.OnChangeStep;
                boardPresenter.ApplyFailed += selectionPresenter.OnFailApply;
                boardPresenter.PuzzleFailed += PuzzleFail;
                boardPresenter.PuzzleSuccessed += OnSuccessPuzzle;
                boardPresenter.CellClicked += selectionPresenter.OnClickCell;
                selectionPresenter.SelectionChanged += boardPresenter.OnChangeSelection;
                selectionPresenter.UndoClicked += boardPresenter.OnClickUndo;
                selectionPresenter.RedoClicked += boardPresenter.OnClickRedo;
                selectionPresenter.ApplyClicked += OnClickApply;
            }
            else
            {
                boardPresenter.StepChanged -= selectionPresenter.OnChangeStep;
                boardPresenter.ApplyFailed -= selectionPresenter.OnFailApply;
                boardPresenter.PuzzleFailed -= PuzzleFail;
                boardPresenter.PuzzleSuccessed -= OnSuccessPuzzle;
                boardPresenter.CellClicked -= selectionPresenter.OnClickCell;
                selectionPresenter.SelectionChanged -= boardPresenter.OnChangeSelection;
                selectionPresenter.UndoClicked -= boardPresenter.OnClickUndo;
                selectionPresenter.RedoClicked -= boardPresenter.OnClickRedo;
                selectionPresenter.ApplyClicked -= OnClickApply;
            }
        }

        private void Start()
        {
            GetPuzzlesForSelectedRegion();
            LoadNewPuzzle();
        }

        private void GetPuzzlesForSelectedRegion()
        {
            // 지역에 해당하는 퍼즐 배열 불러오기
            var session = GameRoot.Instance.Session;
            if (session != null)
            {
                _regionId = session.RegionId;
            }
            else
            {
                Debug.LogError($"{name}: Session is null. Setting regionId 1-1");
                _regionId = "1-1";
            }

            if (!puzzleDB.TryGetPuzzles(_regionId, out _regionPuzzles))
            {
                Debug.LogError($"[RegionRestorePuzzle] 지원하지 않는 지역: {_regionId}");
                return;
            }
        }

        private void LoadNewPuzzle()
        {
            if (_regionPuzzles == null || _regionPuzzles.Length <= 0)
                GetPuzzlesForSelectedRegion();

            // 퍼즐 진행도 불러오기 - 지역별 상태에서 가져오기
            var state = _save.Load();
            if (state == null)
            {
                Debug.LogError("[RegionRestorePuzzle] Failed to load save state");
                return;
            }

            var puzzleState = state.GetPuzzleState(_regionId);
            _curPuzzleIndex = puzzleState.progress;

            if (_curPuzzleIndex < 0 || _curPuzzleIndex >= _regionPuzzles.Length)
            {
                Debug.LogWarning($"[RegionRestorePuzzle] 퍼즐 인덱스 범위 초과: {_curPuzzleIndex} (최대: {_regionPuzzles.Length - 1}). 인덱스를 0으로 초기화합니다.");
                _curPuzzleIndex = 0;

                // struct이므로 리스트에서 직접 수정해야 함
                for (int i = 0; i < state.regionPuzzleStates.Count; i++)
                {
                    if (state.regionPuzzleStates[i].regionId == _regionId)
                    {
                        var temp = state.regionPuzzleStates[i];
                        temp.progress = 0;
                        state.regionPuzzleStates[i] = temp;
                        break;
                    }
                }
                _save.Save(state);
            }

            // 퍼즐 할당
            _puzzle = _regionPuzzles[_curPuzzleIndex];

            if (_puzzle == null || !_puzzle.IsValid())
            {
                string puzzleName = (_puzzle != null) ? _puzzle.name : "null";
                Debug.LogError($"[RegionRestorePuzzle] Puzzle is empty or invalid! Region: {_regionId}, Index: {_curPuzzleIndex}, AssetName: {puzzleName}");
                return;
            }

            // Board 초기화
            boardPresenter.Init(_puzzle, puzzleDB);

            // 변형 조작 선택 초기화
            selectionPresenter.Init(_puzzle);

            // 이벤트 구독 연결
            SetEventListeners(true);

            // 현재 퍼즐 번호 표시
            restoreController.UpdateRegionText();

            // 힌트 팝업 갱신 (RegionRestoreSceneController를 통해 전달되는 방식 검토 필요)
            // 여기서는 일단 기존 로직 유지 또는 UI 직접 갱신
            var hintUI = FindFirstObjectByType<HintPopupUI>();
            if (hintUI != null) hintUI.Refresh();

            // 스토리 및 가이드 순차 진행 시작
            StartCoroutine(PlayIntroSequenceAndStartTimer());
        }

        private IEnumerator PlayIntroSequenceAndStartTimer()
        {
            var flow = GameRoot.Instance.Flow;

            // 1. 스토리 재생
            if (!string.IsNullOrEmpty(_puzzle.introStoryId) && flow != null)
            {
                bool isStoryFinished = false;
                System.Action onFlowEnded = () => { isStoryFinished = true; };
                flow.OnFlowEnded += onFlowEnded;
                
                flow.JumpToNode(_puzzle.introStoryId);
                
                while (!isStoryFinished)
                {
                    yield return null;
                }
                
                flow.OnFlowEnded -= onFlowEnded;
            }

            // 2. 가이드 재생
            if (!string.IsNullOrEmpty(_puzzle.introGuideId))
            {
                var state = _save.Load();
                if (state != null)
                {
                    state.RecordGuide(_puzzle.introGuideId);
                    _save.Save(state);
                }

                var guideView = Game.Dialogue.GuideView.Instance;
                if (guideView != null)
                {
                    bool isGuideFinished = false;
                    guideView.Show(_puzzle.introGuideId, () => { isGuideFinished = true; });
                    
                    while (!isGuideFinished)
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogError("[RegionRestorePuzzle] GuideView.Instance를 찾을 수 없습니다.");
                }
            }

            // [Telemetry] 퍼즐 시작 시간 기록
            if (GameRoot.Instance != null && GameRoot.Instance.Telemetry != null)
            {
                GameRoot.Instance.Telemetry.StartPuzzleTimer();
                GameRoot.Instance.Telemetry.LogEvent("PuzzleStart", _regionId, _curPuzzleIndex);
            }
        }

        /// <summary>
        /// 퍼즐 가이드 버튼 클릭 시 호출됩니다.
        /// </summary>
        public void OnClickShowGuide()
        {
            var guideView = Game.Dialogue.GuideView.Instance;
            if (guideView != null)
            {
                if (guideView.Database != null && guideView.Database.guides != null && guideView.Database.guides.Count > 0)
                {
                    var state = _save.Load();
                    var unlocked = state != null ? state.unlockedGuides : new List<string>();

                    List<string> guidesToShow = new List<string>();
                    foreach (var guide in guideView.Database.guides)
                    {
                        if (guide != null && !string.IsNullOrWhiteSpace(guide.Id) && unlocked.Contains(guide.Id))
                        {
                            guidesToShow.Add(guide.Id);
                        }
                    }

                    if (guidesToShow.Count > 0)
                    {
                        Debug.Log($"[RegionRestorePuzzle] 해금된 가이드 {guidesToShow.Count}개를 통합하여 보여줍니다.");
                        guideView.Show(guidesToShow, null);
                    }
                    else
                    {
                        Debug.LogWarning("[RegionRestorePuzzle] 해금된 가이드가 없습니다.");
                    }
                }
                else
                {
                    Debug.LogWarning("[RegionRestorePuzzle] 재생할 가이드가 없거나 데이터베이스가 비어 있습니다.");
                }
            }
            else
            {
                Debug.LogError("[RegionRestorePuzzle] GuideView.Instance를 찾을 수 없습니다.");
            }
        }

        private void OnClickApply(OperationSelection selection)
        {
            boardPresenter.OnClickApply(_puzzle, selection);
        }

        private void PuzzleFail(int curStep)
        {
            // [Telemetry] 퍼즐 실패 기록 (시간은 기록하지 않음)
            if (GameRoot.Instance != null && GameRoot.Instance.Telemetry != null)
            {
                GameRoot.Instance.Telemetry.LogPuzzleFail(_regionId, _curPuzzleIndex, $"Step: {curStep}");
            }

            StartCoroutine(PlayFailAnim());
        }

        private IEnumerator PlayFailAnim()
        {
            failAnim.gameObject.SetActive(true);
            failAnim.SetTrigger("Play");

            yield return WaitForAnimation(failAnim);

            failAnim.gameObject.SetActive(false);
        }

        private IEnumerator WaitForAnimation(Animator animator)
        {
            // 현재 상태 정보 가져오기
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

            // 애니메이션이 끝날 때까지 대기
            while (info.normalizedTime < 1f)
            {
                yield return null;
                info = animator.GetCurrentAnimatorStateInfo(0);
            }
        }

        private void OnSuccessPuzzle()
        {
            PuzzleSuccess();
        }

        /// <summary>
        /// 정답 확인됨 - 퍼즐 진행도 저장 및 다음 퍼즐로 이동
        /// </summary>
        private void PuzzleSuccess(bool debugMode = false)
        {
            var state = _save.Load();
            if (state == null)
            {
                state = new GameState();
            }

            // 튜토리얼 모드여도 일반적인 저장 로직을 따름
            // (사용자가 튜토리얼 퍼즐도 기록되길 원함)

            // 현재 지역의 퍼즐 상태 가져오기
            var puzzleState = state.GetPuzzleState(_regionId);

            // 현재 퍼즐의 해결 정보 저장
            SaveSolution(puzzleState, debugMode);

            // [Telemetry] 퍼즐 성공 및 소요 시간 기록
            if (GameRoot.Instance != null && GameRoot.Instance.Telemetry != null)
            {
                GameRoot.Instance.Telemetry.LogPuzzleSuccess(_regionId, _curPuzzleIndex);
            }

            // 현재 퍼즐 완료 처리 (hintProgress 리셋 → FlowManager _state 동기화)
            _curPuzzleIndex++;

            // 지역별 퍼즐 상태 업데이트
            state.SetPuzzleState(_regionId, _curPuzzleIndex, puzzleState.solutions);

            // 저장
            _save.Save(state);
            Debug.Log($"[RegionRestorePuzzle] 지역 {_regionId} 퍼즐 {_curPuzzleIndex - 1} 완료. Save called.");

            // FlowManager의 상태 동기화
            if (GameRoot.Instance.Flow != null)
            {
                GameRoot.Instance.Flow.ReloadState();
                GameRoot.Instance.Flow.SetHintProgress(0);
            }

            // 모든 퍼즐을 완료했는지 확인, 콜백 등록
            bool isComplete = _curPuzzleIndex >= _regionPuzzles.Length;
            System.Action callback = isComplete ? CompleteAllPuzzles : LoadNewPuzzle;

            // 성공 연출 후 콜백 실행
            StartCoroutine(PlaySuccessAnim(callback));
        }

        private void SaveSolution(RegionPuzzleState puzzleState, bool debugMode)
        {
            // 적용된 변형 텍스트 수집
            List<string> appliedTexts = boardPresenter.GetAppliedTexts();

            // 솔루션 리스트 초기화 (필요시)
            if (puzzleState.solutions == null)
                puzzleState.solutions = new List<StringListWrapper>();

            // 현재 퍼즐 인덱스까지 리스트 확장
            while (puzzleState.solutions.Count <= _curPuzzleIndex)
                puzzleState.solutions.Add(new StringListWrapper());

            // 디버그 모드면 임의 텍스트 사용
            if (debugMode)
                appliedTexts = new List<string> { "(디버그 클리어)", "임의의 조작으로 해결됨" };

            // 현재 퍼즐의 해결 정보 저장
            puzzleState.solutions[_curPuzzleIndex] = new StringListWrapper(new List<string>(appliedTexts));
        }

        private IEnumerator PlaySuccessAnim(System.Action callback)
        {
            successAnim.gameObject.SetActive(true);
            successAnim.SetTrigger("Play");

            yield return WaitForAnimation(successAnim);

            successAnim.gameObject.SetActive(false);
            callback?.Invoke();
        }

        /// <summary>
        /// 모든 퍼즐 완료 처리
        /// </summary>
        private void CompleteAllPuzzles()
        {
            if (string.IsNullOrWhiteSpace(_regionId))
            {
                Debug.LogError("[RegionRestorePuzzle] SelectedRegionId가 없습니다.");
                SceneLoader.Load(Game.Flow.SceneNames.Report);
                return;
            }

            // 튜토리얼 완료 처리
            if (_regionId == "Tutorial")
            {
                Debug.Log("[RegionRestorePuzzle] 모든 튜토리얼 퍼즐 완료. 보고서 씬으로 이동합니다.");
                SceneLoader.Load(Game.Flow.SceneNames.Report);
                return;
            }

            // 지역 복원 상태 저장
            var state = _save.Load();
            if (state == null)
            {
                state = new GameState();
            }

            // [추가] 원래 상태 체크 (이미 복원된 경우 애니메이션 생략용)
            RegionStatus oldStatus = state.GetRegionStatus(_regionId);

            // [수정] 여기서 즉시 저장하지 않고, 애니메이션 연출이 끝난 후에 저장하도록 변경하여 깜빡임 방지
            // state.SetRegionStatus(_regionId, RegionStatus.Restored); 
            // _save.Save(state);

            // [Fix] FlowManager 상태 동기화 (힌트 등 리셋을 위해 미리 수행할 수도 있음)
            if (GameRoot.Instance.Flow != null)
            {
                GameRoot.Instance.Flow.ReloadState();
            }

            Debug.Log($"[RegionRestorePuzzle] 지역 {_regionId} 퍼즐 완료. (이전 상태: {oldStatus})");

            // RegionManagementScene으로 전환하여 컬러 애니메이션 재생
            // 씬 전환 시 코루틴이 중단되지 않도록 GameRoot에서 실행
            var gameRoot = GameRoot.Instance;
            if (gameRoot != null)
            {
                // [수정] 이미 복원된 상태라면 애니메이션을 생략하고 바로 보고서로 이동
                if (oldStatus >= RegionStatus.Restored)
                {
                    Debug.Log($"[RegionRestorePuzzle] {_regionId}는 이미 복원되었습니다. 애니메이션을 생략합니다.");
                    SceneLoader.Load(Game.Flow.SceneNames.Report);
                }
                else
                {
                    // GameRoot의 코루틴으로 실행 (씬 전환 후에도 계속 실행됨)
                    gameRoot.StartCoroutine(ShowRestoreAnimationAndGoToReportCoroutine(_regionId));
                }
            }
            else
            {
                Debug.LogError("[RegionRestorePuzzle] GameRoot를 찾을 수 없습니다. 바로 ReportScene으로 이동합니다.");
                SceneLoader.Load(Game.Flow.SceneNames.Report);
            }
        }

        /// <summary>
        /// 실제 애니메이션 및 씬 전환 로직 (GameRoot에서 실행)
        /// </summary>
        private IEnumerator ShowRestoreAnimationAndGoToReportCoroutine(string regionId)
        {
            Debug.Log($"[RegionRestorePuzzle] ShowRestoreAnimationAndGoToReportCoroutine 시작: {regionId}");

            // RegionManagementScene으로 전환
            bool sceneLoaded = false;
            UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> onSceneLoaded = null;

            onSceneLoaded = (scene, mode) =>
            {
                if (scene.name == Game.Flow.SceneNames.RegionManagement)
                {
                    sceneLoaded = true;
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onSceneLoaded;
                }
            };

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneLoaded;
            SceneLoader.Load(Game.Flow.SceneNames.RegionManagement);

            // 씬이 로드될 때까지 대기
            while (!sceneLoaded)
            {
                yield return null;
            }

            Debug.Log("[RegionRestorePuzzle] RegionManagementScene 로드 완료");

            // 씬 초기화를 위한 추가 대기
            yield return null;
            yield return null;
            yield return null;

            // RegionManagementUI 찾기
            var regionManagementUI = FindFirstObjectByType<Game.UI.RegionManagementUI>();
            int maxWait = 30;
            int waited = 0;

            while (regionManagementUI == null && waited < maxWait)
            {
                yield return null;
                regionManagementUI = FindFirstObjectByType<Game.UI.RegionManagementUI>();
                waited++;
                if (waited % 5 == 0)
                {
                    Debug.Log($"[RegionRestorePuzzle] RegionManagementUI 찾는 중... ({waited}/{maxWait})");
                }
            }

            if (regionManagementUI == null)
            {
                Debug.LogWarning("[RegionRestorePuzzle] RegionManagementUI를 찾을 수 없습니다. 바로 ReportScene으로 이동합니다.");
                SceneLoader.Load(Game.Flow.SceneNames.Report);
                yield break;
            }

            Debug.Log("[RegionRestorePuzzle] RegionManagementUI 찾음. 상태 새로고침 및 애니메이션 시작");

            // 상태 새로고침 (Restored 상태 반영)
            regionManagementUI.RefreshState();

            // 잠깐 대기 (씬이 완전히 로드되도록)
            yield return null;
            yield return null;

            // 복원 애니메이션 재생 (흑백 → 컬러)
            Debug.Log($"[RegionRestorePuzzle] 복원 애니메이션 시작: {regionId}");
            yield return regionManagementUI.PlayRestoreAnimation(regionId, 1.5f);
            Debug.Log("[RegionRestorePuzzle] 복원 애니메이션 완료. 상태를 Restored로 저장합니다.");

            // [추가] 애니메이션이 완전히 끝난 후 상태 저장
            var save = GameRoot.Instance.Save;
            var finalState = save.Load();
            finalState.SetRegionStatus(regionId, RegionStatus.Restored);
            save.Save(finalState);

            // UI 갱신 (컬러 상태 반영 확인)
            regionManagementUI.RefreshState();

            // 애니메이션 후 잠깐 더 대기
            yield return new WaitForSeconds(0.5f);

            // ReportScene으로 이동
            Debug.Log("[RegionRestorePuzzle] ReportScene으로 이동");
            SceneLoader.Load(Game.Flow.SceneNames.Report);
        }

        /// <summary>
        /// 디버그용: 현재 퍼즐을 즉시 클리어 (정답 체크 우회)
        /// DebugManager 등 외부에서 호출 가능하도록 public으로 변경
        /// </summary>
        public void OnDebugClearPuzzle()
        {
            if (_puzzle == null)
            {
                Debug.LogWarning("[RegionRestorePuzzle] 디버그 클리어 실패: 퍼즐이 초기화되지 않았습니다.");
                return;
            }

            Debug.Log("[RegionRestorePuzzle] 디버그 모드: 퍼즐을 즉시 클리어합니다.");

            PuzzleSuccess(debugMode: true);
        }
    }
}
