using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public static event Action playerReady;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject playerInstance;

    [SerializeField] GameObject playerSkill1;

    public float healAfterEvent = 100f;
    public float healAfterBoss = 200f;

    public bool playerActionable {  get; private set; }

    void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerActionable = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        if (scene.name.Contains("Lobby"))
        {
            Debug.Log("LobbyScene loaded, destroying player instance.");
            DestroyPlayer();
        }
        else
        {
            InstantiatePlayerIfNull();
            playerInstance.transform.position = playerPrefab.transform.position;
            playerInstance.layer = playerPrefab.layer;
            if (playerInstance.TryGetComponent<PlayerAction>(out var playerAction)) playerAction.InitFieldCollection();
            if (playerInstance.TryGetComponent<PlayerStat>(out var playerStat))
            {
                playerStat.FindBulletSet();
                playerStat.SetTimeHpDownActive(true);
            }

            // 씬별 처리
            if (scene.name.Contains("Event"))
            {
                Debug.Log("Event scene loaded, adjusting player for event.");
                SetForEventScene(true);
            }

            if (scene.name.Contains("Viper"))
            {
                SetForViperScene(true);
            }
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        // 씬별 처리
        if (scene.name.Contains("Event"))
        {
            if (playerInstance.TryGetComponent<PlayerStat>(out var playerStat)) playerStat.heal(healAfterEvent);
            SetForEventScene(false);
        }

        // GameManager.OnBossClear로 이관
        //if (scene.name.Contains("Boss"))
        //{
        //    if (playerInstance.TryGetComponent<PlayerStat>(out var playerStat)) playerStat.heal(healAfterBoss);
        //}

        if (scene.name.Contains("Viper"))
        {
            SetForViperScene(false);
        }

        // 오래된 붕대
        if (scene.name.Contains("Phase") || scene.name.Contains("BossScene"))
        {
            if (OldBandage.isAble && playerInstance.TryGetComponent<PlayerStat>(out var playerStat))
            {
                playerStat.heal(playerStat.playerMaxHp * OldBandage.healRatio);
            }
        }
    }

    void SetForEventScene(bool isLoaded)
    {
        playerActionable = !isLoaded;
        if (playerInstance.TryGetComponent<PlayerStat>(out var playerStat)) playerStat.enabled = !isLoaded;
        // 캐릭터 추가되면 수정 필요
        if (playerInstance.TryGetComponent<RedRidingHood>(out var redRidingHood)) redRidingHood.enabled = !isLoaded;

        foreach (Transform child in playerInstance.transform)
        {
            child.gameObject.SetActive(!isLoaded);
        }
    }

    void SetForViperScene(bool isLoaded)
    {
        if (playerInstance.TryGetComponent<PlayerLayer>(out var playerLayer)) playerLayer.enabled = !isLoaded;
    }

    void InstantiatePlayerIfNull()
    {
        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerPrefab);
            playerInstance.name = playerPrefab.name;
            Instantiate(playerSkill1, playerInstance.transform);    // 임시 구현. 추가 레벨 만들어지면 추가 작업 필요
            DontDestroyOnLoad(playerInstance);
            playerReady.Invoke();
        }
    }

    public void DestroyPlayer()
    {
        if (playerInstance != null)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }
    }

    public GameObject GetPlayer()
    {
        return playerInstance;
    }
}
