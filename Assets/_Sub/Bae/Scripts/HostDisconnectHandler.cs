using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EpicTransport;

public class HostDisconnectHandler : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject disconnectPanel;
    public Button disconnectButton;

    [Header("설정")]
    public string lobbySceneName = "Main";

    private bool wasConnected = false;
    private bool monitoringConnection = false;
    private bool isIntentionalExit = false; // ★ 누락되었던 정상 종료 판별 변수 복구
    private ClientLobbyManager clientLobbyManager;

    private void Start()
    {
        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);

        if (disconnectButton == null && disconnectPanel != null)
        {
            disconnectButton = disconnectPanel.GetComponentInChildren<Button>();
        }

        clientLobbyManager = FindFirstObjectByType<ClientLobbyManager>();
    }

    // ★ GameQuitHandler 등에서 호출 (에러가 났던 원인 복구)
    public void SetIntentionalExit()
    {
        isIntentionalExit = true;
        Debug.Log("[HostDisconnectHandler] 의도적 종료 감지. Disconnect 팝업을 차단합니다.");
    }

    public void BeginNewConnectionAttempt()
    {
        wasConnected = false;
        monitoringConnection = false;
        isIntentionalExit = false; // ★ 새 연결 시도 시 리셋

        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);

        Debug.Log("[HostDisconnectHandler] 새 연결 시도 상태 초기화");
    }

    private void Update()
    {
        if (disconnectPanel != null && disconnectPanel.activeSelf)
        {
            if (IsSubmitPressed())
            {
                GoBackToLobby();
                return;
            }
            return;
        }

        if (NetworkServer.active) return;

        // ★ 유저가 스스로 게임을 끄거나 방을 나간 거라면 연결 끊김 패널 무시
        if (isIntentionalExit) return;

        if (clientLobbyManager != null && clientLobbyManager.IsConnecting)
        {
            return;
        }

        if (NetworkClient.isConnected)
        {
            if (!monitoringConnection)
            {
                monitoringConnection = true;
                wasConnected = true;
                Debug.Log("[HostDisconnectHandler] 새 Mirror 연결 감시 시작");
            }
            return;
        }

        if (!monitoringConnection) return;

        if (wasConnected)
        {
            wasConnected = false;
            monitoringConnection = false;

            Debug.Log("[HostDisconnectHandler] 정상 연결 후 Disconnect 감지!");

            if (disconnectPanel != null)
                disconnectPanel.SetActive(true);

            FocusDisconnectButton();
        }
    }

    private bool IsSubmitPressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void FocusDisconnectButton()
    {
        if (EventSystem.current != null && disconnectButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(disconnectButton.gameObject);
        }
    }

    public void GoBackToLobby()
    {
        wasConnected = false;
        monitoringConnection = false;

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();

            EosTransport transport = NetworkManager.singleton.GetComponent<EosTransport>();
            if (transport != null)
            {
                transport.Shutdown();
            }
        }

        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);

        SceneManager.LoadScene(lobbySceneName);
    }
}