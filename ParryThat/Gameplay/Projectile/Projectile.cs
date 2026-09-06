using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSetupContext
{
    public AttackType attackType;
    public float arriveSec;
    public Direction location;
    public Vector3 startPos;
    public Vector3 targetPos;

    public ProjectileSetupContext(AttackType attackType, float arriveSec, Direction location, Vector3 startPos, Vector3 targetPos)
    {
        this.attackType = attackType;
        this.arriveSec = arriveSec;
        this.location = location;
        this.startPos = startPos;
        this.targetPos = targetPos;
    }
}

public abstract class Projectile : MonoBehaviour
{
    public abstract void Setup(ProjectileSetupContext context);
    public abstract void OnJudge(JudgeContext context);
}
