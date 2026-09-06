using UnityEngine;

public class StrikerSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [Header("준비 효과음")]
    [SerializeField] private AudioClip prepareSoundNormal;  // 일반 공격 준비 효과음 (type 0)
    [SerializeField] private AudioClip prepareSoundStrong;  // 강한 공격 준비 효과음 (type 1)
    [SerializeField] private AudioClip prepareSoundGhost;  // 고스트 공격 준비 효과음 (type 4)

    [Header("패링 효과음")]
    [SerializeField] private AudioClip parrySoundNormal;  // 일반 공격 패링 효과음 (type 0)
    [SerializeField] private AudioClip parrySoundStrong;  // 강한 공격 패링 효과음 (type 1)
    [SerializeField] private AudioClip parrySoundGhost;  // 고스트 공격 패링 효과음 (type 4)

    [Header("홀드 효과음")]
    [SerializeField] public AudioClip holdingSound;  // 홀드 중
    [SerializeField] private AudioClip holdingEnd;  // 홀드 끝

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Stop()
    {
        if (audioSource == null) return;
        audioSource.Stop();
    }

    public void PlayPrepareSound(AttackType type)
    {
        if (type == AttackType.Normal)
        {
            PlayPrepareNormal();
        }
        else if (type == AttackType.Strong)
        {
            PlayPrepareStrong();
        }
        else if (type == AttackType.Ghost)
        {
            PlayPrepareGhost();
        }
    }

    public void PlayParrySound(AttackType type)
    {
        if (type == AttackType.Normal)
        {
            PlayParryNormal();
        }
        else if (type == AttackType.Strong)
        {
            PlayParryStrong();
        }
        else if (type == AttackType.Ghost)
        {
            PlayParryGhost();
        }
    }

    public void PlayHoldSound(AttackType type)
    {
        if (type == AttackType.HoldStart)
        {
            PlayHoldStart();
        }
        else if (type == AttackType.HoldStop)
        {
            PlayHoldFinish();
        }
    }

    private void PlayPrepareNormal()
    {
        if (audioSource == null || prepareSoundNormal == null) return;
        audioSource.PlayOneShot(prepareSoundNormal, GetEffectiveEnemyVolume());
    }

    private void PlayPrepareStrong()
    {
        if (audioSource == null || prepareSoundStrong == null) return;
        audioSource.PlayOneShot(prepareSoundStrong, GetEffectiveEnemyVolume());
    }

    private void PlayPrepareGhost()
    {
        if (audioSource == null || prepareSoundGhost == null) return;
        audioSource.PlayOneShot(prepareSoundGhost, GetEffectiveEnemyVolume());
    }

    private void PlayParryNormal()
    {
        if (audioSource == null || parrySoundNormal == null) return;
        audioSource.PlayOneShot(parrySoundNormal, GetEffectivePlayerVolume());
    }

    private void PlayParryStrong()
    {
        if (audioSource == null || parrySoundStrong == null) return;
        audioSource.PlayOneShot(parrySoundStrong, GetEffectivePlayerVolume());
    }

    private void PlayParryGhost()
    {
        if (audioSource == null || parrySoundGhost == null) return;
        audioSource.PlayOneShot(parrySoundGhost, GetEffectiveEnemyVolume());
    }

    public void PlayHoldStart()
    {
        if (audioSource == null || holdingSound == null) return;
        audioSource.PlayOneShot(holdingSound, GetEffectivePlayerVolume());
    }

    public void PlayHoldFinish()
    {
        Stop();
        if (audioSource == null || holdingEnd == null) return;
        audioSource.PlayOneShot(holdingEnd, GetEffectivePlayerVolume());
    }

    private float GetEffectiveEnemyVolume()
    => PlayerPrefs.GetFloat("masterVolume", 1) * PlayerPrefs.GetFloat("enemyVolume", 1);

    private float GetEffectivePlayerVolume()
        => PlayerPrefs.GetFloat("masterVolume", 1) * PlayerPrefs.GetFloat("playerVolume", 1);
}
