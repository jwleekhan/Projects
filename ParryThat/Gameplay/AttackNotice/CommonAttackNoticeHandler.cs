using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonAttackNoticeHandler : MonoBehaviour, IAttackHandler<AttackNoticeContext>
{
    [SerializeField] private Transform noticeParent; // 예고 표시 위치
    [SerializeField] private NoticeAnim[] noticePrefabs; // 공격 타입별 예고 프리팹 배열
    private List<NoticeAnim> noticeInstances = new(); // 예고 인스턴스 저장

    [SerializeField] private float noticeScale = 1f;
    [SerializeField] private float noticeSpacing = 30f; // 예고 인스턴스 사이 간격

    public void OnNotice(AttackNoticeContext context)
    {
        NoticeAnim newNotice = AddNotice((AttackType)context.note.type, (Direction)context.note.direction);
        var flow = StageFlowManager.Instance;
        // Notice 안 사라지는 버그로 인해 임시차단
        //newNotice.SetPreHitTimer(flow.bpm, flow.BeatToSec(context.note.arriveBeat));
        if (context.judgeables.Count > 0)
        {
            context.judgeables[0].AddOnDestroy(context => RemoveNotice(newNotice, context.isMiss));
        }
    }

    void IAttackHandler.OnNotice(IAttackContext context)
        => OnNotice((AttackNoticeContext)context);

    public void OnAttackStart(AttackNoticeContext context)
    {

    }

    void IAttackHandler.OnAttackStart(IAttackContext context)
        => OnAttackStart((AttackNoticeContext)context);

    public void OnJudge(JudgeContext context)
    {

    }

    private NoticeAnim AddNotice(AttackType attackType, Direction direction)
    {
        // Notice 오브젝트 생성
        NoticeAnim newNotice = Instantiate(noticePrefabs[(int)attackType], noticeParent);
        
        // 위치 설정
        int currentIndex = noticeInstances.Count;
        Vector3 newNoticePosition = new Vector3(currentIndex * noticeSpacing, 0, 0);
        newNotice.transform.localPosition = newNoticePosition;

        // 회전 (Direction에 따라)
        float rotationAngle = 0f;
        if (attackType == AttackType.Ghost)
            direction = DirTool.ReverseDir(direction);
        switch (direction)
        {
            case Direction.Up: rotationAngle = 0f; break;
            case Direction.Down: rotationAngle = 180f; break;
            case Direction.Left: rotationAngle = 90f; break;
            case Direction.Right: rotationAngle = -90f; break;
        }
        newNotice.transform.localRotation = Quaternion.Euler(0, 0, rotationAngle);

        // Scale
        newNotice.transform.localScale = new Vector3(noticeScale, noticeScale, 1);

        // 생성 애니메이션
        newNotice.Init();
        newNotice.SpawnSignal(StageFlowManager.Instance.bpm);

        noticeInstances.Add(newNotice);
        return newNotice;
    }

    private void RemoveNotice(NoticeAnim noticeInstance, bool isMiss)
    {
        if (noticeInstance == null) return;

        if (!isMiss)
        {
            noticeInstance.HitSignal(StageFlowManager.Instance.bpm, Relocation);
        }
        else
        {
            noticeInstance.MissSignal(StageFlowManager.Instance.bpm, Relocation);
        }
    }

    private void Relocation(NoticeAnim noticeInstance)
    {
        noticeInstances.Remove(noticeInstance);
        Destroy(noticeInstance.gameObject);

        if (noticeInstances.Count > 0)
        {
            // 남은 예고 인스턴스 위치 재배치
            for (int i = 0; i < noticeInstances.Count; i++)
            {
                noticeInstances[i].transform.localPosition = new Vector3(i * noticeSpacing, 0, 0);
            }
        }
    }

    private void RemoveAll()
    {
        while (noticeInstances.Count > 0)
        {
            Destroy(noticeInstances[0]);
            noticeInstances.RemoveAt(0);
        }
    }
}
