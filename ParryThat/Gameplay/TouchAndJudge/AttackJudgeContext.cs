using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackJudgeContext : IAttackContext
{
    public NoteData note { get; }
    public NoteData nextNote { get; }
    public List<Judgeable> judgeables = new();

    public AttackJudgeContext(NoteData note, NoteData nextNote, List<Judgeable> judgeables)
    {
        this.note = note;
        this.nextNote = nextNote;
        this.judgeables = judgeables;
    }
}
