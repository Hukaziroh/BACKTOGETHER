using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    private static PauseManager _instance;
    public static PauseManager instance
    {
        get
        {
            if (_instance == null || _instance.gameObject.scene != SceneManager.GetActiveScene())
            {
                PauseManager[] managers = FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude);
                foreach (var mgr in managers)
                {
                    if (mgr.gameObject.scene == SceneManager.GetActiveScene())
                    {
                        _instance = mgr;
                        break;
                    }
                }
                if (_instance == null && managers.Length > 0) _instance = managers[0];
            }
            return _instance;
        }
        set => _instance = value;
    }

    private bool isLeaving = false;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false;

    void Awake()
    {
        instance = this;
    }

    // 🌟 [추가된 토글 기능] 일시정지 상태에 따라 열기/닫기 전환
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;

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

    public void CloseOptions()
    {
        OptionsManager.instance.Close();
        if (pausePanel != null) pausePanel.SetActive(true);

        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    public void ClosePause()
    {
        ResumeGame();
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

        // =========================================================
        // 1. EOS Lobby 퇴장 및 방 파괴
        // =========================================================
        if (eosLobby != null)
        {
            Debug.Log($"[퍼즈 시스템] ② EOSLobby 발견 | ConnectedToLobby = {eosLobby.ConnectedToLobby} | IsLeavingLobby = {eosLobby.IsLeavingLobby}");

            if (eosLobby.ConnectedToLobby)
            {
                if (NetworkServer.active)
                {
                    Debug.Log("[퍼즈 시스템] ③ 호스트이므로 EOS Lobby Destroy(방 파괴) 요청");
                    eosLobby.DestroyLobby();
                }
                else
                {
                    Debug.Log("[퍼즈 시스템] ③ 클라이언트이므로 EOS Lobby Leave(퇴장) 요청");
                    eosLobby.LeaveLobby();
                }

                float timeout = 5f;

                while (eosLobby.IsLeavingLobby && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (eosLobby.IsLeavingLobby)
                {
                    Debug.LogWarning("[퍼즈 시스템] ④ EOS Lobby 퇴장 콜백이 5초 안에 도착하지 않았습니다.");
                }
                else if (eosLobby.ConnectedToLobby)
                {
                    Debug.LogWarning("[퍼즈 시스템] ④ EOS Lobby 퇴장 실패 또는 아직 Lobby에 연결된 상태입니다.");
                }
                else
                {
                    Debug.Log("[퍼즈 시스템] ④ EOS Lobby 정상 퇴장/파괴 완료");
                }
            }
            else
            {
                Debug.Log("[퍼즈 시스템] ③ 현재 EOS Lobby에 연결되어 있지 않습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[퍼즈 시스템] EOSLobby 컴포넌트를 찾을 수 없습니다.");
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // =========================================================
        // 2. Mirror 네트워크 종료
        // =========================================================
        if (NetworkServer.active)
        {
            Debug.Log("[퍼즈 시스템] ⑤ 호스트 종료");
            NetworkManager.singleton.StopHost();
        }
        else
        {
            Debug.Log("[퍼즈 시스템] ⑤ 클라이언트 종료");
            NetworkManager.singleton.StopClient();
        }

        // =========================================================
        // 3. 종료 처리
        // =========================================================
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[퍼즈 시스템] ⑥ 게임 종료 시퀀스 완료");

        isLeaving = false;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}