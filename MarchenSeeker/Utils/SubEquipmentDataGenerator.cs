#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using static SubEquipmentData;

public class SubEquipmentDataGenerator : EditorWindow
{
    string csvFileName = "SubEquip_MasterTable";
    string assetFolder = "Assets/ScriptableObjects/SubEquipmentData";
    string spriteFolder = "Assets/Sprites/Equipments";
    string prefabFolder = "Assets/Prefabs/Equipments";
    EnhanceSystem enhanceSystem = null;

    [MenuItem("Window/Sub Equipment Data Generator")]
    public static void ShowWindow()
    {
        GetWindow<SubEquipmentDataGenerator>();
    }

    void OnGUI()
    {
        GUILayout.Label("Sub Equipment Data Generator", EditorStyles.boldLabel);
        csvFileName = EditorGUILayout.TextField("CSV File Name", csvFileName);
        assetFolder = EditorGUILayout.TextField("Asset Folder", assetFolder);
        spriteFolder = EditorGUILayout.TextField("Sprite Folder", spriteFolder);
        prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);

        if (GUILayout.Button("Generate"))
            GenerateSubEquipData();

        enhanceSystem = (EnhanceSystem)EditorGUILayout.ObjectField("EnhanceSystem", enhanceSystem, typeof(EnhanceSystem), true);

        if (GUILayout.Button("Assign"))
            AssignSubEquipData();
    }

    void GenerateSubEquipData()
    {
        Debug.Log("Start Generating Sub Equipment Data");

        List<Dictionary<string, object>> subEquipTable = CSVReader.Read(csvFileName);
        if (subEquipTable == null || subEquipTable.Count == 0)
        {
            Debug.LogWarning(csvFileName + " 데이터가 존재하지 않음");
            return;
        }

        Dictionary<string, string> stringDict = GetStringDict();
        if (stringDict == null || stringDict.Count == 0)
        {
            Debug.LogWarning("String Dictionary is Null");
            return;
        }

        Dictionary<string, string> evolutionDict = GetEvolutionDict();
        if (evolutionDict == null || evolutionDict.Count == 0)
        {
            Debug.LogWarning("Evolution Dictionary is Null");
            return;
        }

        // 폴더 없으면 생성
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            Directory.CreateDirectory(assetFolder);
            AssetDatabase.Refresh();
        }

        for (int i = 0; i < subEquipTable.Count; i++)
        {
            var subEquipment = subEquipTable[i];
            bool hasWarning = false;

            // SubEquipID
            string subEquipID = subEquipment["SubEquipID"].ToString();
            if (string.IsNullOrEmpty(subEquipID))
            {
                Debug.LogWarning($"{i}. " + "SubEquipID is Null or Empty");
                continue;
            }

            // NameStringID
            string nameStringID = subEquipment["NameStringID"].ToString();
            string nameString = "";
            if (string.IsNullOrEmpty(nameStringID) || !stringDict.ContainsKey(nameStringID))
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: NameStringID Not Exists In String Table");
                hasWarning = true;
            }
            else
            {
                nameString = stringDict[nameStringID];
            }

            // Category
            if (!int.TryParse(subEquipment["Category"].ToString(), out int category)
                || !Enum.IsDefined(typeof(Category), category)
                || category == (int)Category.invalid)
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: Category is Invalid");
                hasWarning = true;
            }

            // SubEquipResource
            string subEquipResourceID = subEquipment["SubEquipResource"].ToString();
            Sprite subEquipResource = null;
            if (string.IsNullOrEmpty(subEquipResourceID))
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: SubEquipResourceID is Null or Empty");
                hasWarning = true;
            }
            else
            {
                string subEquipResourcePath = $"{spriteFolder}/{subEquipResourceID}.png";
                subEquipResource = AssetDatabase.LoadAssetAtPath<Sprite>(subEquipResourcePath);
                if (subEquipResource == null)
                {
                    Debug.LogWarning($"{i}. " + $"{subEquipID}: SubEquipResource is Null");
                    hasWarning = true;
                }
            }

            // BulletResource
            string bulletResourceID = subEquipment["BulletResource"].ToString();
            Sprite bulletResource = null;
            if (!string.IsNullOrEmpty(bulletResourceID))
            {
                string bulletResourcePath = $"{spriteFolder}/Bullets/{bulletResourceID}.png";
                bulletResource = AssetDatabase.LoadAssetAtPath<Sprite>(bulletResourcePath);
                if (bulletResource == null)
                {
                    Debug.LogWarning($"{i}. " + $"{subEquipID}: BulletResource is Null");
                    hasWarning = true;
                }
            }

            // AbleStage
            if (!int.TryParse(subEquipment["AbleStage"].ToString().Replace("Stage ", ""), out int ableStage)
                || ableStage <= 0)
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: AbleStage is Invalid");
                hasWarning = true;
            }

            // Penetrate
            string penetrateString = subEquipment["Penetrate"].ToString().ToUpper();
            if (penetrateString != "TRUE" && penetrateString != "FALSE")
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: Penetrate is Invalid");
                hasWarning = true;
            }
            bool penetrate = penetrateString == "TRUE";

            // BulletCategory
            if (!int.TryParse(subEquipment["BulletCategory"].ToString(), out int bulletCategory)
                || !Enum.IsDefined(typeof(BulletCategory), bulletCategory)
                || bulletCategory == (int)BulletCategory.invalid)
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: BulletCategory is Invalid");
                hasWarning = true;
            }

            // HasAdditionalEffect
            string hasAdditionalEffectString = subEquipment["HasAdditionalEffect"].ToString().ToUpper();
            if (hasAdditionalEffectString != "TRUE" && hasAdditionalEffectString != "FALSE")
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: HasAdditionalEffect is Invalid");
                hasWarning = true;
            }
            bool hasAdditionalEffect = hasAdditionalEffectString == "TRUE";

            // IsComeBack
            string isComeBackString = subEquipment["IsComeBack"].ToString().ToUpper();
            if (isComeBackString != "TRUE" && isComeBackString != "FALSE")
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: IsComeBack is Invalid");
                hasWarning = true;
            }
            bool isComeBack = isComeBackString == "TRUE";

            // Prefab
            string prefabID = subEquipID;
            if (category == (int)Category.evolved)
            {
                if (!evolutionDict.ContainsKey(subEquipID))
                {
                    Debug.LogWarning($"{i}. " + $"{subEquipID}: EvoResultID Not Exists In Evolution Table");
                    hasWarning = true;
                }
                else
                {
                    prefabID = evolutionDict[subEquipID];
                }
            }
            GameObject prefab = null;
            if (string.IsNullOrEmpty(prefabID))
            {
                Debug.LogWarning($"{i}. " + $"{subEquipID}: PrefabID is Null or Empty");
                hasWarning = true;
            }
            else
            {
                string prefabPath = $"{prefabFolder}/{prefabID}.prefab";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null && category != (int)Category.passive)
                {
                    Debug.LogWarning($"{i}. " + $"{subEquipID}: Prefab is Null");
                    hasWarning = true;
                }
            }

            if (hasWarning)
            {
                continue;
            }

            // 에셋 생성 또는 수정
            string assetPath = $"{assetFolder}/{subEquipID}.asset";
            SubEquipmentData asset = AssetDatabase.LoadAssetAtPath<SubEquipmentData>(assetPath);

            bool isNew = asset == null;
            if (isNew)
            {
                asset = CreateInstance<SubEquipmentData>();
            }

            bool isDirty = false;
            if (asset.subEquipID != subEquipID)
            {
                asset.subEquipID = subEquipID;
                isDirty = true;
            }
            if (asset.nameString != nameString)
            {
                asset.nameString = nameString;
                isDirty = true;
            }
            if (asset.category != (Category)category)
            {
                asset.category = (Category)category;
                isDirty = true;
            }
            if (asset.subEquipResource != subEquipResource)
            {
                asset.subEquipResource = subEquipResource;
                isDirty = true;
            }
            if (asset.bulletResource != bulletResource)
            {
                asset.bulletResource = bulletResource;
                isDirty = true;
            }
            if (asset.ableStage != ableStage)
            {
                asset.ableStage = ableStage;
                isDirty = true;
            }
            if (asset.penetrate != penetrate)
            {
                asset.penetrate = penetrate;
                isDirty = true;
            }
            if (asset.bulletCategory != (BulletCategory)bulletCategory)
            {
                asset.bulletCategory = (BulletCategory)bulletCategory;
                isDirty = true;
            }
            if (asset.hasAdditionalEffect != hasAdditionalEffect)
            {
                asset.hasAdditionalEffect = hasAdditionalEffect;
                isDirty = true;
            }
            if (asset.isComeBack != isComeBack)
            {
                asset.isComeBack = isComeBack;
                isDirty = true;
            }
            if (asset.prefab != prefab)
            {
                asset.prefab = prefab;
                isDirty = true;
            }

            // 마무리 작업
            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                Debug.Log($"{i}. " + $"{subEquipID}: Newly Generated");
            }
            else if (isDirty)
            {
                EditorUtility.SetDirty(asset);
                Debug.Log($"{i}. " + $"{subEquipID}: Updated");
            }
            else
            {
                Debug.Log($"{i}. " + $"{subEquipID}: Not Changed");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Finished Generating Sub Equipment Data");

        // 테이블에 없는 에셋은 Dummy 폴더로 이동
        MoveDummyData(subEquipTable);
    }

    Dictionary<string, string> GetStringDict()
    {
        List<Dictionary<string, object>> stringTable = CSVReader.Read("StringTable");
        Dictionary<string, string> stringDict = new();

        if (stringTable == null || stringTable.Count == 0)
            return stringDict;

        foreach (var stringData in stringTable)
        {
            string key = stringData["StringID"].ToString();
            string value = stringData["String"].ToString();

            if (!stringDict.ContainsKey(key))
            {
                stringDict[key] = value;
            }
        }

        return stringDict;
    }

    Dictionary<string, string> GetEvolutionDict()
    {
        List<Dictionary<string, object>> evolutionTable = CSVReader.Read("SubEquip_EvolutionTable");
        Dictionary<string, string> evolutionDict = new();

        if (evolutionTable == null || evolutionTable.Count == 0)
            return evolutionDict;

        foreach (var evolutionData in evolutionTable)
        {
            string key = evolutionData["EvoResultID"].ToString();
            string value = evolutionData["SubEquipID"].ToString();

            if (!evolutionDict.ContainsKey(key))
            {
                evolutionDict[key] = value;
            }
        }

        return evolutionDict;
    }

    void MoveDummyData(List<Dictionary<string, object>> subEquipTable)
    {
        Debug.Log("Start Moving Dummy Data");

        // 폴더 없으면 생성
        string dummyFolder = assetFolder + "/Dummy";
        if (!AssetDatabase.IsValidFolder(dummyFolder))
        {
            AssetDatabase.CreateFolder(assetFolder, "Dummy");
        }

        // 테이블에 있는 SubEquipID 목록 생성
        HashSet<string> validIDs = new HashSet<string>();
        foreach (var subEquipment in subEquipTable)
        {
            string id = subEquipment["SubEquipID"].ToString();
            if (!string.IsNullOrEmpty(id))
            {
                validIDs.Add(id);
            }
        }

        // 테이블에 없는 에셋은 Dummy 폴더로 이동
        string[] guids = AssetDatabase.FindAssets("t:SubEquipmentData", new[] { assetFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SubEquipmentData asset = AssetDatabase.LoadAssetAtPath<SubEquipmentData>(path);
            if (asset != null && (!validIDs.Contains(asset.subEquipID) || !validIDs.Contains(asset.name)))
            {
                string fileName = Path.GetFileName(path);
                string newPath = $"{dummyFolder}/{fileName}";
                string error = AssetDatabase.MoveAsset(path, newPath);
                if (string.IsNullOrEmpty(error))
                    Debug.Log($"{asset.name}: Moved to Dummy Folder");
                else
                    Debug.LogWarning($"{asset.name}: {error}");
            }
        }

        Debug.Log("Finished Moving Dummy Data");
    }

    void AssignSubEquipData()
    {
        if (enhanceSystem == null)
        {
            Debug.LogWarning("EnhanceSystem is Null");
            return;
        }

        List<SubEquipmentData> subEquipments = new();

        string[] guids = AssetDatabase.FindAssets("t:SubEquipmentData", new[] { assetFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SubEquipmentData asset = AssetDatabase.LoadAssetAtPath<SubEquipmentData>(path);
            if (asset != null && !subEquipments.Contains(asset))
            {
                subEquipments.Add(asset);
            }
        }

        enhanceSystem.AssignSubEquipments(subEquipments);
        EditorUtility.SetDirty(enhanceSystem);
        Debug.Log("Assignment Complete");
    }
}

#endif