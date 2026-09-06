public interface IAttackHandler
{
    void OnNotice(IAttackContext context);
    void OnAttackStart(IAttackContext context);
    void OnJudge(JudgeContext context);
}

public interface IAttackHandler<T> : IAttackHandler where T : IAttackContext
{
    public void OnNotice(T context);
    public void OnAttackStart(T context);
    new public void OnJudge(JudgeContext context);
}
