using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Striker의 인스턴스 관리.
/// </summary>
public class StrikerManager : MonoBehaviour
{
    [SerializeField] private List<StrikerController> strikerPrefabs;
    private StrikerController strikerInstance;
    [SerializeField] private Transform projectileParent;
    [SerializeField] private Transform[] _spawnPositions; // 0: 리더, 1~4: 방향별 투사체
    private Vector3[] spawnPositions;
    [SerializeField] private float spawnPosOffset = 3f; // 기본 위치보다 offset만큼 위에서 등장하여 기본 위치로 이동
    [SerializeField] private float targetPosOffset = 1f; // 플레이어 판정 위치 보정값

    // Refs
    private PlayerManager playerManager;
    private DynamicUIManager dynamicUIManager;
    [SerializeField] public TutorialManager tutorialManager;

    // Boss
    public bool isBossStage = false; // 보스면 최초 생성된 스트라이커 계속 유지

    private void Awake()
    {
        spawnPositions = new Vector3[5];
        for (int i = 0; i < _spawnPositions.Length; i++)
        {
            spawnPositions[i] = _spawnPositions[i].position;
        }
    }

    public void SetReferences(PlayerManager playerManager, DynamicUIManager dynamicUIManager)
    {
        this.playerManager = playerManager;
        this.dynamicUIManager = dynamicUIManager;
    }

    public StrikerController GetStrikerInstance()
    {
        return strikerInstance;
    }

    public void AppearStriker(int strikerType)
    {
        if (strikerType < 0 || strikerType >= strikerPrefabs.Count) return;
        if (isBossStage && strikerInstance != null) return;

        SpawnStriker(strikerPrefabs[strikerType]);
    }

    public void DisappearStriker()
    {
        if (isBossStage) return;
        if (tutorialManager != null && tutorialManager.ShouldRestartCurrentPhase()) ClearImmediately();
        strikerInstance?.OnClear();
    }

    private void SpawnStriker(StrikerController strikerPrefab)
    {
        if (isBossStage)
        {
            spawnPositions[0] = Vector3.zero;
            spawnPosOffset = 0f;
        }

        Vector3 spawnPosition = spawnPositions[0] + spawnPosOffset * Vector3.up;
        strikerInstance = Instantiate(strikerPrefab, spawnPosition, Quaternion.identity);
        if (strikerInstance == null) return;
        
        Vector3[] targetPositions = new Vector3[5];
        for (int i = 0; i < targetPositions.Length; i++)
        {
            targetPositions[i] = playerManager.transform.position + targetPosOffset * DirTool.TranstoVec((Direction)i);
        }
        
        strikerInstance.Init(spawnPositions, targetPositions, projectileParent, dynamicUIManager);
        if (StageFlowManager.Instance != null &&
            StageFlowManager.Instance.currentStageData != null &&
            strikerInstance.Sound != null)
        {
            strikerInstance.Sound.holdingSound = StageFlowManager.Instance.currentStageData.HoldingSound;
        }
    }

    public void ClearImmediately()
    {
        if (!isBossStage && strikerInstance != null)
        {
            Destroy(strikerInstance.gameObject);
        }
    }

    public void ClearBoss()
    {
        if (!isBossStage || strikerInstance == null) return;

        strikerInstance.OnClear();
        Transform chibi = strikerInstance.transform.Find("boss_chibi");
        if (chibi != null)
            chibi.gameObject.SetActive(true);
    }
}
