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

        ClearNetworkSession(); // 🌟 강제 종료 시 에픽 세션 즉시 정리
    }

    public void GoBackToLobby()
    {
        ClearNetworkSession(); // 🌟 로비로 돌아갈 때 에픽 세션 즉시 정리
        SceneManager.LoadScene(lobbySceneName);
        if (disconnectPanel != null) disconnectPanel.SetActive(false);
    }

    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
        ClearNetworkSession(); // 🌟 의도적 종료 시에도 정리
    }

    // 🌟 에픽 로비와 미러 연결을 즉시 끊어버리는 핵심 함수 추가
    private void ClearNetworkSession()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient(); // 클라이언트 접속 종료

            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null)
            {
                eosLobby.LeaveLobby(); // 에픽 온라인 서비스 방 즉시 퇴장 요청
            }
        }
    }
}