using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 신규 인풋 시스템 네임스페이스

public class HostDisconnectHandler : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject disconnectPanel;
    public Button disconnectButton; // 패널 내부의 [로비로 돌아가기] 버튼 연결용

    [Header("설정")]
    public string lobbySceneName = "Main";

    private bool wasConnected = false;
    private bool isIntentionalExit = false;

    private void Start()
    {
        // 시작 시 디스커넥트 팝업 비활성화
        if (disconnectPanel != null)
            disconnectPanel.SetActive(false);

        // disconnectButton 인스펙터 미할당 대비 자동 탐색
        if (disconnectButton == null && disconnectPanel != null)
        {
            disconnectButton = disconnectPanel.GetComponentInChildren<Button>();
        }
    }

    private void Update()
    {
        // 0. 디스커넥트 팝업이 열려 있는 동안 Input System 기반 입력 처리
        if (disconnectPanel != null && disconnectPanel.activeSelf)
        {
            if (IsSubmitPressed())
            {
                GoBackToLobby();
                return;
            }
            return;
        }

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
                if (disconnectPanel != null && !disconnectPanel.activeSelf)
                {
                    disconnectPanel.SetActive(true);

                    if (disconnectButton == null)
                    {
                        disconnectButton = disconnectPanel.GetComponentInChildren<Button>();
                    }

                    // 글로벌 인풋 매니저 스코프를 디스커넥트 팝업으로 전환
                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.SetFocusScope(disconnectPanel);
                    }

                    // 팝업이 켜질 때 버튼 포커스 최초 지정
                    FocusDisconnectButton();
                }
            }
        }
    }

    private void LateUpdate()
    {
        // EventSystem이나 외부 인풋 스크립트에 의해 포커스가 해제되거나 뒤쪽 UI로 빠지는 것을 강제 보정
        if (disconnectPanel != null && disconnectPanel.activeSelf)
        {
            if (EventSystem.current != null)
            {
                GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

                // 포커스가 null이거나 disconnectPanel 외부 UI를 가리키고 있을 때 고정
                if (currentSelected == null || !currentSelected.transform.IsChildOf(disconnectPanel.transform))
                {
                    FocusDisconnectButton();
                }
            }
        }
    }

    /// <summary>
    /// Input System 패키지를 사용한 키보드 및 게임패드 입력 체크
    /// </summary>
    private bool IsSubmitPressed()
    {
        // 키보드 입력 (Enter, Keypad Enter, Space)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        // 게임패드 입력 (A / Cross 버튼)
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

    /// <summary>
    /// 디스커넥트 팝업 창의 [확인 / 로비로 돌아가기] 버튼 OnClick에 연결
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