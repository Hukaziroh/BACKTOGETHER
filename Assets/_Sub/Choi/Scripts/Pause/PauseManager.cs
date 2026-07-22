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

                }
            }
        }

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

        Debug.Log(
            "[퍼즈 시스템] ① 에픽 로비 비동기 퇴장 시퀀스 시작..."
        );

        Time.timeScale = 1f;

        yield return null;

        if (NetworkManager.singleton == null)
        {
            Debug.LogError(
                "[퍼즈 시스템] NetworkManager.singleton이 NULL입니다."
            );

            isLeaving = false;
            yield break;
        }

        EOSLobby eosLobby =
            NetworkManager.singleton.GetComponent<EOSLobby>();


        // =========================================================
        // 1. EOS Lobby 퇴장
        // =========================================================

        if (eosLobby != null)
        {
            Debug.Log(
                $"[퍼즈 시스템] ② EOSLobby 발견 | " +
                $"ConnectedToLobby = {eosLobby.ConnectedToLobby} | " +
                $"IsLeavingLobby = {eosLobby.IsLeavingLobby}"
            );

            if (eosLobby.ConnectedToLobby)
            {
                Debug.Log(
                    "[퍼즈 시스템] ③ EOS Lobby 퇴장 요청"
                );

                eosLobby.LeaveLobby();

                // 최대 5초 동안 Lobby 퇴장 완료 대기
                float timeout = 5f;

                while (
                    eosLobby.IsLeavingLobby &&
                    timeout > 0f
                )
                {
                    timeout -= Time.unscaledDeltaTime;

                    yield return null;
                }


                // =================================================
                // 2. 퇴장 결과 확인
                // =================================================

                if (eosLobby.IsLeavingLobby)
                {
                    Debug.LogWarning(
                        "[퍼즈 시스템] ④ EOS Lobby 퇴장 콜백이 " +
                        "5초 안에 도착하지 않았습니다."
                    );
                }
                else if (eosLobby.ConnectedToLobby)
                {
                    Debug.LogWarning(
                        "[퍼즈 시스템] ④ EOS Lobby 퇴장 실패 또는 " +
                        "아직 Lobby에 연결된 상태입니다."
                    );
                }
                else
                {
                    Debug.Log(
                        "[퍼즈 시스템] ④ EOS Lobby 정상 퇴장 완료"
                    );
                }
            }
            else
            {
                Debug.Log(
                    "[퍼즈 시스템] ③ 현재 EOS Lobby에 연결되어 있지 않습니다."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[퍼즈 시스템] EOSLobby 컴포넌트를 찾을 수 없습니다."
            );
        }


        // =========================================================
        // 3. Mirror 네트워크 종료
        // =========================================================

        if (NetworkServer.active)
        {
            Debug.Log(
                "[퍼즈 시스템] ⑤ 호스트 종료"
            );

            NetworkManager.singleton.StopHost();
        }
        else if (
            NetworkClient.active ||
            NetworkClient.isConnected
        )
        {
            Debug.Log(
                "[퍼즈 시스템] ⑤ 클라이언트 종료"
            );

            NetworkManager.singleton.StopClient();
        }
        else
        {
            Debug.Log(
                "[퍼즈 시스템] ⑤ 이미 Mirror 네트워크가 종료된 상태입니다."
            );
        }


        // =========================================================
        // 4. 종료 처리
        // =========================================================

        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log(
            "[퍼즈 시스템] ⑥ 게임 종료 시퀀스 완료"
        );

        isLeaving = false;
    }
}