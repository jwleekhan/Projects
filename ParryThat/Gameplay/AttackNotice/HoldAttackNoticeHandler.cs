using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioClipList
{
    public List<AudioClip> clips;
}

public class HoldAttackNoticeHandler : MonoBehaviour, IAttackHandler<AttackNoticeContext>
{
    [SerializeField] private GameObject[] exclamations;
    [SerializeField] private List<AudioClipList> prepareSounds;

    private AudioSource audioSource;
    private Coroutine currentCoroutine = null;

    private void Awake()
    {
        var audioSourceObject = GameObject.Find("Audio Source");
        if (audioSourceObject != null)
            audioSource = audioSourceObject.GetComponent<AudioSource>();

        ForceStop();
    }

    public void OnNotice(AttackNoticeContext context)
    {
        StageFlowManager flow = StageFlowManager.Instance;
        if (flow == null)
            return;

        float durationSec =
            flow.BeatToSec(context.note.arriveBeat) -
            flow.BeatToSec(context.note.noticeBeat);

        int strikerType = context.strikerType;
        AttackType attackType = (AttackType)context.note.type;

        if (attackType == AttackType.HoldStart)
        {
            Appear(durationSec, strikerType);

            if (context.judgeables.Count >= 2)
            {
                context.judgeables[1].AddOnDestroy(_ => ForceStop());
            }
        }
        else if (attackType == AttackType.HoldStop)
        {
            Disappear(durationSec, strikerType);
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

    public void Appear(float durationSec, int strikerType)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(Showing(durationSec, true, strikerType));
    }

    public void Disappear(float durationSec, int strikerType)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(Showing(durationSec, false, strikerType));
    }

    private IEnumerator Showing(float durationSec, bool isAppear, int strikerType)
    {
        float intervalSec = durationSec / 2f;

        for (int index = 0; index < 3; index++)
        {
            bool isAppeared = exclamations[index].activeSelf;
            exclamations[index].SetActive(isAppear);

            if (index < 2)
            {
                if (isAppear && !isAppeared)
                {
                    PlayPrepare(strikerType, index);
                }
                else if (!isAppear && isAppeared)
                {
                    PlayPrepare(strikerType, 1 - index);
                }
                
                yield return new WaitForSeconds(intervalSec);
            }
        }

        currentCoroutine = null;
    }

    private void PlayPrepare(int strikerType, int soundIndex)
    {
        if (strikerType < 0 || strikerType >= prepareSounds.Count)
            return;

        AudioClipList clipList = prepareSounds[strikerType];

        if (soundIndex < 0 || soundIndex >= clipList.clips.Count)
            return;

        AudioClip clip = clipList.clips[soundIndex];
        
        float volume =
            PlayerPrefs.GetFloat("masterVolume", 1f) *
            PlayerPrefs.GetFloat("enemyVolume", 1f);

        audioSource.PlayOneShot(clip, volume);
    }

    public void ForceStop()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        exclamations[0].SetActive(false);
        exclamations[1].SetActive(false);
        exclamations[2].SetActive(false);
    }
}