using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Striker의 Hold 공격 관련 애니메이션을 담당.
/// 애니메이터의 Hold Layer와 연계.
/// </summary>
public class StrikerHoldVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animator holdSpriteAnimator;
    [SerializeField] private Animator bladeAnimator;

    private DynamicUIManager dynamicUIManager;

    // For Move Attack
    [SerializeField] private bool isMoveAttack = false; // Striker가 직접 이동하는지
    private float bladeDistanceOffset => 0.7f;
    private Vector3 spawnPosition;
    private Vector3 targetPosition;

    private int holdLayerIndex;
    private float backToBaseTime => 0.25f; // 공격 끝난 후 Base Layer로 복귀 시간
    
    private void Awake()
    {
        holdLayerIndex = animator.GetLayerIndex("Hold Layer");
    }

    public void Init(Vector3 spawnPosition, Vector3 targetPosition, DynamicUIManager dynamicUIManager)
    {
        this.spawnPosition = spawnPosition;
        Vector3 dirVec = (targetPosition - spawnPosition).normalized;
        this.targetPosition = targetPosition - dirVec * bladeDistanceOffset;
        this.dynamicUIManager = dynamicUIManager;
    }

    public void OnNotice(StrikerAttackContext context)
    {
        if (context.note.type == (int)AttackType.HoldStart)
        {
            StartCoroutine(WaitAndHoldStart(StageFlowManager.Instance.BeatToSec(context.note.arriveBeat)));

            if (context.judgeables.Count >= 2)
            {
                Judgeable judgeable = context.judgeables[0];
                Judgeable nextJudgeable = context.judgeables[1];
                if (judgeable == null || nextJudgeable == null)
                    return;

                float nextArriveSec = StageFlowManager.Instance.BeatToSec(nextJudgeable.arriveBeat);
                judgeable.AddOnDestroy(context => ShowCutIn(context, nextArriveSec));
                nextJudgeable.AddOnDestroy(context => ShowCutIn(context, nextArriveSec));
            }
        }
    }

    public void OnAttackStart(StrikerAttackContext context)
    {
        
    }

    public void OnJudge(JudgeContext context)
    {
        AttackType attackType = context.judgeable.attackType;

        if (attackType == AttackType.HoldStart && !context.isMiss)
        {
            animator.SetTrigger("Holding");
            if (holdSpriteAnimator != null)
            {
                holdSpriteAnimator.ResetTrigger("HoldStop");
                holdSpriteAnimator.SetTrigger("Holding");
            }
        }
        else
        {
            animator.SetTrigger("HoldStop");
            if (holdSpriteAnimator != null)
            {
                holdSpriteAnimator.ResetTrigger("Holding");
                holdSpriteAnimator.SetTrigger("HoldStop");
            }

            if (isMoveAttack)
            {
                StartCoroutine(LerpPosition(targetPosition, spawnPosition, backToBaseTime));
            }

            StartCoroutine(WaitAndBackToBase(backToBaseTime));
        }
    }

    public void OnHit(JudgeContext context)
    {
        
    }

    private IEnumerator WaitAndHoldStart(float arriveSec)
    {
        yield return new WaitForSeconds((arriveSec - StageFlowManager.Instance.currentTime) * 0.5f);
        animator.SetLayerWeight(holdLayerIndex, 1f);
        animator.SetTrigger("HoldStart");

        if (isMoveAttack)
        {
            StartCoroutine(LerpPosition(spawnPosition, targetPosition, arriveSec));
        }
    }

    private void ShowCutIn(JudgeContext context, float targetSec)
    {
        Judgeable judgeable = context.judgeable;
        if (judgeable.attackType != AttackType.HoldStart)
        {
            dynamicUIManager?.CutInDisplay(0, true);
            return;
        }

        if (context.isMiss)
            return;

        dynamicUIManager?.CutInDisplay(targetSec);
    }

    private IEnumerator WaitAndBackToBase(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        animator.SetLayerWeight(holdLayerIndex, 0f);
    }

    private IEnumerator LerpPosition(Vector3 start, Vector3 end, float endSec)
    {
        var flow = StageFlowManager.Instance;
        float duration = endSec - flow.currentTime;
        float fractionOfJourney;

        while (flow.currentTime < endSec)
        {
            fractionOfJourney = (endSec - flow.currentTime) / duration;
            transform.position = Vector3.Lerp(end, start, fractionOfJourney);
            yield return null;
        }

        transform.position = end;
    }
}
