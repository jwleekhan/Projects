using System;
using System.Collections.Generic;

[Serializable]
public class SerializableDictionaryEntry<TKey, TValue>
{
    public TKey key;
    public TValue value;
}

[Serializable]
public class CheckPointSaveObject
{
    //누적 수치
    public int playerLevel = 1;
    public float playerExp = 0f;
    
    //캐릭터 상태
    public float health = -1;
    public float score = -1;

    //페이즈 ID
    public string phaseName = "";

    //강화 상태
    public List<SerializableDictionaryEntry<string, int>> subEquipLevelsForSave = null;

    //환생 능력치
    public AdaptationRecordSystem.AdaptationRecord adaptationRecord = null;

    //패시브
    public int selectedPassive = -1;

    //잔여 스킬 횟수 및 쿨타임
    public int skill1Count = -1;
    public int skill2Count = -1;
    public float skill3Timer = -1f;
    public float skill4Timer = -1f;

    //처치 몬스터 수, 처치 보스 정보 구현되면 추가.

    //<SubEquipmentData, int> 딕셔너리를 받아서 직렬화된 리스트로 변환하여 저장하는 함수.
    public void SetSubEquipLevelsForSave(IReadOnlyDictionary<SubEquipmentData, int> subEquipLevels)
    {
        try
        {
            subEquipLevelsForSave.Clear();
        }
        catch (NullReferenceException)
        {
            subEquipLevelsForSave = new List<SerializableDictionaryEntry<string, int>>();
        }

        foreach (var pair in subEquipLevels)
        {
            if (pair.Value > 0)
            {
                subEquipLevelsForSave.Add(new SerializableDictionaryEntry<string, int> { key = pair.Key.subEquipID, value = pair.Value});
            }
        }
    }

    //직렬화된 리스트를 일반 <string, int> 딕셔너리로 변환하는 함수. 데이터를 불러올 때 사용.
    public Dictionary<string, int> GetSubEquipLevelsFromSave() {
        Dictionary<string, int> result = new Dictionary<string, int>();
        foreach (var entry in subEquipLevelsForSave)
        {
            result[entry.key] = entry.value;
        }
        return result;
    }
}

[Serializable]
public class AdaptationSaveObject
{
    public Character character;
    public Stage stage;

    public int remainingPoint;
    public int maxHp;
    public int startLevel;
    public int mainEquipDamage;
    public int subEquipDamage;
    public int skillDamage;
    public int returnExpRate;

    public AdaptationSaveObject(Character Character, Stage Stage, AdaptationRecordSystem.AdaptationRecord Record)
    {
        character = Character;
        stage = Stage;
        remainingPoint = Record.remainingPoint;
        maxHp = Record.maxHp;
        startLevel = Record.startLevel;
        mainEquipDamage = Record.mainEquipDamage;
        subEquipDamage = Record.subEquipDamage;
        skillDamage = Record.skillDamage;
        returnExpRate = Record.returnExpRate;
    }
}
