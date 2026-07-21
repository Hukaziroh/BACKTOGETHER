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
                pauseRoomCodeText.text = "CODE:\nEmpty";
            }
            else
            {
                pauseRoomCodeText.text = isCodeVisible ?
                    $"CODE:\n{PrivateLobbyManager.currentShortCode}" :
                    "CODE:\n******";
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
        Debug.Log("[퍼즈 시스템] 에픽 로비 비동기 퇴장 시퀀스 시작...");

        Time.timeScale = 1f;
        yield return null;

        if (NetworkManager.singleton != null)
        {
            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.LeaveLobby();

                float timeout = 5f;
                while (eosLobby.ConnectedToLobby && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                Debug.Log("[퍼즈 시스템] EOS 네이티브 정리 완료 확인.");
            }
        }

        if (NetworkServer.active)
        {
            Debug.Log("[퍼즈 시스템] 호스트 종료 및 Offline 씬 전환");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[퍼즈 시스템] 클라이언트 종료 및 Offline 씬 전환");
            NetworkManager.singleton.StopClient();
        }

        isLeaving = false;
    }
}