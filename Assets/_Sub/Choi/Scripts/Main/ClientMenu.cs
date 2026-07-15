using UnityEngine;

public class ClientMenu : MonoBehaviour
{
    public GameObject ClientPanel;
    public GameObject ConnectPanel;
    public GameObject optionsPanel;

    public void OpenClientPanel()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(false);
        if (ClientPanel != null) ClientPanel.SetActive(true);
    }

    public void CloseClientPanel()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(true);
        if (ClientPanel != null) ClientPanel.SetActive(false);
    }

    public void OpenOptionPanel()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptionPanel()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}