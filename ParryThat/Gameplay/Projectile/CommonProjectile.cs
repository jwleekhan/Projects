using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonProjectile : Projectile
{
    private Vector3 startPosition; // 시작 위치
    private Vector3 targetPosition; // 목표 위치

    private bool hasReachedTarget = false; // 목표 위치 도달 여부
    private Vector3 finalVelocity; // 도착 시의 마지막 속도 저장

    private float arriveSec; // 도착 시각
    private float duration;

    public override void Setup(ProjectileSetupContext context)
    {
        arriveSec = context.arriveSec;
        Direction location = context.location;
        startPosition = context.startPos;
        targetPosition = context.targetPos;

        switch (location)
        {
            case Direction.Up:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case Direction.Down:
                transform.rotation = Quaternion.Euler(0, 0, 180);
                break;
            case Direction.Left:
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
            case Direction.Right:
                transform.rotation = Quaternion.Euler(0, 0, 270);
                break;
            default:
                break;
        }
        transform.position = startPosition;

        duration = arriveSec - StageFlowManager.Instance.currentTime;
    }

    public override void OnJudge(JudgeContext context)
    {
        Destroy(gameObject);
        if (!context.isParried)
            return;

        // 위로 반격
        ParriedProjectileManager parriedProjectileManager = FindAnyObjectByType<ParriedProjectileManager>();
        if (parriedProjectileManager == null)
            return;

        Judgeable judgeable = context.judgeable;
        int fixRandom = 0;
        if (judgeable.noteDirection == Direction.Left)
            fixRandom = 1;
        else if (judgeable.noteDirection == Direction.Right)
            fixRandom = 2;
        parriedProjectileManager.ParryProjectile(Direction.Up, judgeable.attackType, fixRandom);
    }

    void Update()
    {
        LerpPosition(StageFlowManager.Instance.currentTime);
    }

    private void LerpPosition(float currentSec)
    {
        if (!hasReachedTarget)
        {
            float fractionOfJourney = (arriveSec - currentSec) / duration;
            transform.position = Vector3.Lerp(targetPosition, startPosition, fractionOfJourney);

            if (fractionOfJourney < 0f)
            {
                hasReachedTarget = true;
                finalVelocity = (targetPosition - startPosition) / duration;
            }
        }
        else
        {
            transform.position = targetPosition + finalVelocity * (currentSec - arriveSec);
            if (currentSec - arriveSec > 5f)
                Destroy(gameObject);
        }
    }
}
