using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrongAttackJudgeHandler : MonoBehaviour, IAttackJudgeHandler<AttackJudgeContext>
{
    [SerializeField] private JudgeSystem judgeSystem;
    private AttackType[] relevantAttacks = new AttackType[2] { AttackType.Strong, AttackType.Ghost };
    private AttackType[] relevantTouches = new AttackType[1] { AttackType.Strong };

    public void OnNotice(AttackJudgeContext context)
    {
        NoteData note = context.note;
        Judgeable judgeable = new Judgeable((AttackType)note.type, note.arriveBeat, (Direction)note.direction);
        judgeSystem.EnqueueJudgeable(judgeable);
        context.judgeables.Add(judgeable);
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

    }

    public Judgeable GetFirstJudgeable(Dictionary<Direction, Judgeable> judgeables, Touched touch)
    {
        Direction touchDirection = touch.direction;
        AttackType touchType = touch.type;

        // 관련 없는 터치이면 무시
        if (!relevantTouches.Contains(touchType))
            return null;

        // 방향이 일치하는 Judgeable만 확인
        Judgeable judgeable = judgeables[touchDirection];

        if (judgeable == null || !relevantAttacks.Contains(judgeable.attackType))
            return null;

        return judgeable;
    }

    public JudgeType Judge(Judgeable judgeable, Touched touch)
    {
        double touchSec = touch.touchSec;
        AttackType touchType = touch.type;

        // 관련 없는 터치이면 무시
        if (!relevantTouches.Contains(touchType))
            return JudgeType.None;

        float arriveSec = StageFlowManager.Instance.BeatToSec(judgeable.arriveBeat);
        JudgeType judgeType = judgeSystem.GetJudgeType(touchSec, arriveSec);

        // EarlyMiss는 무시
        if (judgeType == JudgeType.EarlyMiss)
            judgeType = JudgeType.None;

        return judgeType;
    }
}
