using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class EnhanceSystem : MonoBehaviour
{
    public static EnhanceSystem Instance { get; private set; }

    [SerializeField] List<SubEquipmentData> subEquipments;
    Dictionary<SubEquipmentData, int> subEquipLevels = new();
    Dictionary<SubEquipmentData, SubEquipment> subEquipInstances = new();
    HashSet<SubEquipmentData> evolvedBaseEquips = new();
    Dictionary<string, SubEquipmentData> subEquipIDtoData = new();
    Dictionary<string, string> stringDict = new();
    Dictionary<(SubEquipmentData, int), Dictionary<string, object>> additionalEffectDict = new();
    Dictionary<(SubEquipmentData, int), Dictionary<string, object>> levelDataDict = new();

    [SerializeField] int normalMaxLevel = 7;
    [SerializeField] int specialMaxLevel = 5;
    [SerializeField] int passiveMaxLevel = 3;

    [Header("Enhance Choice Probability")]
    [SerializeField] int numChoices = 3;
    [SerializeField] float normalProbability = 0.7f;
    [SerializeField] float specialProbability = 0.1f;
    [SerializeField] float passiveProbability = 0.2f;

    AdditionalEffectManager additionalEffectManager;
    EvolutionSystem evolutionSystem;
    EnhanceUI enhanceUI;
    Transform player;

    void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerManager.playerReady += OnPlayerReady;

        InitComponents();
        InitTableDicts();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PlayerManager.playerReady -= OnPlayerReady;
        }
    }

    void InitComponents()
    {
        additionalEffectManager = GetComponent<AdditionalEffectManager>();
        if (additionalEffectManager == null)
        {
            additionalEffectManager = gameObject.AddComponent<AdditionalEffectManager>();
        }

        evolutionSystem = GetComponent<EvolutionSystem>();
        if (evolutionSystem == null)
        {
            evolutionSystem = gameObject.AddComponent<EvolutionSystem>();
        }
    }

    void InitTableDicts()
    {
        InitIDtoData();
        InitStringDict();
        InitAdditionalEffectDict();
        InitLevelDataDict();
    }

    public void InitIDtoData()
    {
        subEquipIDtoData.Clear();

        foreach (var subEquipment in subEquipments)
        {
            if (subEquipment == null)
            {
                Debug.LogWarning("SubEquipmentData Initialize Warning");
                continue;
            }

            // 보조장비 ID to ScriptableObject 딕셔너리 할당
            if (!subEquipIDtoData.ContainsKey(subEquipment.subEquipID))
            {
                subEquipIDtoData[subEquipment.subEquipID] = subEquipment;
            }
        }
    }

    void InitStringDict()
    {
        stringDict.Clear();

        List<Dictionary<string, object>> stringTable = CSVReader.Read("StringTable");

        if (stringTable == null || stringTable.Count == 0)
            return;

        foreach (var stringData in stringTable)
        {
            string key = stringData["StringID"].ToString();
            string value = stringData["String"].ToString();

            if (!stringDict.ContainsKey(key))
            {
                stringDict[key] = value;
            }
        }

        stringTable = null;
    }

    void InitAdditionalEffectDict()
    {
        additionalEffectDict.Clear();

        List<Dictionary<string, object>> subEquipAdditionalEffectTable = CSVReader.Read("SubEquip_AdditionalEffectTable");

        foreach (var additionalEffect in subEquipAdditionalEffectTable)
        {
            string subEquipID = additionalEffect["SubEquipID"].ToString();
            SubEquipmentData subEquipment = GetSubEquipDataFromID(subEquipID);
            int level = Convert.ToInt32(additionalEffect["SubEquipLevel"]);

            if (subEquipment == null)
            {
                Debug.LogWarning(subEquipID + " not exists in subEquipIDtoData");
                continue;
            }

            if (!additionalEffectDict.ContainsKey((subEquipment, level)))
            {
                additionalEffectDict[(subEquipment, level)] = additionalEffect;
            }
        }

        subEquipAdditionalEffectTable = null;
    }

    void InitLevelDataDict()
    {
        levelDataDict.Clear();

        List<Dictionary<string, object>> subEquipLevelTable = CSVReader.Read("SubEquip_LevelTable");

        foreach (var levelData in subEquipLevelTable)
        {
            string subEquipID = levelData["SubEquipID"].ToString();
            SubEquipmentData subEquipment = GetSubEquipDataFromID(subEquipID);
            int level = Convert.ToInt32(levelData["SubEquipLevel"]);

            if (subEquipment == null)
            {
                Debug.LogWarning(subEquipID + " not exists in subEquipIDtoData");
                continue;
            }

            // String 참조 변환 작업
            string descString = levelData["DescStringID"].ToString();
            if (stringDict.ContainsKey(descString))
            {
                descString = stringDict[descString];
            }
            levelData["DescStringID"] = descString;

            // 추가효과 Dictionary 합치기
            if (subEquipment.hasAdditionalEffect)
            {
                var additionalEffect = GetAdditionalEffect(subEquipment, level);
                if (additionalEffect != null)
                {
                    foreach (var keyValuePair in additionalEffect)
                    {
                        // 중복 키 제거하며 병합
                        if (!levelData.ContainsKey(keyValuePair.Key))
                        {
                            levelData[keyValuePair.Key] = keyValuePair.Value;
                        }
                    }
                }
            }

            // levelDataDict 할당
            if (!levelDataDict.ContainsKey((subEquipment, level)))
            {
                levelDataDict[(subEquipment, level)] = levelData;
            }
        }

        subEquipLevelTable = null;
    }

    public void AssignSubEquipments(List<SubEquipmentData> assets)
    {
        subEquipments.Clear();
        subEquipments.AddRange(assets);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Lobby"))
        {
            ResetEnhanceSystem();
            return;
        }

        enhanceUI = FindAnyObjectByType<EnhanceUI>();
        evolutionSystem.SetEvolutionUI();

        StartCoroutine(RespawnSubEquipments());
    }

    public void ResetEnhanceSystem()
    {
        subEquipLevels.Clear();
        subEquipInstances.Clear();
        evolvedBaseEquips.Clear();

        foreach (var subEquipment in subEquipments)
        {
            if (subEquipment == null)
            {
                Debug.LogWarning("SubEquipmentData Initialize Warning");
                continue;
            }

            // 보조장비 레벨 딕셔너리 할당
            if (!subEquipLevels.ContainsKey(subEquipment))
            {
                subEquipLevels[subEquipment] = 0;
            }
        }

        evolutionSystem.ResetEvolutionSystem();
    }

    IEnumerator RespawnSubEquipments()
    {
        if (player == null || subEquipInstances.Count <= 0)
        {
            Debug.Log($"Player is {player}, subEquipInstances count is {subEquipInstances.Count}");
            yield break;
        }

        // 보조장비 모두 파괴
        foreach (var subEquipment in subEquipInstances.Keys.ToList())
        {
            Debug.Log($"{subEquipment.subEquipID}, level: {subEquipLevels[subEquipment]}");
            // 정상적인 경우
            if (subEquipInstances[subEquipment] != null)
            {
                Destroy(subEquipInstances[subEquipment].gameObject);
            }
            // 없어야 하는 key가 존재하는 경우 -> key 제거
            else if (subEquipLevels[subEquipment] <= 0)
            {
                subEquipInstances.Remove(subEquipment);
            }
            // subEquipInstances[subEquipment] == null && subEquipLevels[subEquipment] > 0
            // : 보유해야 하지만 보유하지 않고 있던 경우
            // 아래에서 생성
        }

        yield return new WaitForEndOfFrame();

        // 보조장비 재생성
        foreach (var subEquipment in subEquipInstances.Keys.ToList())
        {
            SpawnSubEquip(subEquipment);
            subEquipInstances[subEquipment].LevelUp(GetLevelData(subEquipment, subEquipLevels[subEquipment]));
        }
    }

    void OnPlayerReady()
    {
        player = PlayerManager.Instance.GetPlayer().transform;
        LoadFromCheckPoint();
    }

    void Start()
    {
        // 로비가 아닌 곳에서 시작하는 경우 대비
        if (!SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            ResetEnhanceSystem();
        }
    }

    public void LoadFromCheckPoint()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasCheckPoint())
        {
            return;
        }

        if (subEquipLevels.Values.Any(level => level > 0))
        {
            Debug.LogWarning("SubEquipments already exist. LoadFromCheckPoint aborted.");
            return;
        }

        // 저장된 보조장비 레벨 불러오기
        Dictionary<string, int> loadedSubEquipLevels = null;
        try
        {
            CheckPointSaveObject checkPoint = SaveManager.Instance.GetCheckPoint();
            loadedSubEquipLevels = checkPoint.GetSubEquipLevelsFromSave();
        }
        catch (NullReferenceException)
        {
            Debug.LogWarning("SaveObject is null");
        }

        if (loadedSubEquipLevels != null)
        {
            Debug.Log("Loaded sub equipment levels from save data.");

            HashSet<SubEquipmentData> evolvedSubEquips = new();

            // 강화 적용
            foreach (var subEquipment in subEquipments)
            {
                if (subEquipment == null)
                {
                    Debug.LogWarning("SubEquipmentData Load Warning");
                    continue;
                }

                string subEquipID = subEquipment.subEquipID;
                if (loadedSubEquipLevels.ContainsKey(subEquipID) && subEquipLevels.ContainsKey(subEquipment))
                {
                    // 진화 장비는 따로 처리
                    if (subEquipment.category == SubEquipmentData.Category.evolved)
                    {
                        evolvedSubEquips.Add(subEquipment);
                        continue;
                    }
                    ApplyEnhance(subEquipment, loadedSubEquipLevels[subEquipID]);
                }
            }

            // 진화 적용
            foreach (var evolutionTarget in evolutionSystem.GetEvolutionTargets())
            {
                SubEquipmentData evoResultData = GetSubEquipDataFromID(evolutionTarget.evoResultID);
                if (evolvedSubEquips.Contains(evoResultData))
                {
                    evolutionSystem.ApplyEvolution(evolutionTarget);
                }
            }
        }
    }

    public void StartEnhance(bool isSpecialOnly = false)
    {
        // 리스트에서 랜덤 뽑기
        List<SubEquipmentData> choices = GenerateChoices(isSpecialOnly);

        // 모든 강화 완료된 경우 즉시 종료
        if (choices.Count <= 0)
        {
            GameManager.Instance.OnEnhanceEnd();
            return;
        }

        // UI에 정보 출력
        List<Dictionary<string, object>> nextLevelData = new List<Dictionary<string, object>>();
        foreach (var choice in choices)
        {
            nextLevelData.Add(GetLevelData(choice, subEquipLevels[choice] + 1));
        }
        enhanceUI.ShowEnhanceUI(choices, nextLevelData);
    }

    List<SubEquipmentData> GenerateChoices(bool isSpecialOnly = false)
    {
        // Passive까지 구현하면, 코드 반복되는 부분 깔끔하게 반복문으로 짜겠음

        // 조건1: 서로 다른 3개를 선발
        // 조건2: 만렙인 장비는 배제
        // 조건3: 특정 한 분류가 전부 만렙이면 해당 분류는 배제
        // 만렙이 아닌 장비가 3개 미만이면 반환값이 3개 미만

        List<SubEquipmentData> choices = new List<SubEquipmentData>();
        List<SubEquipmentData> normalEnhanceTargets = new List<SubEquipmentData>();
        List<SubEquipmentData> specialEnhanceTargets = new List<SubEquipmentData>();
        List<SubEquipmentData> passiveEnhanceTargets = new List<SubEquipmentData>();

        // 전체 보조장비를 분류에 따라 만렙 여부 검사해서 강화 후보 리스트에 할당
        foreach (var subEquipment in subEquipments)
        {
            if (subEquipLevels[subEquipment] < GetMaxLevel(subEquipment))
            {
                switch (subEquipment.category)
                {
                    case SubEquipmentData.Category.normal:
                        normalEnhanceTargets.Add(subEquipment);
                        break;
                    case SubEquipmentData.Category.special:
                        specialEnhanceTargets.Add(subEquipment);
                        break;
                    case SubEquipmentData.Category.passive:
                        passiveEnhanceTargets.Add(subEquipment);
                        break;
                    default:
                        break;
                }
            }
        }

        // 강화 대상 선발
        while (choices.Count < numChoices)
        {
            bool includeNormal = normalEnhanceTargets.Count > 0 && !isSpecialOnly;
            bool includeSpecial = specialEnhanceTargets.Count > 0;
            bool includePassive = passiveEnhanceTargets.Count > 0 && !isSpecialOnly;

            // 모든 장비 강화 완료되었으면 중단
            if (!includeNormal && !includePassive && !includeSpecial)
                break;

            // 각 분류의 포함 여부에 따라 Random Range가 다름
            float randomRange = 0f;
            if (includeNormal) randomRange += normalProbability;
            if (includeSpecial) randomRange += specialProbability;
            if (includePassive) randomRange += passiveProbability;

            float randomValue = Random.Range(0, randomRange);
            float threshold = 0f;
            SubEquipmentData currentChoice = null;

            // 로직 이해를 위한 예시
            // (확률: 일반 0.7, 특수 0.1, 패시브 0.2)
            // 일반 보조장비 강화 완료 (!includeNormal), 따라서 Random Range [0, 0.3f]인 상황
            // randomValue = 0.25f가 나왔다고 가정
            // 첫번째 if문은 !includeNormal이므로 pass
            // 두번째 if문 진입, 임계값 0.1, randomValue가 더 크기 때문에 pass
            // 세번째 if문 진입, 임계값 0.3, 패시브 강화 후보 중 하나를 pop
            if (currentChoice == null && includeNormal)
            {
                threshold += normalProbability;
                if (randomValue < threshold)
                {
                    currentChoice = normalEnhanceTargets[Random.Range(0, normalEnhanceTargets.Count)];
                    normalEnhanceTargets.Remove(currentChoice);
                }
            }
            if (currentChoice == null && includeSpecial)
            {
                threshold += specialProbability;
                if (randomValue < threshold)
                {
                    currentChoice = specialEnhanceTargets[Random.Range(0, specialEnhanceTargets.Count)];
                    specialEnhanceTargets.Remove(currentChoice);
                }
            }
            if (currentChoice == null && includePassive)
            {
                threshold += passiveProbability;
                if (randomValue < threshold)
                {
                    currentChoice = passiveEnhanceTargets[Random.Range(0, passiveEnhanceTargets.Count)];
                    passiveEnhanceTargets.Remove(currentChoice);
                }
            }

            // 중복 검사 후 강화 대상으로 추가
            if (currentChoice != null && !choices.Contains(currentChoice))
            {
                choices.Add(currentChoice);
            }
        }

        return choices;
    }

    public Dictionary<string, object> GetLevelData(SubEquipmentData subEquipment, int level)
    {
        if (levelDataDict.ContainsKey((subEquipment, level)))
        {
            return levelDataDict[(subEquipment, level)].ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        return null;
    }

    Dictionary<string, object> GetAdditionalEffect(SubEquipmentData subEquipment, int level)
    {
        if (!subEquipment.hasAdditionalEffect)
            return null;

        if (additionalEffectDict.ContainsKey((subEquipment, level)))
        {
            return additionalEffectDict[(subEquipment, level)].ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        return null;
    }

    public void ApplyEnhance(SubEquipmentData subEquipment, int levelUpAmount = 1)
    {
        int targetLevel = Mathf.Min(GetMaxLevel(subEquipment), subEquipLevels[subEquipment] + levelUpAmount);
        //int targetLevel = GetMaxLevel(subEquipment); // for test
        if (targetLevel == subEquipLevels[subEquipment])
            return;

        // 패시브는 다른 방식으로 강화
        if (subEquipment.category == SubEquipmentData.Category.passive)
        {
            if (additionalEffectManager == null)
            {
                Debug.LogWarning("AdditionalEffectManager is null");
                return;
            }
            additionalEffectManager.LevelUp(GetAdditionalEffect(subEquipment, subEquipLevels[subEquipment]), GetAdditionalEffect(subEquipment, targetLevel));
        }
        else
        {
            // 신규 획득 처리
            if (!subEquipInstances.ContainsKey(subEquipment))
            {
                if (subEquipment.prefab == null)
                {
                    Debug.LogWarning("This subEquipment does not have prefab");
                    return;
                }

                // 플레이어 자식 오브젝트 추가 및 딕셔너리에 저장
                SpawnSubEquip(subEquipment);
            }

            // 추상 클래스 SubEquipment를 상속받은 자식의 메서드 호출됨
            subEquipInstances[subEquipment].LevelUp(GetLevelData(subEquipment, targetLevel));
        }

        subEquipLevels[subEquipment] = targetLevel;
        Debug.Log(subEquipment.subEquipID + " Level " + subEquipLevels[subEquipment]);
        evolutionSystem.OnEnhanceApplied(subEquipment);

        if (enhanceUI.IsEnhanceUIActive())
        {
            enhanceUI.HideEnhanceUI();
            GameManager.Instance.OnEnhanceEnd();
        }
    }

    void SpawnSubEquip(SubEquipmentData subEquipment)
    {
        if (subEquipment.prefab == null)
        {
            Debug.LogWarning("This subEquipment does not have prefab");
            return;
        }
        Debug.Log($"Spawning sub equipment: {subEquipment.subEquipID} at player position. player is {player}");
        GameObject instance = Instantiate(subEquipment.prefab, player);
        instance.name = subEquipment.subEquipID;
        if (SceneManager.GetActiveScene().name.Contains("Event")) instance.SetActive(false);
        subEquipInstances[subEquipment] = instance.GetComponent<SubEquipment>();
    }

    public int GetMaxLevel(SubEquipmentData subEquipment)
    {
        switch (subEquipment.category)
        {
            case SubEquipmentData.Category.normal:
                return normalMaxLevel;
            case SubEquipmentData.Category.special:
                return specialMaxLevel;
            case SubEquipmentData.Category.passive:
                return passiveMaxLevel;
            default:
                return 0;
        }
    }

    public IReadOnlyDictionary<SubEquipmentData, int> GetSubEquipLevels(bool excludeEvolvedBaseEquips = true)
    {
        if (excludeEvolvedBaseEquips)
        {
            return subEquipLevels.Where(kv => !evolvedBaseEquips.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        else
        {
            return subEquipLevels;
        }
    }

    public int GetSubEquipLevel(string subEquipID, bool excludeEvolvedBaseEquips = true)
    {
        SubEquipmentData subEquipment = GetSubEquipDataFromID(subEquipID);
        if (subEquipment != null && subEquipLevels.ContainsKey(subEquipment))
        {
            if (excludeEvolvedBaseEquips && evolvedBaseEquips.Contains(subEquipment))
            {
                return 0;
            }
            else
            {
                return subEquipLevels[subEquipment];
            }
        }
        else
            return 0;
    }

    public SubEquipmentData GetSubEquipDataFromID(string subEquipID)
    {
        if (subEquipIDtoData.ContainsKey(subEquipID))
            return subEquipIDtoData[subEquipID];
        else
            return null;
    }

    public void ApplyEvolution(SubEquipmentData baseSubEquip, SubEquipmentData resultSubEquip)
    {
        subEquipLevels[resultSubEquip] = subEquipLevels[baseSubEquip];
        if (subEquipInstances.ContainsKey(baseSubEquip) && subEquipInstances[baseSubEquip] != null)
        {
            subEquipInstances[resultSubEquip] = subEquipInstances[baseSubEquip];
            subEquipInstances.Remove(baseSubEquip);
            subEquipInstances[resultSubEquip].name = resultSubEquip.subEquipID;
            subEquipInstances[resultSubEquip].LevelUp(GetLevelData(resultSubEquip, subEquipLevels[resultSubEquip]));
        }
        evolvedBaseEquips.Add(baseSubEquip);
    }
}
