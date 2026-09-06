using System;
using System.Collections.Generic;
using UnityEngine;

public class AdaptationRecordSystem : MonoBehaviour
{
    public static AdaptationRecordSystem Instance { get; private set; }

    public enum AdaptationStat { MaxHp, StartLevel, MainEquipDamage, SubEquipDamage, SkillDamage, ReturnExpRate }

    [Serializable]
    public class AdaptationRecord
    {
        public static readonly int[] maxLevels = { 5, 4, 3, 3, 5, 5 };

        // Points
        public int remainingPoint;

        // Levels
        [SerializeField] private int _maxHp;
        [SerializeField] private int _startLevel;
        [SerializeField] private int _mainEquipDamage;
        [SerializeField] private int _subEquipDamage;
        [SerializeField] private int _skillDamage;
        [SerializeField] private int _returnExpRate;
        public int maxHp { get => _maxHp; set => _maxHp = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.MaxHp]); }
        public int startLevel { get => _startLevel; set => _startLevel = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.StartLevel]); }
        public int mainEquipDamage { get => _mainEquipDamage; set => _mainEquipDamage = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.MainEquipDamage]); }
        public int subEquipDamage { get => _subEquipDamage; set => _subEquipDamage = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.SubEquipDamage]); }
        public int skillDamage { get => _skillDamage; set => _skillDamage = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.SkillDamage]); }
        public int returnExpRate { get => _returnExpRate; set => _returnExpRate = Mathf.Clamp(value, 0, maxLevels[(int)AdaptationStat.ReturnExpRate]); }

        public int this[AdaptationStat stat]
        {
            get
            {
                return stat switch
                {
                    AdaptationStat.MaxHp => maxHp,
                    AdaptationStat.StartLevel => startLevel,
                    AdaptationStat.MainEquipDamage => mainEquipDamage,
                    AdaptationStat.SubEquipDamage => subEquipDamage,
                    AdaptationStat.SkillDamage => skillDamage,
                    AdaptationStat.ReturnExpRate => returnExpRate,
                    _ => 0
                };
            }
            set
            {
                switch (stat)
                {
                    case AdaptationStat.MaxHp:
                        maxHp = value;
                        break;
                    case AdaptationStat.StartLevel:
                        startLevel = value;
                        break;
                    case AdaptationStat.MainEquipDamage:
                        mainEquipDamage = value;
                        break;
                    case AdaptationStat.SubEquipDamage:
                        subEquipDamage = value;
                        break;
                    case AdaptationStat.SkillDamage:
                        skillDamage = value;
                        break;
                    case AdaptationStat.ReturnExpRate:
                        returnExpRate = value;
                        break;
                    default:
                        break;
                }
            }
        }

        public int this[int index]
        {
            get => this[(AdaptationStat)index];
            set => this[(AdaptationStat)index] = value;
        }

        public AdaptationRecord Clone()
        {
            AdaptationRecord clone = new AdaptationRecord();
            clone.remainingPoint = remainingPoint;
            foreach (AdaptationStat stat in Enum.GetValues(typeof(AdaptationStat)))
            {
                clone[stat] = this[stat];
            }
            return clone;
        }

        public void Apply(AdaptationRecord clone)
        {
            remainingPoint = clone.remainingPoint;
            foreach (AdaptationStat stat in Enum.GetValues(typeof(AdaptationStat)))
            {
                this[stat] = clone[stat];
            }
        }
    }

    Dictionary<(Character, Stage), AdaptationRecord> adaptationRecords = new();

    Dictionary<AdaptationStat, float[]> statAmountTable = new Dictionary<AdaptationStat, float[]>
    {
        { AdaptationStat.MaxHp, new float[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f } },
        { AdaptationStat.StartLevel, new float[] { 1, 2, 3, 4, 5 } },
        { AdaptationStat.MainEquipDamage, new float[] { 0f, 0.05f, 0.1f, 0.15f } },
        { AdaptationStat.SubEquipDamage, new float[] { 0f, 0.05f, 0.1f, 0.15f } },
        { AdaptationStat.SkillDamage, new float[] { 0f, 0.05f, 0.1f, 0.15f, 0.2f, 0.25f } },
        { AdaptationStat.ReturnExpRate, new float[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f } },
    };

    Dictionary<AdaptationStat, int[]> statCostTable = new Dictionary<AdaptationStat, int[]>
    {
        { AdaptationStat.MaxHp, new int[] { 2, 4, 6, 9, 12, 0 } },
        { AdaptationStat.StartLevel, new int[] { 1, 2, 3, 4, 0 } },
        { AdaptationStat.MainEquipDamage, new int[] { 3, 5, 7, 0 } },
        { AdaptationStat.SubEquipDamage, new int[] { 3, 5, 7, 0 } },
        { AdaptationStat.SkillDamage, new int[] { 2, 3, 4, 5, 6, 0 } },
        { AdaptationStat.ReturnExpRate, new int[] { 3, 4, 5, 6, 8, 0 } },
    };

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
    }

    void Start()
    {
        InitRecords();
    }

    void InitRecords()
    {
        foreach (Character character in Enum.GetValues(typeof(Character)))
        {
            foreach (Stage stage in Enum.GetValues(typeof(Stage)))
            {
                if (SaveManager.LoadAdaptation(character, stage) is AdaptationRecord record)
                {
                    adaptationRecords[(character, stage)] = record;
                }
                else
                {
                    adaptationRecords[(character, stage)] = new AdaptationRecord();
                }

                // 유니콘에서는 기본적으로 Max Point를 제공
                AdaptationRecord curRecord = adaptationRecords[(character, stage)];
                curRecord.remainingPoint = GetRequiredPointFromCurToMax(curRecord);
            }
        }
    }

    public void GainPoint(Character character, Stage stage, int playerLevel)
    {
        Debug.Log(character);
        Debug.Log(stage);
        Debug.Log(Mathf.RoundToInt(playerLevel / 10f));

        AdaptationRecord record = GetRecord(character, stage);
        int point = Mathf.RoundToInt(playerLevel / 10f);
        record.remainingPoint += Mathf.Clamp(point, 0, GetRequiredPointFromCurToMax(record)); // 포인트 상한
        SaveManager.SaveAdaptation(character, stage, record);
    }

    public AdaptationRecord GetRecord(Character character, Stage stage)
    {
        return adaptationRecords[(character, stage)];
    }

    public float GetStatAmount(AdaptationStat stat, int level)
    {
        return statAmountTable[stat][level];
    }

    public int GetStatCost(AdaptationStat stat, int level)
    {
        return statCostTable[stat][level];
    }

    int GetRequiredPointFromCurToMax(AdaptationRecord record)
    {
        int point = 0;
        for (int stat = 0; stat < 6; stat++)
        {
            for (int level = record[stat]; level < AdaptationRecord.maxLevels[stat]; level++)
            {
                point += GetStatCost((AdaptationStat)stat, level);
            }
        }
        return point;
    }
}
