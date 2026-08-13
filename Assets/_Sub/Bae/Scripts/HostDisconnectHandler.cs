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

    private void Start()
    {
        // 시작 시 디스커넥트 팝업 비활성화
        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);
    }

    private void Update()
    {
        // 내가 호스트(서버)라면 감지할 필요 없음
        if (NetworkServer.active) return;

        // 1. 클라이언트가 정상 연결된 상태
        if (NetworkClient.isConnected)
        {
            wasConnected = true;
        }
        // 2. 연결되어 있다가 호스트가 나가서 끊어진 순간 감지
        else if (wasConnected)
        {
            wasConnected = false;

            if (isIntentionalExit)
            {
                // 유저가 직접 [나가기] 버튼을 누른 경우 -> 팝업 없이 로비로 이동
                isIntentionalExit = false;
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                // 호스트가 강제 종료되거나 튕김 -> 연결 끊김 팝업창 띄우기
                if (disconnectPanel != null)
                    disconnectPanel.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 디스커넥트 팝업 창의 [확인 / 로비로 돌아가기] 버튼에 연결
    /// </summary>
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

    /// <summary>
    /// Pause 메뉴 등에서 유저가 스스로 방을 나갈 때 호출
    /// </summary>
    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
    }
}