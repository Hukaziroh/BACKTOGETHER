using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class HostDisconnectHandler : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject disconnectPanel;

    [Header("설정")]
    public string lobbySceneName = "Main";

    private bool wasConnected = false;
    private bool isIntentionalExit = false;

    void Update()
    {
        if (NetworkServer.active) return;

        if (NetworkClient.isConnected && !wasConnected)
        {
            wasConnected = true;
            isIntentionalExit = false; 
        }
        if (wasConnected && !NetworkClient.isConnected)
        {
            wasConnected = false;
            if (!isIntentionalExit)
            {
                ShowDisconnectUI();
            }

            isIntentionalExit = false;
        }
    }

    private void ShowDisconnectUI()
    {
        if (disconnectPanel != null)
        {
            disconnectPanel.SetActive(true);
        }
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    public void GoBackToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
        disconnectPanel.SetActive(false);
    }
    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
    }
}