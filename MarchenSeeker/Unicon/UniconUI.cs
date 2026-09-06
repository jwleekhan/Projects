using UnityEngine;
using UnityEngine.UI;

public class UniconUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Button[] buttons;
    [SerializeField] GameObject adaptationButton;

    private void Start()
    {
        SetActivePanel(false);
    }

    public void SetActivePanel(bool active)
    {
        panel.SetActive(active);
        adaptationButton.SetActive(!active);
    }

    public void SelectScene(int sceneNumber)
    {
        UniconManager.Instance.SelectScene(sceneNumber);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = i != sceneNumber - 1;
        }
    }
}
