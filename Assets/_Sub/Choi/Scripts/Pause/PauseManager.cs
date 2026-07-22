using System.Collections;
using Epic.OnlineServices.Lobby;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Epic.OnlineServices;

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
        if (pauseRoomCodeText == null) return;

        // 1. 우선 PrivateLobbyManager에 코드가 있다면 가져옵니다.
        string displayCode = PrivateLobbyManager.currentShortCode;

        // 2. 만약 비어있다면(클라이언트 등) 에픽 로비(EOSLobby) 어트리뷰트에서 코드를 가져옵니다.
        if (string.IsNullOrEmpty(displayCode) && NetworkManager.singleton != null)
        {
            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby && eosLobby.ConnectedLobbyDetails != null)
            {
                try
                {
                    Attribute shortCodeAttribute = new Attribute();
                    Result result = eosLobby.ConnectedLobbyDetails.CopyAttributeByKey(
                        new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "SHORTCODE" },
                        out shortCodeAttribute
                    );

                    if (result == Result.Success)
                    {
                        displayCode = shortCodeAttribute.Data.Value.AsUtf8;
                    }
                }
                catch
                {
                    // 예외 무시
                }
            }
        }

        // 3. 최종적으로 UI에 표시
        if (string.IsNullOrEmpty(displayCode))
        {
            pauseRoomCodeText.text = "CODE:\nEmpty";
        }
        else
        {
            pauseRoomCodeText.text = isCodeVisible ?
                $"CODE:\n{displayCode}" :
                "CODE:\n******";
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

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = null;
#endif

        Debug.Log("[퍼즈 시스템] ① 에픽 로비 비동기 퇴장 시퀀스 시작...");

        Time.timeScale = 1f;

        yield return null;

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[퍼즈 시스템] NetworkManager.singleton이 NULL입니다.");
            isLeaving = false;
            yield break;
        }

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
        }

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        yield return null;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        isLeaving = false;
    }
}