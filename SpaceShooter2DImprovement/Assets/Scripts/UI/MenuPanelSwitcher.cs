using UnityEngine;

public class MenuPanelSwitcher : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject instructionsPanel;
    public GameObject creditsPanel;

    public void ShowMain()
    {
        SetPanel(mainPanel);
    }

    public void ShowInstructions()
    {
        SetPanel(instructionsPanel);
    }

    public void ShowCredits()
    {
        SetPanel(creditsPanel);
    }

    private void SetPanel(GameObject activePanel)
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(activePanel == mainPanel);
        }
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(activePanel == instructionsPanel);
        }
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(activePanel == creditsPanel);
        }
    }
}
