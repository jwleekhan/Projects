using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private CheckPointSaveObject checkPointSaveObject;
    public static string directory = "SaveData";
    public static string checkPointFileName = "CheckPoint.json";

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

    }

    private void Start()
    {
        // AdaptationSaveObject는 AdaptationRecordSystem에서 직접 불러옴
        checkPointSaveObject = LoadCheckPoint();
        if (checkPointSaveObject != null)
        {
            Debug.Log("Save loaded successfully: " + GetCheckPointPath());
        }
        else
        {
            Debug.Log("No save file found.");
            checkPointSaveObject = null;
        }
    }

    public void SaveCheckPoint(CheckPointSaveObject saveObject)
    {
        if (UniconManager.Instance != null && UniconManager.Instance.IsUniconStart())
            return;

        checkPointSaveObject = saveObject;

        if (!DirectoryExists())
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory);
        }
        
        string json = JsonUtility.ToJson(saveObject);
        File.WriteAllText(GetCheckPointPath(), json);
        Debug.Log("Save complete: " + GetCheckPointPath());
    }

    public static void SaveAdaptation(Character character, Stage stage, AdaptationRecordSystem.AdaptationRecord record)
    {
        if (!DirectoryExists())
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/" + directory);
        }

        AdaptationSaveObject saveObject = new AdaptationSaveObject(character, stage, record);
        string json = JsonUtility.ToJson(saveObject);
        string path = GetDirectoryPath() + "/" + $"AdaptationRecord_{character}_{stage}.json";
        File.WriteAllText(path, json);
        Debug.Log("Save complete: " + path);
    }

    public CheckPointSaveObject LoadCheckPoint() {
        if (SaveFileExists(GetCheckPointPath()))
        {
            string json = File.ReadAllText(GetCheckPointPath());
            CheckPointSaveObject saveObject = JsonUtility.FromJson<CheckPointSaveObject>(json);
            return saveObject;
        }
        else
        {
            return null;
        }
    }

    public static AdaptationRecordSystem.AdaptationRecord LoadAdaptation(Character character, Stage stage)
    {
        string path = GetDirectoryPath() + "/" + $"AdaptationRecord_{character}_{stage}.json";
        if (SaveFileExists(path))
        {
            string json = File.ReadAllText(path);
            AdaptationSaveObject saveObject = JsonUtility.FromJson<AdaptationSaveObject>(json);
            var record = new AdaptationRecordSystem.AdaptationRecord
            {
                remainingPoint = saveObject.remainingPoint,
                maxHp = saveObject.maxHp,
                startLevel = saveObject.startLevel,
                mainEquipDamage = saveObject.mainEquipDamage,
                subEquipDamage = saveObject.subEquipDamage,
                skillDamage = saveObject.skillDamage,
                returnExpRate = saveObject.returnExpRate
            };
            return record;
        }
        else
        {
            return null;
        }
    }

    public void DestroyCheckPoint()
    {
        if (UniconManager.Instance != null && UniconManager.Instance.IsUniconStart())
            return;

        checkPointSaveObject = null;

        if (File.Exists(GetCheckPointPath()))
        {
            File.Delete(GetCheckPointPath());
            Debug.Log("Save file deleted: " + GetCheckPointPath());
        }
        else
        {
            Debug.LogWarning("No save file to delete at: " + GetCheckPointPath());
        }
    }

    private static bool SaveFileExists(string path) 
    {
        return File.Exists(path);
    }

    private static bool DirectoryExists() { 
        return Directory.Exists(Application.persistentDataPath + "/" + directory);
    }

    private static string GetDirectoryPath() { 
        return Application.persistentDataPath + "/" + directory;
    }

    private static string GetCheckPointPath() { 
        return Application.persistentDataPath + "/" + directory + "/" + checkPointFileName;
    }

    public bool HasCheckPoint()
    {
        return GetCheckPoint() != null;
    }

    public CheckPointSaveObject GetCheckPoint()
    {
        if (UniconManager.Instance != null && UniconManager.Instance.IsUniconStart())
            return null;
        return checkPointSaveObject;
    }
}
