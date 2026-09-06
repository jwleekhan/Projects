using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public enum Character { None, RedRidingHood }
public enum Stage { None, WolfForest }

public class GameManager : MonoBehaviour
{
    // 싱글톤: 타 스크립트에서 GameManager.Instance로 접근 가능
    public static GameManager Instance { get; private set; }

    public static string[] characterNames = { "None", "빨간망토" };
    public static string[] stageNames = { "None", "늑대가 나오는 숲" };

    public Character curCharacter { get; private set; }
    public Stage curStage { get; private set; }

    private UIManager uiManager;
    private UIManager2 uiManager2;
    [SerializeField] GameObject player;
    [SerializeField] GameObject[] maps;
    [SerializeField] GameObject scrolling_background;
    [SerializeField] GameObject bulletSet;

    //지금 작업하고 있는 씬을 변수로 입력하면 됨. Build Profiles에서 자신의 씬 넣을 것.
    public string mainScene = "MainScene";
    public string lobbyScene = "LobbyScene";

    private bool isGameOver = false; // 게임 오버 변수로 중복 게임오버 처리 방지.
    private bool isTimeStopped = false;
    int enhanceActive = 0;
    int enhanceQueued = 0;

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
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PlayerManager.playerReady -= OnPlayerReady;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Lobby"))
        {
            isTimeStopped = false;
            isGameOver = false; // 로비 씬에 들어오면 게임 오버 상태 초기화
            return; // 로비 씬은 GameManager가 필요 없으므로 아무것도 하지 않음
        }

        // 보스 다음 페이즈 시작 시 체크포인트 저장
        if (scene.name == "Phase4" || scene.name == "Phase7")
        {
            BuildCheckPointFile();
        }

        // BGM Play
        {
            // switch-case 대신 if 쓰는 이유 : 씬 이름 완전 대조 말고 포함이라던가 여러 조건 쓸일 있을까봐
            if (scene.name == "Phase1" || scene.name == "Phase2" || scene.name == "specialEventScene" ||
                 scene.name == "specialEventScene2" || scene.name == "Phase4" || scene.name == "Phase5")
            {
                SoundManager.Instance.PlayBGM("bgmStage1Day", 0.4f);
            }
            if (scene.name == "Phase7" || scene.name == "Phase8" || scene.name == "specialEventScene3")
            {
                SoundManager.Instance.PlayBGM("bgmStage1Night", 0.4f);
            }
            if (scene.name == "ViperMidBossScene")
            {
                SoundManager.Instance.PlayBGM("bgmStage1Viper", 0.4f);
            }
            if (scene.name == "GhostMidBossScene")
            {
                SoundManager.Instance.PlayBGM("bgmStage1Ghost", 0.5f);
            }
            if (scene.name == "WolfFinalBossScene")
            {
                SoundManager.Instance.PlayBGM("bgmStage1BossWolf", 0.5f);
            }
        }

        isGameOver = false; // 게임 오버 상태 초기화

        maps = GameObject.FindGameObjectsWithTag("Map");
        scrolling_background = GameObject.FindGameObjectWithTag("ScrollingBG");
        bulletSet = GameObject.FindGameObjectWithTag("BulletSet");
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        uiManager2 = GameObject.Find("UIManager").GetComponent<UIManager2>();
    }

    void OnPlayerReady()
    {
        player = PlayerManager.Instance.GetPlayer();
    }

    public void LoadLobbyScene()
    {
        // 메인 씬에서 로비 씬으로 전환
        curCharacter = Character.None;
        curStage = Stage.None;
        SceneManager.LoadScene(lobbyScene);
    }

    public void LoadMainScene()
    {
        // 현재 캐릭터와 스테이지가 한 종류뿐이므로 임시로 코드 간단하게 적용
        curCharacter = Character.RedRidingHood;
        curStage = Stage.WolfForest;
        SceneManager.LoadScene(mainScene);
    }

    public void RestartMainScene()
    {
        PlayerManager.Instance.DestroyPlayer();
        EnhanceSystem.Instance.ResetEnhanceSystem();
        LoadCheckPointScene(); // UI가 없으므로 체크포인트가 있다면 무조건 적용. 없으면 메인 씬이 로드됨
    }

    public void LoadCheckPointScene()
    {
        if (!SaveManager.Instance.HasCheckPoint())
        {
            LoadMainScene();
            return;
        }

        CheckPointSaveObject checkPoint = SaveManager.Instance.GetCheckPoint();

        // 현재 캐릭터와 스테이지가 한 종류뿐이므로 임시로 코드 간단하게 적용
        curCharacter = Character.RedRidingHood;
        curStage = Stage.WolfForest;

        if (LobbySelectionManager.Instance != null)
        {
            LobbySelectionManager.Instance.setSelectedPassive(checkPoint.selectedPassive);
        }

        try
        {
            SceneManager.LoadScene(checkPoint.phaseName);
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("CheckPoint phaseName is invalid. Loading mainScene instead.");
            LoadMainScene();
        }
    }

    void Update()
    {
        // Debug
        if (Input.GetKeyDown(KeyCode.End) || Input.GetKeyDown(KeyCode.RightShift))
        {
            uiManager.SetDeathCause("Debug Key Pressed");
            GameOver();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            BuildCheckPointFile();
        }
    }

    public void OnPlayerLevelUp(int levelUpAmount = 1)
    {
        if (levelUpAmount <= 0)
            return;

        if (enhanceActive > 0)
        {
            enhanceQueued += levelUpAmount;
            return;
        }

        enhanceActive += levelUpAmount;
        OnEnhanceStart();
    }

    void OnEnhanceStart()
    {
        if (EnhanceSystem.Instance != null)
        {
            Time.timeScale = 0f;
            isTimeStopped = true;
            EnhanceSystem.Instance.GetComponent<EnhanceSystem>().StartEnhance();
        }
    }

    public void OnEnhanceEnd()
    {
        if (EnhanceSystem.Instance != null)
        {
            Time.timeScale = 1f;
            isTimeStopped = false;
            if (!SceneManager.GetActiveScene().name.Contains("Event") && player.TryGetComponent<PlayerStat>(out var playerStat))
            {
                playerStat.makeInvincible(1f);
                playerStat.triggerHitBlink(1f, false);
            }
        }

        enhanceActive--;
        if (enhanceActive > 0 || enhanceQueued > 0)
        {
            enhanceActive += enhanceQueued;
            enhanceQueued = 0;
            OnEnhanceStart();
        }
    }

    public bool IsTimeStopped()
    {
        return isTimeStopped;
    }

    /// <summary>
    /// 보스가 사망 판정되는 즉시 호출되어야 하는 함수
    /// </summary>
    public void OnBossClear()
    {
        PlayerStat playerStat = player.GetComponent<PlayerStat>();
        playerStat.SetTimeHpDownActive(false);
        playerStat.makeInvincible(0.5f);
        uiManager.DisableBossUI();
    }

    /// <summary>
    /// 보스의 사망 애니메이션이 끝나고 완전히 사라지면 호출되어야 하는 함수
    /// </summary>
    public void OnBossDestoryed()
    {
        if (SceneManager.GetActiveScene().name.Contains("Mid"))
        {
            player.GetComponent<PlayerStat>().makeInvincible(0.5f);
            Invoke("RewardAfterBoss", 0.5f);
        }
        else if (SceneManager.GetActiveScene().name.Contains("Final"))
        {
            GameClear();
        }
    }

    void RewardAfterBoss()
    {
        player.GetComponent<PlayerStat>().heal(PlayerManager.Instance.healAfterBoss);
        player.GetComponent<PlayerAction>().AddSmokeShell(2);

        // 특수 보조장비 강화 선택지 등장
        if (EnhanceSystem.Instance != null)
        {
            Time.timeScale = 0f;
            isTimeStopped = true;
            EnhanceSystem.Instance.GetComponent<EnhanceSystem>().StartEnhance(true);
        }

        // 보상 수령 후 잠시 뒤 씬 전환
        Invoke("GoNextSceneAfterBoss", 0.1f);
    }

    void GoNextSceneAfterBoss()
    {
        foreach (var map in maps)
        {
            if (map.TryGetComponent<MapScroll>(out var mapScroll))
            {
                mapScroll.isScrolling = false;
            }

            if (map.TryGetComponent<SceneTransition>(out var sceneTransition))
            {
                sceneTransition.StartFadeOut();
            }
        }
    }

    public void BuildCheckPointFile()
    {
        CheckPointSaveObject saveObject = new CheckPointSaveObject();

        if (player != null && player.TryGetComponent<PlayerStat>(out var playerStat) && player.TryGetComponent<PlayerAction>(out var playerAction))
        {
            saveObject.playerLevel = playerStat.level;
            saveObject.playerExp = playerStat.playerExp;
            saveObject.health = playerStat.currentHp;
            saveObject.score = playerStat.score;
            saveObject.skill1Count = playerAction.GetMoonSkillCount();
            saveObject.skill2Count = playerAction.GetSmokeShellCount();
            if (player.TryGetComponent<RedRidingHood>(out var redRidingHood))
            {
                saveObject.skill3Timer = redRidingHood.GetFiveArrowTimer();
                saveObject.skill4Timer = redRidingHood.GetHuntTimer();
            }
            saveObject.phaseName = SceneManager.GetActiveScene().name;
            if (LobbySelectionManager.Instance != null)
            {
                saveObject.selectedPassive = LobbySelectionManager.Instance.getSelectedPassive();
            }
            if (EnhanceSystem.Instance != null)
            {
                saveObject.SetSubEquipLevelsForSave(EnhanceSystem.Instance.GetSubEquipLevels(false));
            }
            if (AdaptationRecordSystem.Instance != null)
            {
                saveObject.adaptationRecord = AdaptationRecordSystem.Instance.GetRecord(curCharacter, curStage).Clone();
            }

            SaveManager saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SaveCheckPoint(saveObject);
                Debug.Log("Save file built successfully.");
            }
            else
            {
                Debug.LogWarning("saveManager not found.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerStat not found. Cannot build save file.");
        }
    }

    // 게임 진행 상황을 저장하는 메서드. 체크포인트 이외에 게임오버 상황에서 호출됨.
    public void SaveProgress()
    {
        CheckPointSaveObject so = new CheckPointSaveObject();
        PlayerStat playerStat = GameObject.FindWithTag("Player").GetComponent<PlayerStat>();
    }

    public void GameClear()
    {
        if (isGameOver) return; // 이미 게임 오버 상태이면 중복 처리 방지.
        isGameOver = true; // Clear와 GameOver에 동일한 플래그 사용
        
        Debug.Log("GameClear");

        // 체크포인트 삭제
        SaveManager.Instance.DestroyCheckPoint();

        enhanceActive = 0;
        enhanceQueued = 0;

        // 주요 스크립트 비활성화하여 게임 제어 차단
        DisableComponents();

        if (uiManager != null)
        {
            Console.WriteLine("GameClear UI 활성화");
            uiManager.DisablePauseButton();
            uiManager.ShowBossClearPanel();
        }

        if (AdaptationRecordSystem.Instance != null && player != null && player.TryGetComponent<PlayerStat>(out var playerStat))
        {
            // 유니콘 페이즈 선택 옵션 사용 시 환생 포인트 지급하지 않음
            if (UniconManager.Instance == null || UniconManager.Instance.selectedMainScene == UniconManager.Instance.defaultMainScene)
            {
                AdaptationRecordSystem.Instance.GainPoint(curCharacter, curStage, playerStat.level);
            }
        }
    }

    public async UniTask GameOver(string deathCause = null)
    {
        if (isGameOver) return; // 이미 게임 오버 상태이면 중복 처리 방지.
        isGameOver = true;
        //deathCause는 나중에 확장 예정. 텍스트에 죽은 원인 표시할 예정
        Debug.Log("GameOver");

        enhanceActive = 0;
        enhanceQueued = 0;

        // 주요 스크립트 비활성화하여 게임 제어 차단
        DisableComponents();

        if (uiManager != null)
        {
            Console.WriteLine("GameOver UI 활성화");
            uiManager.DisablePauseButton();
            await uiManager.ShowGameOverPanel();
        }
        else if (uiManager2 != null) {
            Console.WriteLine("GameOver UI 활성화 : uiManager2");
            uiManager2.DisablePauseButton();
            await uiManager2.ShowGameOverPanel();
        }

        if (AdaptationRecordSystem.Instance != null && player != null && player.TryGetComponent<PlayerStat>(out var playerStat))
        {
            // 유니콘 페이즈 선택 옵션 사용 시 환생 포인트 지급하지 않음
            if (UniconManager.Instance == null || UniconManager.Instance.selectedMainScene == UniconManager.Instance.defaultMainScene)
            {
                AdaptationRecordSystem.Instance.GainPoint(curCharacter, curStage, playerStat.level);
            }
        }
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    void DisableComponents()
    {
        if (player != null)
        {
            TryDisableComponent<PlayerAction>(player);
            TryDisableComponent<PlayerAttack>(player);
            TryDisableComponent<PlayerStat>(player);
            //TryDisableComponent<PlayerLayer>(player);
            // 캐릭터 추가되면 수정 필요
            TryDisableComponent<RedRidingHood>(player);

            foreach (Transform child in player.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        if (scrolling_background != null)
        {
            foreach (Transform child in scrolling_background.transform)
            {
                TryDisableComponent<BackgroundScroll>(child.gameObject);
            }
        }
        if (maps.Length > 0)
        {
            foreach (var map in maps)
            {
                TryDisableComponent<MapScroll>(map);
                TryDisableComponent<SceneTransition>(map);
                TryDisableComponent<PhaseEndTrigger>(map);
            }
        }
        if (bulletSet != null)
        {
            bulletSet.SetActive(false);
        }
    }

    void TryDisableComponent<T>(GameObject targetObject) where T : Behaviour
    {
        if (targetObject.TryGetComponent<T>(out var component)) component.enabled = false;
    }
}
