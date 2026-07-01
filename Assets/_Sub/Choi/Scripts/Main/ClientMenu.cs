using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientMenu : MonoBehaviour
{
    public TMP_InputField roomCodeInput;
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

    // 붙여넣기 전용 기능
    public void PasteCode()
    {
        roomCodeInput.text = GUIUtility.systemCopyBuffer;
    }
}
