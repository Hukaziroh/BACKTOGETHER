using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;
    private bool isLeaving = false;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("방 코드 UI (모든 플레이어 공용)")]
    public GameObject roomCodeUIContainer;
    public TextMeshProUGUI pauseRoomCodeText;
    public Button toggleVisibilityButton;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false;

    private bool isCodeVisible = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleRoomCodeVisibility);
        }
    }

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;

        UpdateRoomCodeUI();

        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }
    }

    private void ToggleRoomCodeVisibility()
    {
        isCodeVisible = !isCodeVisible;
        UpdateRoomCodeUI();
    }

    private void UpdateRoomCodeUI()
    {
        if (pauseRoomCodeText != null)
        {
            if (string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
            {
                pauseRoomCodeText.text = "CODE: Empty";
            }
            else
            {
                pauseRoomCodeText.text = isCodeVisible ?
                    $"CODE: {PrivateLobbyManager.currentShortCode}" :
                    "CODE: ******";
            }
        }
    }

    public void CloseOptions()
    {
        OptionsManager.instance.Close();
        if (pausePanel != null) pausePanel.SetActive(true);

        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    public void ReturnToMainMenu()
    {
        if (isLeaving) return;
        ResumeGame();

        HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        StartCoroutine(LeaveGameGracefullyRoutine());
    }

    private IEnumerator LeaveGameGracefullyRoutine()
    {
        isLeaving = true;

        Debug.Log("[퍼즈 시스템] ① 에픽 로비 비동기 퇴장 시퀀스 시작...");

        Time.timeScale = 1f;

        yield return null;

        Debug.Log("[퍼즈 시스템] ② yield null 통과");

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[퍼즈 시스템] NetworkManager.singleton이 NULL입니다.");
            isLeaving = false;
            yield break;
        }

        Debug.Log("[퍼즈 시스템] ③ NetworkManager 확인 완료");

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby == null)
        {
            Debug.LogError("[퍼즈 시스템] EOSLobby 컴포넌트를 찾지 못했습니다.");
        }
        else
        {
            Debug.Log(
                $"[퍼즈 시스템] ④ EOSLobby 발견 | " +
                $"ConnectedToLobby = {eosLobby.ConnectedToLobby}"
            );

            if (eosLobby.ConnectedToLobby)
            {
                Debug.Log("[퍼즈 시스템] ⑤ LeaveLobby() 호출 전");

                eosLobby.LeaveLobby();

                Debug.Log("[퍼즈 시스템] ⑥ LeaveLobby() 호출 완료");

                float timeout = 5f;

                while (eosLobby.ConnectedToLobby && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;

                    Debug.Log(
                        $"[퍼즈 시스템] ⑦ Lobby 퇴장 대기 중 | " +
                        $"ConnectedToLobby = {eosLobby.ConnectedToLobby} | " +
                        $"남은 시간 = {timeout:F2}"
                    );

                    yield return null;
                }

                Debug.Log(
                    $"[퍼즈 시스템] ⑧ Lobby 퇴장 대기 종료 | " +
                    $"ConnectedToLobby = {eosLobby.ConnectedToLobby} | " +
                    $"남은 시간 = {timeout:F2}"
                );
            }
            else
            {
                Debug.Log("[퍼즈 시스템] ⑤ 이미 Lobby에 연결되어 있지 않습니다.");
            }
        }

        Debug.Log(
            $"[퍼즈 시스템] ⑨ Mirror 종료 처리 시작 | " +
            $"ServerActive = {NetworkServer.active} | " +
            $"ClientActive = {NetworkClient.active} | " +
            $"ClientConnected = {NetworkClient.isConnected}"
        );

        if (NetworkServer.active)
        {
            Debug.Log("[퍼즈 시스템] ⑩ 호스트이므로 StopHost() 실행");

            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            Debug.Log("[퍼즈 시스템] ⑩ 클라이언트이므로 StopClient() 실행");

            NetworkManager.singleton.StopClient();
        }
        else
        {
            Debug.LogWarning(
                "[퍼즈 시스템] ⑩ Mirror 연결이 이미 종료된 상태입니다."
            );
        }

        Debug.Log("[퍼즈 시스템] ⑪ 퇴장 시퀀스 종료");

        isLeaving = false;
    }
}