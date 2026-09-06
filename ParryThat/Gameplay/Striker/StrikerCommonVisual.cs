using System.Collections;
using UnityEngine;

/// <summary>
/// Striker의 일반적인 애니메이션을 담당.
/// </summary>
public class StrikerCommonVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem particleSystemGreen; // 초록색 파티클 시스템

    // Animation Durations
    [SerializeField] private float spawnMoveDuration = 1.0f;
    [SerializeField] private float disappearDuration = 1.0f;

    [Header("Valid Animations")]
    [SerializeField] private bool hasPrepareAnim = true;
    [SerializeField] private bool hasPrepareDirection = false;
    [SerializeField] private bool hasAttackAnim = true;
    [SerializeField] private bool hasAttackDirection = false;
    [SerializeField] private bool hasDamagedAnim = true;
    [SerializeField] private bool hasDamagedDirection = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Init(Vector3 defaultPosition)
    {
        // 스트라이커를 화면 밖에서 시작 위치로 이동
        StartCoroutine(LerpPosition(transform.position, defaultPosition, StageFlowManager.Instance.currentTime + spawnMoveDuration));
    }

    public void OnNotice(StrikerAttackContext context)
    {
        if (hasPrepareAnim)
        {
            SetDirection(hasPrepareDirection, context.note.direction);
            SetStrongVal(context.note.type == (int)AttackType.Strong);
            animator.SetTrigger("Prepare");
        }
    }

    public void OnAttackStart(StrikerAttackContext context)
    {
        if (hasAttackAnim)
        {
            SetDirection(hasAttackDirection, context.note.direction);
            animator.SetTrigger("Attack");
        }
    }

    public void OnJudge(JudgeContext context)
    {

    }

    public void OnHit(JudgeContext context)
    {
        if (hasDamagedAnim)
        {
            SetDirection(hasDamagedDirection, (int)context.judgeable.noteDirection);
            animator.SetTrigger("Damaged");
        }
    }

    public void OnClear()
    {
        SetDirection(false, 0);
        animator.SetBool("isClear", true);
        if (particleSystemGreen != null) particleSystemGreen.Play();
        Destroy(gameObject, disappearDuration);
    }

    private void SetDirection(bool hasDirection, int direction = 0)
    {
        if (hasDirection)
            animator.SetFloat("Direction", direction);
        else
            animator.SetFloat("Direction", 0f);
    }

    private void SetStrongVal(bool isStrong)
    {
        if (isStrong)
            animator.SetFloat("StrongVal", 1f);
        else
            animator.SetFloat("StrongVal", 0f);
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
