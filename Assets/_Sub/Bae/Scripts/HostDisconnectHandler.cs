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
        if (NetworkServer.active)
            return;

        if (NetworkClient.isConnected && !wasConnected)
        {
            wasConnected = true;
            isIntentionalExit = false;
        }

        if (wasConnected && !NetworkClient.isConnected)
        {
            wasConnected = false;

            if (isIntentionalExit)
            {
                Debug.Log("[HostDisconnectHandler] 의도적인 종료 - 메인으로 이동");

                isIntentionalExit = false;

                if (disconnectPanel != null)
                    disconnectPanel.SetActive(false);

                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.Log("[HostDisconnectHandler] 호스트 연결 끊김");

                ShowDisconnectUI();
            }
        }
    }

    private void ShowDisconnectUI()
    {
        if (disconnectPanel != null)
            disconnectPanel.SetActive(true);
    }

    public void GoBackToLobby()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);

        SceneManager.LoadScene(lobbySceneName);
    }

    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
    }
}