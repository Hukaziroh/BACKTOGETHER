using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class HostDisconnectHandler : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject disconnectPanel; // 인스펙터에서 위에서 만든 패널 연결

    [Header("설정")]
    public string lobbySceneName = "Main";

    private bool wasConnected = false;

    void Update()
    {
        // 서버일 때는 체크하지 않음 (방장은 스스로 종료하므로)
        if (NetworkServer.active) return;

        if (NetworkClient.isConnected && !wasConnected)
        {
            wasConnected = true;
        }

        // 연결이 끊겼을 때
        if (wasConnected && !NetworkClient.isConnected)
        {
            wasConnected = false;
            ShowDisconnectUI();
        }
    }

    private void ShowDisconnectUI()
    {
        // 1. 연결 끊김 UI를 보여줌
        if (disconnectPanel != null)
        {
            disconnectPanel.SetActive(true);
        }

        // 2. 네트워크 클라이언트 정리
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    // 확인 버튼에 연결할 함수
    public void GoBackToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
        disconnectPanel.SetActive(false);
    }
}