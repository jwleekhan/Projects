using System;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionSystem : MonoBehaviour
{
    public struct EvolutionData
    {
        public string evoResultID { get; }
        public string subEquipID { get; }
        public int requiredLevel { get; }
        public string requiredOtherSubEquipID { get; }
        public int requiredOtherLevel { get; }

        public EvolutionData(
            string EvoResultID,
            string SubEquipID,
            int RequiredLevel,
            string RequiredOtherSubEquipID,
            int RequiredOtherLevel)
        {
            evoResultID = EvoResultID;
            subEquipID = SubEquipID;
            requiredLevel = RequiredLevel;
            requiredOtherSubEquipID = RequiredOtherSubEquipID;
            requiredOtherLevel = RequiredOtherLevel;
        }
    }

    List<EvolutionData> evolutionTable = new();
    List<EvolutionData> evolutionTargets = new();

    EnhanceSystem enhanceSystem;
    EvolutionUI evolutionUI;

    void Start()
    {
        enhanceSystem = EnhanceSystem.Instance;
        LoadEvolutionTable();
    }

    void LoadEvolutionTable()
    {
        List<Dictionary<string, object>> subEquipEvolutionTable = CSVReader.Read("SubEquip_EvolutionTable");
        foreach (var evolutionData in subEquipEvolutionTable)
        {
            evolutionTable.Add(new EvolutionData(
                (string)evolutionData["EvoResultID"],
                (string)evolutionData["SubEquipID"],
                (int)evolutionData["RequiredLevel"],
                (string)evolutionData["RequiredOtherSubEquipID"],
                (int)evolutionData["RequiredOtherLevel"]));
        }
    }

    public void SetEvolutionUI()
    {
        evolutionUI = FindAnyObjectByType<EvolutionUI>();
    }

    public void ResetEvolutionSystem()
    {
        evolutionTargets.Clear();
    }

    public void OnEnhanceApplied(SubEquipmentData enhancedSubEquip)
    {
        foreach (var evolutionData in evolutionTable)
        {
            // 강화된 장비가 진화 기반 장비이거나 조건 장비인 경우
            if (evolutionData.subEquipID == enhancedSubEquip.subEquipID || evolutionData.requiredOtherSubEquipID == enhancedSubEquip.subEquipID)
            {
                // 진화 기반 장비와 조건 장비의 레벨 충족 여부 체크
                if (enhanceSystem.GetSubEquipLevel(evolutionData.subEquipID, true) >= evolutionData.requiredLevel
                    && enhanceSystem.GetSubEquipLevel(evolutionData.requiredOtherSubEquipID, false) >= evolutionData.requiredOtherLevel)
                {
                    // evolutionTargets에 추가
                    if (!evolutionTargets.Contains(evolutionData) && enhanceSystem.GetSubEquipDataFromID(evolutionData.evoResultID) != null)
                    {
                        evolutionTargets.Add(evolutionData);
                    }
                }
                else
                {
                    // evolutionTargets에서 제거 (장비 레벨이 낮아지는 경우가 생기면 필요한 부분)
                    if (evolutionTargets.Contains(evolutionData))
                    {
                        evolutionTargets.Remove(evolutionData);
                    }
                }
            }
        }

        if (evolutionTargets.Count > 0)
        {
            if (evolutionUI != null)
            {
                evolutionUI.ShowEvolutionOpenButton();
            }
        }
        else
        {
            if (evolutionUI != null)
            {
                evolutionUI.HideEvolutionOpenButton();
            }
        }
    }

    public IReadOnlyList<EvolutionData> GetEvolutionTargets()
    {
        return evolutionTargets.AsReadOnly();
    }

    public void ApplyEvolution(EvolutionData evolutionData)
    {
        SubEquipmentData baseSubEquip = enhanceSystem.GetSubEquipDataFromID(evolutionData.subEquipID);
        SubEquipmentData resultSubEquip = enhanceSystem.GetSubEquipDataFromID(evolutionData.evoResultID);
        EnhanceSystem.Instance.ApplyEvolution(baseSubEquip, resultSubEquip);
        evolutionTargets.RemoveAll(target => target.subEquipID == evolutionData.subEquipID);
    }
}
