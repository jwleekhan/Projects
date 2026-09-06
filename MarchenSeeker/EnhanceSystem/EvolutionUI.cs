using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EvolutionSystem;

public class EvolutionUI : MonoBehaviour
{
    [SerializeField] Button evolutionOpenButton;
    [SerializeField] Button evolutionCloseButton;
    [SerializeField] GameObject evolutionPanel;
    [SerializeField] GameObject[] choiceGroups;
    [SerializeField] Button leftPageButton;
    [SerializeField] Button rightPageButton;

    int _curPage;
    // 기본값 0, 페이지 활성화 시 1부터 시작
    // curPage 값 할당 시 자동으로 ShowChoices 함수 호출함
    [SerializeField] int curPage
    {
        get => _curPage;
        set
        {
            if (_curPage != value && value >= 0)
            {
                _curPage = value;
                SetActivePageButtons();
                if (_curPage > 0)
                {
                    ShowChoices();
                }
            }
        }
    }
    IReadOnlyList<EvolutionData> evolutionTargets;
    List<EvolutionData> displayedEquips = new();

    EvolutionSystem evolutionSystem;

    void Start()
    {
        evolutionSystem = EnhanceSystem.Instance.GetComponent<EvolutionSystem>();
        HideEvolutionOpenButton();
        HideEvolutionPanel();
    }

    public void ShowEvolutionOpenButton()
    {
        evolutionOpenButton.gameObject.SetActive(true);
    }

    public void HideEvolutionOpenButton()
    {
        evolutionOpenButton.gameObject.SetActive(false);
    }

    public void ShowEvolutionPanel()
    {
        Time.timeScale = 0f;
        evolutionTargets = evolutionSystem.GetEvolutionTargets();
        curPage = 1;
        evolutionPanel.SetActive(true);
    }

    public void HideEvolutionPanel()
    {
        Time.timeScale = 1f;
        curPage = 0;
        evolutionPanel.SetActive(false);
    }

    void SetActiveChoiceGroups(int count)
    {
        for (int i = 0; i < choiceGroups.Length; i++)
        {
            choiceGroups[i].SetActive(i < count);
        }
    }

    void SetActivePageButtons()
    {
        int maxPage = 0;
        if (evolutionTargets != null)
        {
            maxPage = (evolutionTargets.Count + 1) / 2;
        }
        leftPageButton.gameObject.SetActive(curPage > 1);
        rightPageButton.gameObject.SetActive(curPage < maxPage);
    }

    void ShowChoices()
    {
        SetActiveChoiceGroups(0);
        displayedEquips = new();

        if (evolutionTargets == null)
        {
            return;
        }

        for (int i = (curPage - 1) * 2; i < evolutionTargets.Count && displayedEquips.Count < 2; i++)
        {
            displayedEquips.Add(evolutionTargets[i]);
        }

        // 임시
        for (int i = 0; i < displayedEquips.Count && i < choiceGroups.Length; i++)
        {
            SubEquipmentData resultSubEquip = EnhanceSystem.Instance.GetSubEquipDataFromID(displayedEquips[i].evoResultID);
            Dictionary<string, object> levelData = EnhanceSystem.Instance.GetLevelData(resultSubEquip, displayedEquips[i].requiredLevel);

            if (choiceGroups[i].transform.Find("IconBackGround/IconImage") is Transform iconImage)
            {
                iconImage.GetComponent<Image>().sprite = resultSubEquip.subEquipResource;
            }
            if (choiceGroups[i].transform.Find("ChoiceButton/NamePanel/NameText") is Transform nameText)
            {
                nameText.GetComponent<TextMeshProUGUI>().text = resultSubEquip.nameString;
            }
            if (choiceGroups[i].transform.Find("ChoiceButton/DescText") is Transform descText)
            {
                descText.GetComponent<TextMeshProUGUI>().text = (string)levelData["DescStringID"];
            }
        }

        SetActiveChoiceGroups(displayedEquips.Count);
    }

    public void GoPrevPage()
    {
        curPage--;
    }

    public void GoNextPage()
    {
        curPage++;
    }

    public void OnChoiceButtonClicked(int index)
    {
        if (evolutionSystem != null)
        {
            if (index >= 0 && displayedEquips != null && index < displayedEquips.Count)
            {
                evolutionSystem.ApplyEvolution(displayedEquips[index]);
            }
        }

        HideEvolutionPanel();
        if (evolutionTargets.Count == 0)
        {
            HideEvolutionOpenButton();
        }
    }
}
