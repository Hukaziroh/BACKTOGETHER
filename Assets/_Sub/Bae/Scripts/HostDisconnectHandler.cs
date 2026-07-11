using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using EpicTransport; // 추가

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

        ClearNetworkSession();
    }

    public void GoBackToLobby()
    {
        ClearNetworkSession(); 
        SceneManager.LoadScene(lobbySceneName);
        if (disconnectPanel != null) disconnectPanel.SetActive(false);
    }

    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
  
    }

    private void ClearNetworkSession()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient(); 
            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null)
            {
                eosLobby.LeaveLobby();
            }
        }
    }
}