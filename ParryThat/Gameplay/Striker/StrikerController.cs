using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrikerAttackContext : IAttackContext
{
    public NoteData note { get; }
    public List<Judgeable> judgeables;

    public StrikerAttackContext(NoteData note, List<Judgeable> judgeables)
    {
        this.note = note;
        this.judgeables = judgeables;
    }
}

/// <summary>
/// 각 Striker의 세부 컴포넌트들을 연결하는 핵심 로직.
/// </summary>
public class StrikerController : MonoBehaviour, IAttackHandler<StrikerAttackContext>
{
    [Header("Striker Components")]
    [SerializeField] private StrikerCommonVisual commonVisual;
    [SerializeField] private StrikerHoldVisual holdVisual;
    [SerializeField] private StrikerSound sound;

    [Header("Projectiles")]
    private Transform projectileParent;
    [SerializeField] private List<Projectile> projectilePrefabs;

    public StrikerCommonVisual Visual => commonVisual;
    public StrikerSound Sound => sound;

    [SerializeField] private float _preAttackDelay = 0.5f;
    public float preAttackDelay => _preAttackDelay; // 공격 명령으로부터 판정까지 걸리는 시간

    private Vector3[] spawnPositions; // 0: 리더, 1~4: 방향별 투사체
    private Vector3[] targetPositions; // 0: 플레이어, 1~4: 방향별 보정된 판정 위치

    public void Init(Vector3[] spawnPositions, Vector3[] targetPositions, Transform projectileParent, DynamicUIManager dynamicUIManager)
    {
        this.spawnPositions = spawnPositions;
        this.targetPositions = targetPositions;
        this.projectileParent = projectileParent;

        commonVisual.Init(spawnPositions[0]);
        holdVisual?.Init(spawnPositions[0], targetPositions[(int)Direction.Up], dynamicUIManager);
    }

    public void OnNotice(StrikerAttackContext context)
    {
        if (IsHoldAttack(context))
        {
            holdVisual?.OnNotice(context);
        }
        else
        {
            commonVisual.OnNotice(context);
            sound.PlayPrepareSound((AttackType)context.note.type);
        }
    }

    void IAttackHandler.OnNotice(IAttackContext context)
        => OnNotice((StrikerAttackContext)context);

    public void OnAttackStart(StrikerAttackContext context)
    {
        if (IsHoldAttack(context))
        {
            holdVisual?.OnAttackStart(context);
        }
        else
        {
            commonVisual.OnAttackStart(context);
            Projectile projectile = FireProjectile(context.note.direction, StageFlowManager.Instance.BeatToSec(context.note.arriveBeat), context.note.type);
            if (context.judgeables.Count > 0)
                context.judgeables[0].AddOnDestroy(projectile.OnJudge);
        }
    }

    void IAttackHandler.OnAttackStart(IAttackContext context)
        => OnAttackStart((StrikerAttackContext)context);

    public void OnJudge(JudgeContext context)
    {
        if (IsHoldAttack(context))
        {
            holdVisual?.OnJudge(context);
            if (context.isMiss)
            {
                sound.Stop();
            }
        }
        else
        {
            commonVisual.OnJudge(context);
        }

        if (context.isParried)
        {
            OnHit(context);
        }
    }

    private void OnHit(JudgeContext context)
    {
        if (IsHoldAttack(context))
        {
            holdVisual?.OnHit(context);
            sound.PlayHoldSound(context.judgeable.attackType);
        }
        else
        {
            commonVisual.OnHit(context);
            sound.PlayParrySound(context.judgeable.attackType);
        }
    }

    public void OnClear()
    {
        commonVisual.OnClear();
    }

    private Projectile FireProjectile(int direction, float arriveSec, int attackType)
    {
        if (attackType < 0 || attackType >= projectilePrefabs.Count)
        {
            Debug.LogWarning($"{name}.FireProjectile: projectile prefab not exists for attackType {attackType}");
            return null;
        }
        Projectile selectedProjectile = projectilePrefabs[attackType];

        Vector3 startPos = spawnPositions[direction];
        Vector3 targetPos = targetPositions[direction];

        // 투사체 생성
        Projectile projectile = Instantiate(selectedProjectile, startPos, Quaternion.identity, projectileParent);
        projectile.Setup(new ProjectileSetupContext((AttackType)attackType, arriveSec, (Direction)direction, startPos, targetPos));

        return projectile;
    }

    private bool IsHoldAttack(StrikerAttackContext context)
    {
        AttackType attackType = (AttackType)context.note.type;
        return attackType == AttackType.HoldStart || attackType == AttackType.HoldStop;
    }

    private bool IsHoldAttack(JudgeContext context)
    {
        AttackType attackType = context.judgeable.attackType;
        return attackType == AttackType.HoldStart || attackType == AttackType.HoldStop;
    }
}
