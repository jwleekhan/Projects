using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AdaptationRecordSystem;

public class AdaptationRecordUI : MonoBehaviour
{
    [SerializeField] Character selectedCharacter = Character.RedRidingHood;
    [SerializeField] Stage selectedStage = Stage.WolfForest;
    [SerializeField] AdaptationRecord selectedRecord;
    [SerializeField] AdaptationRecord tempRecord;
    [SerializeField] bool isChanged;

    [SerializeField] GameObject adaptationRecordPanel;
    [SerializeField] TextMeshProUGUI characterNameText;
    [SerializeField] TextMeshProUGUI stageNameText;
    [SerializeField] TextMeshProUGUI remainingPointText;
    [SerializeField] GameObject[] statGroups;
    [SerializeField] Button applyButton;
    [SerializeField] Button cancelButton;
    [SerializeField] GameObject applyConfirmPanel;
    [SerializeField] TextMeshProUGUI applyConfirmText;

    AdaptationRecordSystem adaptationRecordSystem;

    void Start()
    {
        adaptationRecordSystem = AdaptationRecordSystem.Instance;
        HideRecordPanel();
    }

    void RefreshUI()
    {
        applyConfirmPanel.SetActive(false);

        characterNameText.text = GameManager.characterNames[(int)selectedCharacter];
        stageNameText.text = GameManager.stageNames[(int)selectedStage];
        remainingPointText.text = tempRecord.remainingPoint.ToString();

        for (int stat = 0; stat < statGroups.Length; stat++)
        {
            if (statGroups[stat].transform.Find("DescText") is Transform descText)
            {
                bool isPercent = (AdaptationStat)stat != AdaptationStat.StartLevel;
                float curStatAmount = adaptationRecordSystem.GetStatAmount((AdaptationStat)stat, tempRecord[stat]);
                if (tempRecord[stat] < AdaptationRecord.maxLevels[stat])
                {
                    float nextStatAmount = adaptationRecordSystem.GetStatAmount((AdaptationStat)stat, tempRecord[stat] + 1);
                    if (isPercent)
                        descText.GetComponent<TextMeshProUGUI>().text = $"+{curStatAmount * 100}%" + " → " + $"+{nextStatAmount * 100}%";
                    else
                        descText.GetComponent<TextMeshProUGUI>().text = curStatAmount + " → " + nextStatAmount;
                }
                else
                {
                    if (isPercent)
                        descText.GetComponent<TextMeshProUGUI>().text = $"+{curStatAmount * 100}%";
                    else
                        descText.GetComponent<TextMeshProUGUI>().text = curStatAmount.ToString();

                }
            }
            if (statGroups[stat].transform.Find("PointPanel/PointText") is Transform pointText)
            {
                pointText.GetComponent<TextMeshProUGUI>().text = adaptationRecordSystem.GetStatCost((AdaptationStat)stat, tempRecord[stat]).ToString();
            }
            if (statGroups[stat].transform.Find("IncreaseButton") is Transform increaseButton)
            {
                increaseButton.GetComponent<Button>().interactable = tempRecord[stat] < AdaptationRecord.maxLevels[stat] && adaptationRecordSystem.GetStatCost((AdaptationStat)stat, tempRecord[stat]) <= tempRecord.remainingPoint;
            }
            if (statGroups[stat].transform.Find("DecreaseButton") is Transform decreaseButton)
            {
                decreaseButton.GetComponent<Button>().interactable = tempRecord[stat] > 0;
            }
        }

        applyButton.interactable = isChanged;
        cancelButton.interactable = isChanged;
    }

    public void ShowRecordPanel()
    {
        if (adaptationRecordSystem == null)
            return;
        selectedRecord = adaptationRecordSystem.GetRecord(selectedCharacter, selectedStage);
        tempRecord = selectedRecord.Clone();
        isChanged = false;
        RefreshUI();
        adaptationRecordPanel.SetActive(true);
    }

    public void HideRecordPanel()
    {
        adaptationRecordPanel.SetActive(false);
    }

    public void OnIncreaseButtonClicked(int stat)
    {
        tempRecord.remainingPoint -= adaptationRecordSystem.GetStatCost((AdaptationStat)stat, tempRecord[stat]);
        tempRecord[stat]++;
        isChanged = true;
        RefreshUI();
    }

    public void OnDecreaseButtonClicked(int stat)
    {
        tempRecord[stat]--;
        tempRecord.remainingPoint += adaptationRecordSystem.GetStatCost((AdaptationStat)stat, tempRecord[stat]);
        isChanged = true;
        RefreshUI();
    }

    public void OnApplyButtonClicked()
    {
        string characterName = GameManager.characterNames[(int)selectedCharacter];
        string stageName = GameManager.stageNames[(int)selectedStage];
        applyConfirmText.text = $"{stageName}에 대한 적응의 기록을 작성해,\n{characterName}의 힘을 새롭게 구성합니다.";
        applyConfirmPanel.SetActive(true);
    }

    public void OnCancelButtonClicked()
    {
        tempRecord = selectedRecord.Clone();
        isChanged = false;
        RefreshUI();
    }

    public void OnApplyConfirmButtonClicked()
    {
        selectedRecord.Apply(tempRecord);
        isChanged = false;
        RefreshUI();
        SaveManager.SaveAdaptation(selectedCharacter, selectedStage, selectedRecord);
    }

    public void OnApplyCancelButtonClicked()
    {
        applyConfirmPanel.SetActive(false);
    }
}
