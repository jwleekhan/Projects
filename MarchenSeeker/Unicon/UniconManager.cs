using UnityEngine;
using UnityEngine.SceneManagement;

public class UniconManager : MonoBehaviour
{
    // 유니콘을 위한 페이즈 선택 기능

    public static UniconManager Instance { get; private set; }

    public string defaultMainScene;
    public string selectedMainScene;

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

        defaultMainScene = GameManager.Instance.mainScene;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Lobby"))
        {
            GameManager.Instance.mainScene = defaultMainScene;
            selectedMainScene = defaultMainScene;
            return;
        }
    }

    public void SelectScene(int sceneNumber)
    {
        switch (sceneNumber)
        {
            case 1:
                selectedMainScene = "Phase1";
                break;
            case 2:
                selectedMainScene = "Phase2";
                break;
            case 3:
                selectedMainScene = "ViperMidBossScene";
                break;
            case 4:
                selectedMainScene = "Phase4";
                break;
            case 5:
                selectedMainScene = "Phase5";
                break;
            case 6:
                selectedMainScene = "GhostMidBossScene";
                break;
            case 7:
                selectedMainScene = "Phase7";
                break;
            case 8:
                selectedMainScene = "Phase8";
                break;
            case 9:
                selectedMainScene = "WolfFinalBossScene";
                break;
            default:
                selectedMainScene = defaultMainScene;
                break;
        }
        GameManager.Instance.mainScene = selectedMainScene;
    }

    public bool IsUniconStart()
    {
        return selectedMainScene != defaultMainScene;
    }
}
