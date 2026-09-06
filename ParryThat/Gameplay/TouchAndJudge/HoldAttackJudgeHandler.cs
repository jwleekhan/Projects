using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HoldAttackJudgeHandler : MonoBehaviour, IAttackJudgeHandler<AttackJudgeContext>
{
    [SerializeField] private JudgeSystem judgeSystem;
    private AttackType[] relevantAttacks = new AttackType[2] { AttackType.HoldStart, AttackType.HoldStop };
    private Dictionary<AttackType, AttackType[]> relevantTouches = new()
    {
        { AttackType.HoldStart, new AttackType[2] { AttackType.Normal, AttackType.Strong } },
        { AttackType.HoldStop, new AttackType[1] { AttackType.HoldStop } },
    };
    private bool isHolding = false;
    private bool isTouchBlocked = false;
    private List<AttackType> touchAllowOnlyList = new() { AttackType.HoldStop };

    public void OnNotice(AttackJudgeContext context)
    {
        NoteData note = context.note;

        if (note.type != (int)AttackType.HoldStart)
            return;

        NoteData nextNote = context.nextNote;
        if (nextNote == null || nextNote.type != (int)AttackType.HoldStop)
        {
            Debug.LogError("Next of HoldStart MUST be HoldStop");
            return;
        }

        Judgeable judgeable = new Judgeable((AttackType)note.type, note.arriveBeat, Direction.Up);
        judgeSystem.EnqueueJudgeable(judgeable);
        context.judgeables.Add(judgeable);

        // HoldStart 시, HoldStop까지 같이 생성
        Judgeable nextJudgeable = new Judgeable((AttackType)nextNote.type, nextNote.arriveBeat, Direction.Up);
        judgeSystem.EnqueueJudgeable(nextJudgeable);
        context.judgeables.Add(nextJudgeable);
    }

    void IAttackHandler.OnNotice(IAttackContext context)
        => OnNotice((AttackJudgeContext)context);

    public void OnAttackStart(AttackJudgeContext context)
    {

    }

    void IAttackHandler.OnAttackStart(IAttackContext context)
        => OnAttackStart((AttackJudgeContext)context);

    public void OnJudge(JudgeContext context)
    {
        Judgeable judgeable = context.judgeable;
        AttackType attackType = judgeable.attackType;

        // HoldStart에서 홀드하지 않으면 HoldStop의 EarlyMiss까지 유도
        if (attackType == AttackType.HoldStart && !isHolding)
        {
            judgeSystem.touchQueue.Enqueue(new Touched(Direction.None, StageFlowManager.Instance.currentTime, AttackType.HoldStop));
        }

        // HoldStop 공격 판정이 끝나면 isHolding 해제
        else if (attackType == AttackType.HoldStop)
        {
            isHolding = false;
            if (isTouchBlocked)
            {
                judgeSystem.AllowOnly(false, touchAllowOnlyList);
                isTouchBlocked = false;
            }
        }
    }

    public Judgeable GetFirstJudgeable(Dictionary<Direction, Judgeable> judgeables, Touched touch)
    {
        AttackType touchType = touch.type;

        Judgeable judgeable = null;
        float arriveBeat = Mathf.Infinity;

        // 모든 방향 중 가장 빠른 홀드 공격 탐색
        foreach (var tempJudgeable in judgeables.Values)
        {
            if (tempJudgeable != null && relevantAttacks.Contains(tempJudgeable.attackType) && tempJudgeable.arriveBeat < arriveBeat)
            {
                judgeable = tempJudgeable;
                arriveBeat = tempJudgeable.arriveBeat;
            }
        }

        // 관련 없는 터치이면 무시
        if (judgeable == null || !relevantTouches[judgeable.attackType].Contains(touchType))
            return null;

        return judgeable;
    }

    public JudgeType Judge(Judgeable judgeable, Touched touch)
    {
        double touchSec = touch.touchSec;
        AttackType touchType = touch.type;

        // 관련 없는 터치이면 무시
        if (!relevantTouches[judgeable.attackType].Contains(touchType))
            return JudgeType.None;

        float arriveSec = StageFlowManager.Instance.BeatToSec(judgeable.arriveBeat);
        JudgeType judgeType = judgeSystem.GetJudgeType(touchSec, arriveSec);

        // HoldStop이 아니면 EarlyMiss는 무시
        if (judgeable.attackType != AttackType.HoldStop && judgeType == JudgeType.EarlyMiss)
            judgeType = JudgeType.None;

        // 판정 보정, 플레이어가 자동으로 공격 방향을 바라봄
        if (judgeType >= JudgeType.LateBlocked && judgeType <= JudgeType.EarlyBlocked)
        {
            judgeType = JudgeType.Perfect;
            touch.direction = judgeable.noteDirection;

            // 홀드 시작
            if (judgeable.attackType == AttackType.HoldStart)
            {
                isHolding = true;
                judgeSystem.AllowOnly(true, touchAllowOnlyList);
                isTouchBlocked = true;
            }
        }

        return judgeType;
    }
}
