using UnityEngine;

public class ClientMenu : MonoBehaviour
{
    public GameObject ClientPanel;
    public GameObject ConnectPanel;

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
}