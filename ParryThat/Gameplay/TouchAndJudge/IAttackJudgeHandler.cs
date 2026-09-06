using System.Collections.Generic;

public interface IAttackJudgeHandler : IAttackHandler
{
    Judgeable GetFirstJudgeable(Dictionary<Direction, Judgeable> judgeables, Touched touch);
    JudgeType Judge(Judgeable judgeable, Touched touch);
}

public interface IAttackJudgeHandler<T> : IAttackJudgeHandler where T : IAttackContext
{
    new public Judgeable GetFirstJudgeable(Dictionary<Direction, Judgeable> judgeables, Touched touch);
    new public JudgeType Judge(Judgeable judgeable, Touched touch);
}
