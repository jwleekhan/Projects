using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Note를 읽고 연주하는 역할.
/// 시간에 따라 예고 이펙트, 판정 생성, 공격 명령 등을 담당.
/// </summary>
public class NotePerformer : MonoBehaviour
{
    [SerializeField] private StrikerManager strikerManager;
    [SerializeField] private JudgeSystem judgeSystem;

    [Header("Attack Notice Handlers")]
    [SerializeField] private CommonAttackNoticeHandler commonAttackNoticeHandler;
    [SerializeField] private HoldAttackNoticeHandler holdAttackNoticeHandler;

    [Header("Attack Judge Handlers")]
    [SerializeField] private NormalAttackJudgeHandler normalAttackJudgeHandler;
    [SerializeField] private StrongAttackJudgeHandler strongAttackJudgeHandler;
    [SerializeField] private HoldAttackJudgeHandler holdAttackJudgeHandler;

    private Dictionary<AttackType, IAttackHandler> attackNoticeHandlerDict;
    private Dictionary<AttackType, IAttackHandler> attackJudgeHandlerDict;

    // 차트/노트 관련
    private int strikerType;
    private float strikerStartBeat;
    private float strikerEndBeat;
    private int strikerStatus;
    private NoteData[] notes;
    private int nextNoteIndex = 0;

    // Prepare Queue 관련
    private struct PrepareEntry
    {
        public NoteData note;
        public List<Judgeable> judgeables;

        public PrepareEntry(NoteData note, List<Judgeable> judgeables)
        {
            this.note = note;
            this.judgeables = judgeables;
        }
    }

    private Queue<PrepareEntry> prepareQueue = new();

    private void Awake()
    {
        attackNoticeHandlerDict = new()
        {
            { AttackType.Normal, commonAttackNoticeHandler },
            { AttackType.Strong, commonAttackNoticeHandler },
            { AttackType.HoldStart, holdAttackNoticeHandler },
            { AttackType.HoldStop, holdAttackNoticeHandler },
            { AttackType.Ghost, commonAttackNoticeHandler },
        };

        attackJudgeHandlerDict = new()
        {
            { AttackType.Normal, normalAttackJudgeHandler },
            { AttackType.Strong, strongAttackJudgeHandler },
            { AttackType.HoldStart, holdAttackJudgeHandler },
            { AttackType.HoldStop, holdAttackJudgeHandler },
            { AttackType.Ghost, strongAttackJudgeHandler },
        };

        judgeSystem.Judged -= OnJudge;
        judgeSystem.Judged += OnJudge;
    }

    private void OnDestroy()
    {
        judgeSystem.Judged -= OnJudge;
    }

    public void InitChart(ChartData chart)
    {
        strikerStatus = 0;
        nextNoteIndex = 0;
        prepareQueue.Clear();

        if (chart == null || chart.notes == null)
        {
            Debug.LogWarning("NotePerformer.InitChart: Chart is null");
            notes = System.Array.Empty<NoteData>();
            return;
        }

        StageFlowManager.Instance?.SetBPM(chart.bpm);

        strikerType = chart.strikerType;
        strikerStartBeat = chart.startBeat;
        strikerEndBeat = chart.endBeat;
        notes = (NoteData[])chart.notes.Clone();
    }

    private void Update()
    {
        if (StageFlowManager.Instance == null) return;
        if (notes == null || notes.Length == 0) return;

        float currentSec = StageFlowManager.Instance.currentTime;

        SetStrikerAppear(currentSec);
        PrepareNextNote(currentSec);
        HandleAttack(currentSec);
    }

    private void SetStrikerAppear(float currentSec)
    {
        float appearSec = StageFlowManager.Instance.BeatToSec(strikerStartBeat);
        float disappearSec = StageFlowManager.Instance.BeatToSec(strikerEndBeat);

        if (currentSec >= appearSec && currentSec < disappearSec && strikerStatus == 0)
        {
            strikerManager.AppearStriker(strikerType);
            strikerStatus = 1;
        }
        else if (currentSec >= disappearSec && strikerStatus != 0)
        {
            strikerManager.DisappearStriker();
            strikerStatus = 0;
        }
    }

    private void PrepareNextNote(float currentSec)
    {
        while (nextNoteIndex < notes.Length &&
               currentSec >= StageFlowManager.Instance.BeatToSec(notes[nextNoteIndex].noticeBeat))
        {
            NoteData note = notes[nextNoteIndex];
            AttackType attackType = (AttackType)note.type;

            List<Judgeable> judgeables = new();

            if (attackJudgeHandlerDict.TryGetValue(attackType, out var attackJudgeHandler)
                && attackJudgeHandler != null)
            {
                NoteData nextNote = nextNoteIndex + 1 < notes.Length ? notes[nextNoteIndex + 1] : null;
                attackJudgeHandler.OnNotice(new AttackJudgeContext(note, nextNote, judgeables));
            }

            if (attackNoticeHandlerDict.TryGetValue(attackType, out var attackNoticeHandler)
                && attackNoticeHandler != null)
            {
                attackNoticeHandler.OnNotice(new AttackNoticeContext(note, judgeables, strikerType));
            }

            StrikerController striker = strikerManager.GetStrikerInstance();
            if (striker != null)
            {
                striker.OnNotice(new StrikerAttackContext(note, judgeables));
            }

            nextNoteIndex++;
            prepareQueue.Enqueue(new PrepareEntry(note, judgeables));
        }
    }

    private void HandleAttack(float currentSec)
    {
        while (prepareQueue.Count > 0)
        {
            PrepareEntry prepareEntry = prepareQueue.Peek();

            StrikerController striker = strikerManager.GetStrikerInstance();
            if (striker == null)
                break;

            if (currentSec < StageFlowManager.Instance.BeatToSec(prepareEntry.note.arriveBeat) - striker.preAttackDelay)
                break;

            striker.OnAttackStart(new StrikerAttackContext(prepareEntry.note, prepareEntry.judgeables));
            prepareQueue.Dequeue();
        }
    }

    private void OnJudge(JudgeContext context)
    {
        Judgeable judgeable = context.judgeable;
        if (judgeable == null)
            return;

        StrikerController striker = strikerManager.GetStrikerInstance();
        if (striker != null)
        {
            striker.OnJudge(context);
        }

        if (attackJudgeHandlerDict.TryGetValue(judgeable.attackType, out var attackJudgeHandler)
            && attackJudgeHandler != null)
        {
            attackJudgeHandler.OnJudge(context);
        }
    }
}