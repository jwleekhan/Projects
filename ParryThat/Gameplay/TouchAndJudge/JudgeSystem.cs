using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct JudgeContext
{
    public Judgeable judgeable;
    public JudgeType judgeType;
    public bool isParried;
    public bool isMiss;

    public JudgeContext(Judgeable judgeable, JudgeType judgeType)
    {
        this.judgeable = judgeable;
        this.judgeType = judgeType;
        this.isParried = judgeType >= JudgeType.LateParried && judgeType <= JudgeType.EarlyParried;
        this.isMiss = judgeType == JudgeType.LateMiss || judgeType == JudgeType.EarlyMiss;
    }
}

/// <summary>
/// 터치 입력을 전달받아 판정 및 점수 등 후처리.
/// </summary>
public class JudgeSystem : MonoBehaviour
{
    public event Action<JudgeContext> Judged;

    [Header("Attack Judge Handlers")]
    [SerializeField] private NormalAttackJudgeHandler normalAttackJudgeHandler;
    [SerializeField] private StrongAttackJudgeHandler strongAttackJudgeHandler;
    [SerializeField] private HoldAttackJudgeHandler holdAttackJudgeHandler;

    private Dictionary<AttackType, IAttackJudgeHandler> attackJudgeHandlerDict;

    public PlayerManager playerManager;
    public StrikerManager strikerManager;

    // ScoreUI / UIManager -> DynamicUIManager
    [SerializeField] private DynamicUIManager dynamicUIManager;

    public int combo = 0;
    public int score = 0;

    private Dictionary<Direction, Queue<Judgeable>> judgeableQueues = new() {
        { Direction.None, new() },
        { Direction.Up, new() },
        { Direction.Down, new() },
        { Direction.Left, new() },
        { Direction.Right, new() },
    };
    public Queue<Touched> touchQueue = new();

    public List<int[]> judgeDetails = new List<int[]>();  

    private string[] judgeStrings = new string[8]
                                    { "공노트", "늦은 MISS", "늦은 BLOCKED", "늦은 PARRIED",
                                      "PERFECT", "빠른 PARRIED", "빠른 BLOCKED", "빠른 MISS" };

    private Dictionary<AttackType, int> touchBlockCounts = new(); // Value가 1 이상이면 터치를 무시

    private void Awake()
    {
        if (dynamicUIManager == null)
            dynamicUIManager = FindObjectOfType<DynamicUIManager>();

        attackJudgeHandlerDict = new()
        {
            { AttackType.Normal, normalAttackJudgeHandler },
            { AttackType.Strong, strongAttackJudgeHandler },
            { AttackType.HoldStart, holdAttackJudgeHandler },
            { AttackType.HoldStop, holdAttackJudgeHandler },
            { AttackType.Ghost, strongAttackJudgeHandler },
        };

        foreach (AttackType type in Enum.GetValues(typeof(AttackType)))
        {
            touchBlockCounts[type] = 0;
        }

        judgeDetails.Add(new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 }); // 0: total, 1~7: 판정별 count
    }

    void Update()
    {
        // 방향별 스트라이커 공격 Queue를 순회하며 LateMiss 여부 확인하여 처리
        foreach (var judgeableQueue in judgeableQueues.Values)
        {
            if (judgeableQueue.Count <= 0) continue;

            Judgeable tempJudgeable = judgeableQueue.Peek();

            float tempSecDiff =
                StageFlowManager.Instance.currentTime
                - StageFlowManager.Instance.BeatToSec(tempJudgeable.arriveBeat);

            // 늦은 MISS
            if (tempSecDiff > 0.2d)
            {
                JudgeManage(tempJudgeable, JudgeType.LateMiss, null);
            }
        }

        // 터치 입력 Queue에 들어온 입력을 판정
        foreach (Touched touch in touchQueue)
            Judge(touch);

        if (touchQueue.Count != 0)
            touchQueue.Clear();
    }

    public void InitChart(ChartData chart)
    {
        if (chart == null)
            return;

        if (judgeDetails.Count == 0 || judgeDetails[0] == null || judgeDetails[0].Length == 0)
            return;

        int totalNoteCount = chart.notes != null ? chart.notes.Length : 0;

        // 전체
        judgeDetails[0][0] += totalNoteCount;
        
        // Striker별 or 방향별은 일단 제거함 (사용처 없음)
    }

    public void EnqueueJudgeable(Judgeable judgeable)
    {
        judgeableQueues[judgeable.noteDirection].Enqueue(judgeable);
    }

    public Judgeable DequeueJudgeable(Direction dir)
    {
        return judgeableQueues[dir].Dequeue();
    }

    public Judgeable PeekJudgeable(Direction dir)
    {
        return judgeableQueues[dir].Peek();
    }

    public int CountJudgeable(Direction dir)
    {
        return judgeableQueues[dir].Count;
    }

    public void BlockTouch(bool setBlock, List<AttackType> touchTypes)
    {
        foreach (AttackType type in touchTypes)
        {
            if (setBlock)
                touchBlockCounts[type]++;
            else if (touchBlockCounts[type] > 0)
                touchBlockCounts[type]--;
        }
    }

    public void AllowOnly(bool setAllow, List<AttackType> touchTypes)
    {
        foreach (AttackType type in touchBlockCounts.Keys.ToList())
        {
            if (!touchTypes.Contains(type))
            {
                // 허용하지 않는 타입의 block count를 올림
                if (setAllow)
                    touchBlockCounts[type]++;
                else if (touchBlockCounts[type] > 0)
                    touchBlockCounts[type]--;
            }
        }
    }

    public void Judge(Touched touch)
    {
        // 터치 입력 차단된 type이면 즉시 리턴
        if (touchBlockCounts.ContainsKey(touch.type) && touchBlockCounts[touch.type] > 0)
            return;

        // 방향별 가장 빠른 공격
        Dictionary<Direction, Judgeable> firstJudgeables = new();
        foreach (var kvp in judgeableQueues)
        {
            if (kvp.Value.Count > 0)
                firstJudgeables[kvp.Key] = kvp.Value.Peek();
            else
                firstJudgeables[kvp.Key] = null;
        }

        Judgeable firstJudgeable = null;
        float firstArriveBeat = Mathf.Infinity;

        // 터치에 호응되는 가장 빠른 공격을 찾음
        foreach (var handler in attackJudgeHandlerDict.Values)
        {
            Judgeable tempJudgeable = handler.GetFirstJudgeable(firstJudgeables, touch);
            if (tempJudgeable != null && tempJudgeable.arriveBeat < firstArriveBeat)
            {
                firstJudgeable = tempJudgeable;
                firstArriveBeat = tempJudgeable.arriveBeat;
            }
        }

        if (firstJudgeable == null)
        {
            JudgeManage(null, JudgeType.None, touch);
            return;
        }

        // 판정 결과 받기
        AttackType attackType = firstJudgeable.attackType;
        JudgeType judgeType = attackJudgeHandlerDict[attackType].Judge(firstJudgeable, touch);

        // 후처리
        DebugJudge(touch, firstJudgeable, judgeType);
        JudgeManage(firstJudgeable, judgeType, touch);
    }

    public JudgeType GetJudgeType(double touchSec, double attackArriveSec)
    {
        double timeDiff = touchSec - attackArriveSec;
        if (timeDiff > 0.2d) return JudgeType.LateMiss;
        else if (timeDiff > 0.14d) return JudgeType.LateBlocked;
        else if (timeDiff > 0.07d) return JudgeType.LateParried;
        else if (timeDiff >= -0.07d) return JudgeType.Perfect;
        else if (timeDiff >= -0.14d) return JudgeType.EarlyParried;
        else if (timeDiff >= -0.2d) return JudgeType.EarlyBlocked;
        else return JudgeType.EarlyMiss;
    }

    private void DebugJudge(Touched touch, Judgeable judgeable, JudgeType judgeType)
    {
        Debug.Log($"Touch: Dir.{touch.direction}, Type.{touch.type}");
        if (judgeable != null)
        {
            Debug.Log($"Judgeable: Dir.{judgeable.noteDirection}, Type.{judgeable.attackType}, Beat.{judgeable.arriveBeat}");
            Debug.Log($"touchTimeSec: {touch.touchSec}, arriveSec: {StageFlowManager.Instance.BeatToSec(judgeable.arriveBeat)}");
        }
        else
        {
            Debug.Log("Judgeable is null");
        }
        Debug.Log($"판정 결과: {judgeStrings[(int)judgeType]}");
    }

    public void JudgeManage(Judgeable judgeable, JudgeType judgeType, Touched touch)
    {
        // 노트가 처리되지 않은 경우
        if (judgeable == null || judgeType == JudgeType.None)
        {
            if (touch != null && touch.type != AttackType.HoldStop)
            {
                playerManager.Operate(touch.direction, touch.type);
                playerManager.PlayerParrySound(touch.type);
            }
            
            return;
        }

        // 플레이어의 조작이 없는 경우 제외
        if (touch != null || judgeable.attackType == AttackType.HoldStop)
        {
            playerManager.Operate(judgeable.noteDirection, judgeable.attackType);
        }

        // 판정 및 점수 표시
        judgeDetails[0][(int)judgeType] += 1;
        dynamicUIManager?.DisplayJudge((int)judgeType, judgeable.noteDirection);
        SetScore(judgeType);

        // Miss일 때 피격 처리
        if (judgeType == JudgeType.LateMiss || judgeType == JudgeType.EarlyMiss)
        {
            playerManager.hp--;
            playerManager.PlayerHitSound();
            dynamicUIManager?.DisplayHP(playerManager.hp, false);

            // 피격 → 사망
            if (playerManager.hp <= 0 && StageFlowManager.Instance.currentStageData.Category != StageCategory.Tutorial)
            {
                dynamicUIManager?.HideAll();
                StageFlowManager.Instance?.GameOver();
            }
            // 피격 → 생존
            else
            {
                dynamicUIManager?.ShowDamageOverlayEffect();
                GameObject.Find("Main Camera")?.GetComponent<CameraMoving>()?.CameraShake();
            }
        }
        // 가드 시 처리
        else if (judgeType == JudgeType.LateBlocked || judgeType == JudgeType.EarlyBlocked)
        {
            playerManager.PlayerBlockedSound();
        }
        // 패링 성공 시 처리
        else if (judgeType >= JudgeType.LateParried && judgeType <= JudgeType.EarlyParried)
        {
            dynamicUIManager?.ShowParticle(Direction.Up, judgeType == JudgeType.Perfect);
        }

        // 대상 노트 제거
        FinishJudge(new JudgeContext(judgeable, judgeType));
    }

    private void SetScore(JudgeType judgeType)
    {
        // 점수 부여
        switch (judgeType)
        {
            case JudgeType.LateMiss:
            case JudgeType.EarlyMiss:
                score += 0;
                combo = 0;
                break;

            case JudgeType.LateBlocked:
            case JudgeType.EarlyBlocked:
                score += 300 + combo * 500;
                combo += 1;
                break;

            case JudgeType.LateParried:
            case JudgeType.EarlyParried:
                score += 9000 + combo * 500;
                combo += 1;
                break;

            case JudgeType.Perfect:
                score += 30000 + combo * 500;
                combo += 1;
                break;
        }

        dynamicUIManager?.DisplayScore(score);
        dynamicUIManager?.DisplayCombo(combo);
    }

    private void FinishJudge(JudgeContext context)
    {
        Judgeable judgeable = context.judgeable;
        var judgeableQueue = judgeableQueues[judgeable.noteDirection];

        if (judgeableQueue.Peek() == judgeable)
        {
            judgeableQueue.Dequeue();

            Judged?.Invoke(context);
            judgeable.Destroy(context);
        }
    }
}
