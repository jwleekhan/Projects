using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnhanceUI : MonoBehaviour
{
    enum ColorCategory { normal, special, passive, evolvable }

    [SerializeField] GameObject enhancePanel;
    [SerializeField] Button[] choiceButtons;
    [SerializeField] Sprite[] buttonSprites = { null, null, null, null }; // index <-> ColorCategory
    [SerializeField] Color[] buttonColors = { Color.white, Color.white, Color.white, Color.white }; // index <-> ColorCategory
    [SerializeField] Button returnExpButton;
    [SerializeField] GameObject dontTouchPanel;
    List<SubEquipmentData> displayedEquips;

    EnhanceSystem enhanceSystem;
    PlayerStat playerStat;

    void Start()
    {
        enhanceSystem = EnhanceSystem.Instance;
        GameObject player = PlayerManager.Instance.GetPlayer();
        if (player != null && player.TryGetComponent<PlayerStat>(out var stat))
            playerStat = stat;
        HideEnhanceUI();
    }
    public void ShowEnhanceUI(List<SubEquipmentData> choices, List<Dictionary<string, object>> nextLevelData)
    {
        dontTouchPanel.SetActive(true);
        StartCoroutine(AllowTouch());

        displayedEquips = choices;
        for (int i = 0; i < choices.Count; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);

            // Set Button Color
            if (choices[i].category == SubEquipmentData.Category.passive)
            {
                choiceButtons[i].transform.Find("BookBody").GetComponent<Image>().sprite = buttonSprites[0];
            }
            else if (choices[i].category == SubEquipmentData.Category.special)
            {
                choiceButtons[i].transform.Find("BookBody").GetComponent<Image>().sprite = buttonSprites[1];
            }
            else if (choices[i].category == SubEquipmentData.Category.normal && (int)nextLevelData[i]["SubEquipLevel"] == enhanceSystem.GetMaxLevel(choices[i]))
            {
                choiceButtons[i].transform.Find("BookBody").GetComponent<Image>().sprite = buttonSprites[2];
            }
            else
            {
                choiceButtons[i].transform.Find("BookBody").GetComponent<Image>().sprite = buttonSprites[3];
            }

            // Set Contents
            if (choiceButtons[i].transform.Find("BookBody/ItemImage") is Transform iconImage)
            {
                iconImage.GetComponent<Image>().sprite = choices[i].subEquipResource;
            }
            if (choiceButtons[i].transform.Find("BookBody/ItemLevel") is Transform levelText)
            {
                int nextLevel = (int)nextLevelData[i]["SubEquipLevel"];
                levelText.GetComponent<TextMeshProUGUI>().text = "Lv." + (nextLevel - 1) + "→" + nextLevel;
            }
            if (choiceButtons[i].transform.Find("BookBody/ItemName") is Transform nameText)
            {
                nameText.GetComponent<TextMeshProUGUI>().text = choices[i].nameString;
            }
            if (choiceButtons[i].transform.Find("BookBody/ItemDescription") is Transform descText)
            {
                descText.GetComponent<TextMeshProUGUI>().text = (string)nextLevelData[i]["DescStringID"];
            }

            choiceButtons[i].gameObject.SetActive(true);
        }
        //public void ShowEnhanceUI(List<SubEquipmentData> choices, List<Dictionary<string, object>> nextLevelData)
        //{
        //    dontTouchPanel.SetActive(true);
        //    StartCoroutine(AllowTouch());

        //    displayedEquips = choices;
        //    for (int i = 0; i < choices.Count; i++)
        //    {
        //        choiceButtons[i].gameObject.SetActive(false);

        //        // Set Button Color
        //        if (choices[i].category == SubEquipmentData.Category.passive)
        //        {
        //            choiceButtons[i].GetComponent<Image>().color = buttonColors[(int)ColorCategory.passive];
        //        }
        //        else if (choices[i].category == SubEquipmentData.Category.special)
        //        {
        //            choiceButtons[i].GetComponent<Image>().color = buttonColors[(int)ColorCategory.special];
        //        }
        //        else if (choices[i].category == SubEquipmentData.Category.normal && (int)nextLevelData[i]["SubEquipLevel"] == enhanceSystem.GetMaxLevel(choices[i]))
        //        {
        //            choiceButtons[i].GetComponent<Image>().color = buttonColors[(int)ColorCategory.evolvable];
        //        }
        //        else
        //        {
        //            choiceButtons[i].GetComponent<Image>().color = buttonColors[(int)ColorCategory.normal];
        //        }

        //        // Set Contents
        //        if (choiceButtons[i].transform.Find("IconBackGround/IconImage") is Transform iconImage)
        //        {
        //            iconImage.GetComponent<Image>().sprite = choices[i].subEquipResource;
        //        }
        //        if (choiceButtons[i].transform.Find("IconBackGround/LevelText") is Transform levelText)
        //        {
        //            int nextLevel = (int)nextLevelData[i]["SubEquipLevel"];
        //            levelText.GetComponent<TextMeshProUGUI>().text = "Lv." + (nextLevel - 1) + "→" + nextLevel;
        //        }
        //        if (choiceButtons[i].transform.Find("DescPanel/NamePanel/NameText") is Transform nameText)
        //        {
        //            nameText.GetComponent<TextMeshProUGUI>().text = choices[i].nameString;
        //        }
        //        if (choiceButtons[i].transform.Find("DescPanel/DescText") is Transform descText)
        //        {
        //            descText.GetComponent<TextMeshProUGUI>().text = (string)nextLevelData[i]["DescStringID"];
        //        }

        //        choiceButtons[i].gameObject.SetActive(true);
        //    }

        // 빈칸 채우기
        for (int i = choices.Count; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);

            //choiceButtons[i].GetComponent<Image>().color = buttonColors[(int)ColorCategory.normal];

            //if (choiceButtons[i].transform.Find("IconBackGround/IconImage") is Transform iconImage)
            //{
            //    iconImage.GetComponent<Image>().sprite = null;
            //}
            //if (choiceButtons[i].transform.Find("IconBackGround/LevelText") is Transform levelText)
            //{
            //    levelText.GetComponent<TextMeshProUGUI>().text = "";
            //}
            //if (choiceButtons[i].transform.Find("DescPanel/NamePanel/NameText") is Transform nameText)
            //{
            //    nameText.GetComponent<TextMeshProUGUI>().text = "";
            //}
            //if (choiceButtons[i].transform.Find("DescPanel/DescText") is Transform descText)
            //{
            //    descText.GetComponent<TextMeshProUGUI>().text = "";
            //}
        }

        // 경험치 환수 버튼
        if (returnExpButton.transform.Find("ReturnExpText") is Transform returnExpText && playerStat != null)
        {
            returnExpText.GetComponent<TextMeshProUGUI>().text = "경험치 환수 (" + Mathf.RoundToInt(playerStat.returnExpRate * 100) + "%)";
            returnExpButton.gameObject.SetActive(true);
        }
        else
        {
            returnExpButton.gameObject.SetActive(false);
        }

        enhancePanel.SetActive(true);
    }

    IEnumerator AllowTouch()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        dontTouchPanel.SetActive(false);
    }

    public void OnChoiceButtonClicked(int index)
    {
        if (enhanceSystem != null)
        {
            if (index >= 0 && displayedEquips != null && index < displayedEquips.Count)
            {
                enhanceSystem.ApplyEnhance(displayedEquips[index]);
            }
        }
        else
        {
            HideEnhanceUI();
            GameManager.Instance.OnEnhanceEnd();
        }
    }

    public void OnReturnExpButtonClicked()
    {
        if (playerStat != null)
        {
            playerStat.ReturnExp();
        }
        HideEnhanceUI();
        GameManager.Instance.OnEnhanceEnd();
    }

    public bool IsEnhanceUIActive()
    {
        return enhancePanel.activeSelf;
    }

    public void HideEnhanceUI()
    {
        enhancePanel.SetActive(false);
    }
}
